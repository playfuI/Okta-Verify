// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.UserVerificationEnrollmentViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class UserVerificationEnrollmentViewModel : BaseEnrollAccountViewModel
  {
    private readonly string accountId;
    private readonly string userId;
    private readonly string userEmail;
    private readonly bool enableUV;

    public UserVerificationEnrollmentViewModel(
      string orgDomain,
      string orgId,
      string userEmail,
      string userId,
      string accountId,
      bool enableUV)
      : base(orgDomain, orgId)
    {
      accountId.EnsureNotNullOrBlank(nameof (accountId));
      this.userEmail = userEmail;
      this.accountId = accountId;
      this.userId = userId;
      this.enableUV = enableUV;
      this.InitializeEnrollment();
    }

    public override string EnrollingText => this.GetEnrollingText();

    protected override async Task InitializeEnrollmentFlows()
    {
      if (this.enableUV)
        await base.InitializeEnrollmentFlows().ConfigureAwait(true);
      else
        await this.InitiateDisableAccountVerificationFlow().ConfigureAwait(true);
    }

    protected override async Task<SignInResult> StartSignIn()
    {
      UserVerificationEnrollmentViewModel enrollmentViewModel = this;
      SignInResult signInResult;
      using (SignInViewModel viewModel = new SignInViewModel(enrollmentViewModel.SignInUrl, enrollmentViewModel.userEmail, enrollmentViewModel.userId, enrollmentViewModel.OrgId))
        signInResult = await enrollmentViewModel.SignIn(EnrollViewState.RedirectedToBrowser, viewModel).ConfigureAwait(true);
      return signInResult;
    }

    protected override EnrollmentEndEventArg GetEnrollmentFailureEventArg(
      string orgId,
      string errorMessage,
      bool isCancellation)
    {
      return !isCancellation ? EnrollmentEndEventArg.AsAccountUpdate(false, orgId, this.accountId, errorMessage) : EnrollmentEndEventArg.AsAccountUpdateCancelled(orgId, this.accountId);
    }

    protected override async Task StartEnrollment(
      EnrollmentParameters enrollmentParameters,
      bool uvRequired)
    {
      await this.InitiateUserVerificationFlow(enrollmentParameters, uvRequired).ConfigureAwait(true);
    }

    [AnalyticsScenario(ScenarioType.AccountUpdate)]
    private async Task InitiateUserVerificationFlow(
      EnrollmentParameters enrollmentParameters,
      bool uvRequired)
    {
      UserVerificationEnrollmentViewModel enrollmentViewModel = this;
      try
      {
        bool flag = enrollmentViewModel.SignInManager.CanSignInWithWindowsHello;
        if (flag && !enrollmentViewModel.SignInManager.CheckIfWindowsHelloSignInConfigured())
          flag = await enrollmentViewModel.InitiateWindowsHelloConfiguration(enrollmentViewModel.userEmail, uvRequired).ConfigureAwait(true);
        if (flag)
          await enrollmentViewModel.EnrollAccountVerificationAsync(enrollmentParameters.AccessToken).ConfigureAwait(false);
        else
          enrollmentViewModel.FailEnrollment();
      }
      catch (Exception ex) when (!ex.IsCritical(enrollmentViewModel.Logger))
      {
        enrollmentViewModel.Logger.WriteException("An error occurred while enrolling Windows Hello for " + enrollmentViewModel.SignInUrl, ex);
        enrollmentViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    internal async Task EnrollAccountVerificationAsync(string accessToken)
    {
      UserVerificationEnrollmentViewModel enrollmentViewModel = this;
      AuthenticatorOperationResult result = AuthenticatorOperationResult.Unknown;
      try
      {
        enrollmentViewModel.EnrollmentInProcess = true;
        enrollmentViewModel.UpdateCurrentState(EnrollViewState.AddingExtraVerification, (BaseViewModel) new EnrollingViewModel(enrollmentViewModel.GetEnrollingText()));
        result = await enrollmentViewModel.ClientAccountManager.AddAccountUserVerification(enrollmentViewModel.accountId, accessToken).ConfigureAwait(true);
      }
      catch (Exception ex) when (!ex.IsCritical(enrollmentViewModel.Logger))
      {
        enrollmentViewModel.Logger.WriteException("Got unexpected exception on adding user verification", ex);
        enrollmentViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      finally
      {
        enrollmentViewModel.EnrollmentInProcess = false;
        enrollmentViewModel.HandleEnrollmentUpdateResult(result, enrollmentViewModel.ExtraVerificationEnabledText.CultureFormat((object) enrollmentViewModel.CurrentEnrollEmail), Resources.ErrorMessageGeneric);
      }
    }

    [AnalyticsScenario(ScenarioType.AccountUpdate)]
    internal async Task InitiateDisableAccountVerificationFlow()
    {
      UserVerificationEnrollmentViewModel enrollmentViewModel = this;
      string successMessage = string.Empty;
      AuthenticatorOperationResult result = AuthenticatorOperationResult.Unknown;
      try
      {
        IOktaAccount accountModel;
        if (!enrollmentViewModel.ClientAccountManager.TryGetAccount(enrollmentViewModel.accountId, out accountModel))
        {
          successMessage = (string) null;
        }
        else
        {
          enrollmentViewModel.UpdateCurrentState(EnrollViewState.RemovingExtraVerification, (BaseViewModel) new EnrollingViewModel(enrollmentViewModel.GetEnrollingText()));
          result = await enrollmentViewModel.ClientAccountManager.RemoveAccountUserVerification(accountModel).ConfigureAwait(true);
          if (result == AuthenticatorOperationResult.Success)
          {
            string str;
            if (!enrollmentViewModel.WindowsHelloSupported)
              str = Resources.UserVerificationDisabledPasscode.CultureFormat((object) accountModel.UserLogin);
            else
              str = Resources.UserVerificationDisabledWindowsHello.CultureFormat((object) accountModel.UserLogin);
            successMessage = str;
          }
          accountModel = (IOktaAccount) null;
          successMessage = (string) null;
        }
      }
      catch (Exception ex) when (!ex.IsCritical(enrollmentViewModel.Logger))
      {
        enrollmentViewModel.Logger.WriteErrorEx("Failed to disable the account verification: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\UserVerificationEnrollmentViewModel.cs", nameof (InitiateDisableAccountVerificationFlow));
        enrollmentViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
        successMessage = (string) null;
      }
      finally
      {
        enrollmentViewModel.HandleEnrollmentUpdateResult(result, successMessage, Resources.ErrorMessageGeneric);
      }
    }

    private string GetEnrollingText() => this.enableUV ? (!this.WindowsHelloSupported ? Resources.UserVerificationEnablingPasscode : Resources.UserVerificationEnablingWindowsHello) : (!this.WindowsHelloSupported ? Resources.UserVerificationDisablingPasscode : Resources.UserVerificationDisablingWindowsHello);

    private void HandleEnrollmentUpdateResult(
      AuthenticatorOperationResult result,
      string successMessage,
      string failureMessage)
    {
      switch (result)
      {
        case AuthenticatorOperationResult.Success:
          this.PublishEndEnrollmentEvent(EnrollmentEndEventArg.AsAccountUpdate(true, this.OrgId, this.accountId, successMessage));
          break;
        case AuthenticatorOperationResult.Cancelled:
          this.FailEnrollment(string.Empty, true);
          break;
        default:
          this.PublishEndEnrollmentEvent(EnrollmentEndEventArg.AsAccountUpdate(false, this.OrgId, this.accountId, failureMessage));
          break;
      }
    }
  }
}
