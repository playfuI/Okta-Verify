// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.EnrollAccountViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public abstract class EnrollAccountViewModel : BaseEnrollAccountViewModel
  {
    protected EnrollAccountViewModel(string orgDomain, string orgId)
      : base(orgDomain, orgId)
    {
    }

    protected virtual bool IsNewEnrollment => true;

    [AnalyticsScenario(ScenarioType.AddAccount)]
    internal async Task<bool> EnrollAccountAsync(
      EnrollmentParameters parameters,
      bool withBiometrics,
      bool setAsDefault = false)
    {
      EnrollAccountViewModel accountViewModel = this;
      bool enrolled = false;
      EnrollmentError enrollError = new EnrollmentError();
      try
      {
        accountViewModel.EnrollmentInProcess = true;
        accountViewModel.UpdateCurrentState(EnrollViewState.AddingAccount, (BaseViewModel) new EnrollingViewModel(Resources.EnrollAddingAccountText));
        (IOktaAccount, EnrollmentError) valueTuple = await accountViewModel.RequestAccountEnrollment(parameters, withBiometrics, setAsDefault).ConfigureAwait(true);
        enrolled = valueTuple.Item1 != null;
        enrollError = valueTuple.Item2;
        if (enrolled)
          await accountViewModel.NotifyEnrollmentSuccess(valueTuple.Item1).ConfigureAwait(true);
      }
      finally
      {
        accountViewModel.EnrollmentInProcess = false;
        if (!enrolled)
          accountViewModel.HandleFailedEnrollment(parameters, enrollError).AsBackgroundTask("cancelling enrollment", accountViewModel.Logger, accountViewModel.AnalyticsProvider);
      }
      return enrolled;
    }

    protected virtual async Task<(IOktaAccount Account, EnrollmentError Error)> RequestAccountEnrollment(
      EnrollmentParameters parameters,
      bool withBiometrics,
      bool setAsDefault)
    {
      return await this.ClientAccountManager.EnrollAccount(parameters, withBiometrics, setAsDefault).ConfigureAwait(true);
    }

    protected virtual async Task<bool> AnyOtherAccountsForOrg(string orgId) => await this.ClientAccountManager.AnyAccountsAsync((Func<IOktaAccount, bool>) (a => a.OrgId == orgId)).ConfigureAwait(true);

    protected async Task InitiateEnrollmentWithBiometrics(
      EnrollmentParameters parameters,
      bool uvRequired)
    {
      EnrollAccountViewModel accountViewModel = this;
      bool useBiometrics = await accountViewModel.CanUseBiometrics(parameters.UserEmail, uvRequired).ConfigureAwait(true);
      if (uvRequired && !useBiometrics)
      {
        accountViewModel.Logger.WriteWarningEx("Stop enrolling " + parameters.SignInUrl + " due to UV required", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\EnrollAccountViewModel.cs", nameof (InitiateEnrollmentWithBiometrics));
        await accountViewModel.ResetFailedEnrollment(parameters.SignInUrl, Resources.ErrorMessageUvRequired).ConfigureAwait(false);
      }
      else
      {
        ConfiguredTaskAwaitable<bool> configuredTaskAwaitable = accountViewModel.ShouldEnrollAccountAsDefault(parameters).ConfigureAwait(true);
        bool setAsDefault = await configuredTaskAwaitable;
        configuredTaskAwaitable = accountViewModel.EnrollAccountAsync(parameters, useBiometrics, setAsDefault).ConfigureAwait(true);
        int num = await configuredTaskAwaitable ? 1 : 0;
      }
    }

    protected override async Task<(bool ProceedEnrolling, string DenialReason)> ShouldProceedEnrolling(
      bool uvRequired,
      bool inRemoteSession,
      string userId)
    {
      EnrollAccountViewModel accountViewModel = this;
      bool flag = accountViewModel.IsNewEnrollment;
      if (flag)
        flag = await accountViewModel.ClientAccountManager.AnyAccountsAsync((Func<IOktaAccount, bool>) (account => account.UserId == userId)).ConfigureAwait(true);
      // ISSUE: reference to a compiler-generated method
      return flag ? (false, Resources.ErrorMessageAccountAlreadyEnrolled) : await accountViewModel.\u003C\u003En__0(uvRequired, inRemoteSession, userId).ConfigureAwait(false);
    }

    protected abstract Task ResetFailedEnrollment(string signInUrl, string errorMessage);

    private async Task HandleFailedEnrollment(
      EnrollmentParameters parameters,
      EnrollmentError error)
    {
      EnrollAccountViewModel accountViewModel = this;
      if (accountViewModel.EnrollViewState != EnrollViewState.AddingAccount)
        return;
      bool flag = false;
      switch (error.Reason)
      {
        case EnrollmentFailureReason.CryptoError:
          flag = await accountViewModel.HandleCryptoRelatedFailures(parameters, error.ErrorCode).ConfigureAwait(false);
          break;
        case EnrollmentFailureReason.ApiError:
          flag = await accountViewModel.HandleApiRelatedFailures(parameters, error.ErrorCode).ConfigureAwait(false);
          break;
      }
      if (flag)
        return;
      await accountViewModel.ResetFailedEnrollment(parameters.SignInUrl, Resources.ErrorMessageGeneric).ConfigureAwait(false);
    }

    private async Task<bool> HandleCryptoRelatedFailures(
      EnrollmentParameters parameters,
      int platformErrorCode)
    {
      EnrollAccountViewModel accountViewModel = this;
      if (platformErrorCode != 3)
        return false;
      accountViewModel.Logger.WriteInfoEx("Biometrics key set up cancelled; back to the consent screen...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\EnrollAccountViewModel.cs", nameof (HandleCryptoRelatedFailures));
      ConfiguredTaskAwaitable<bool> configuredTaskAwaitable = accountViewModel.ClientAccountManager.IsAccountUserVerificationRequired(parameters.OrgId, parameters.SignInUrl, parameters.AccessToken).ConfigureAwait(false);
      bool uvRequired = await configuredTaskAwaitable;
      configuredTaskAwaitable = accountViewModel.InitiateAllowExtraVerification(parameters.UserEmail, uvRequired).ConfigureAwait(false);
      bool useBiometrics = await configuredTaskAwaitable;
      configuredTaskAwaitable = accountViewModel.ShouldEnrollAccountAsDefault(parameters).ConfigureAwait(true);
      bool setAsDefault = await configuredTaskAwaitable;
      configuredTaskAwaitable = accountViewModel.EnrollAccountAsync(parameters, useBiometrics, setAsDefault).ConfigureAwait(false);
      int num = await configuredTaskAwaitable ? 1 : 0;
      return true;
    }

    private async Task<bool> HandleApiRelatedFailures(
      EnrollmentParameters parameters,
      int apiErrorCode)
    {
      bool handled = true;
      switch (apiErrorCode)
      {
        case 152:
        case 153:
          await this.ResetFailedEnrollment(parameters.SignInUrl, Resources.ErrorMessageDeviceDeactivated).ConfigureAwait(false);
          break;
        case 158:
          await this.ResetFailedEnrollment(parameters.SignInUrl, Resources.ErrorMessageUvRequired).ConfigureAwait(false);
          break;
        default:
          handled = false;
          break;
      }
      return handled;
    }

    private async Task NotifyEnrollmentSuccess(IOktaAccount account)
    {
      EnrollAccountViewModel accountViewModel = this;
      accountViewModel.UpdateCurrentState(EnrollViewState.AccountAdded, (BaseViewModel) new AccountAddedViewModel(account));
      await Task.Delay(BaseEnrollAccountViewModel.EnrollmentSuccessConfirmationDelay).ConfigureAwait(true);
      accountViewModel.EventAggregator.GetEvent<AccountEnrollEndEvent>()?.Publish(EnrollmentEndEventArg.AsAccountAdded(account.Enrollment));
    }

    private async Task<bool> ShouldEnrollAccountAsDefault(EnrollmentParameters enrollmentParameters)
    {
      EnrollAccountViewModel accountViewModel = this;
      bool setAsDefault = false;
      if (await accountViewModel.AnyOtherAccountsForOrg(enrollmentParameters.OrgId).ConfigureAwait(true))
      {
        SetAsDefaultAccountViewModel model = new SetAsDefaultAccountViewModel(enrollmentParameters.UserEmail, enrollmentParameters.SignInUri.Host);
        setAsDefault = await accountViewModel.UseInlineViewModel<bool>(EnrollViewState.SetAsDefaultAccount, (AsyncInlineOperationViewModel<bool>) model).ConfigureAwait(true);
      }
      return setAsDefault;
    }
  }
}
