// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.SignInViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Telemetry;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class SignInViewModel : AsyncInlineScenarioViewModel<SignInResult>
  {
    private readonly IClientSignInManager signInManager;
    private readonly IClientAccountManager clientAccountManager;

    public SignInViewModel(string defaultSignInUrl = null)
      : base(AppTelemetryScenario.OIDCSignIn)
    {
      this.signInManager = AppInjector.Get<IClientSignInManager>();
      this.clientAccountManager = AppInjector.Get<IClientAccountManager>();
      this.SignInCommand = (ICommand) new DelegateCommand<string>((Action<string>) (url => this.SignInNewOrg(url).AsBackgroundTask("sign in command", this.Logger)));
      this.LaunchBrowserCommand = (ICommand) new DelegateCommand<string>(new Action<string>(((BaseViewModel) this).LaunchLink));
      this.CancelEnrollmentCommand = (ICommand) new DelegateCommand(new Action(this.CancelSignIn));
      this.CanGoBack = true;
      this.DefaultSignInURL = defaultSignInUrl;
    }

    public SignInViewModel(string url, string loginHint, string userId, string orgId)
      : this(url)
    {
      this.SignInExistingOrg(url, loginHint, userId, orgId).AsBackgroundTask("signing in with existing account", this.Logger, this.AnalyticsProvider);
    }

    public string DefaultSignInURL { get; }

    public ICommand SignInCommand { get; }

    public ICommand LaunchBrowserCommand { get; }

    public ICommand CancelEnrollmentCommand { get; }

    public ICommand CancelSignInTaskCommand { get; private set; }

    public virtual bool CanGoBack { get; private set; }

    public bool RedirectedToBrowser { get; private set; }

    private static TelemetryEventStatus GetFailureStatus(SignInErrorCode errorCode)
    {
      switch (errorCode)
      {
        case SignInErrorCode.Timeout:
          return TelemetryEventStatus.Abandoned;
        case SignInErrorCode.Cancelled:
        case SignInErrorCode.UserCancelled:
          return TelemetryEventStatus.UserCancelled;
        case SignInErrorCode.InvalidUrl:
        case SignInErrorCode.UserMismatch:
          return TelemetryEventStatus.UserError;
        case SignInErrorCode.EnrollmentNotSupported:
          return TelemetryEventStatus.Disallowed;
        default:
          return TelemetryEventStatus.UnknownError;
      }
    }

    [AnalyticsScenario(ScenarioType.AddAccount)]
    private async Task SignInNewOrg(string url)
    {
      this.StartSignInTelemetry(url, (string) null);
      this.EndSignInTelemetry(await this.SignInNewOrgInternal(url).ConfigureAwait(true));
    }

    [AnalyticsScenario(ScenarioType.AccountUpdate)]
    private async Task SignInExistingOrg(
      string url,
      string userEmail,
      string userId,
      string orgId)
    {
      this.StartSignInTelemetry(url, userId);
      int accountCount = this.clientAccountManager.Accounts.Count<IOktaAccount>((Func<IOktaAccount, bool>) (a => a.OrgId.Equals(orgId, StringComparison.Ordinal)));
      this.EndSignInTelemetry(await this.SignInAsync((Func<CancellationToken, Task<ISignInInformationModel>>) (t => this.signInManager.SignInWithExistingAccountAsync(url, userEmail, userId, accountCount > 1, new CancellationToken?(t))), orgId).ConfigureAwait(false));
    }

    private void CancelSignIn() => this.EndSignInTelemetry(SignInResult.CreateError(errorCode: SignInErrorCode.UserCancelled));

    private async Task<SignInResult> SignInNewOrgInternal(string url)
    {
      SignInViewModel signInViewModel = this;
      SignInResult error = SignInResult.CreateError();
      try
      {
        string validUrl;
        if (!Okta.Authenticator.NativeApp.Extensions.NormalizeWebAddress(url, out validUrl))
        {
          signInViewModel.Logger.WriteInfoEx("Failed to validate the sign-in URL", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SignInViewModel.cs", nameof (SignInNewOrgInternal));
          return SignInResult.CreateError(Resources.ErrorMessageUrl, SignInErrorCode.InvalidUrl);
        }
        (OrganizationEnrollmentStatus status, IOktaOrganization oktaOrganization) = await signInViewModel.clientAccountManager.CheckOrganizationStatus(new Uri(validUrl)).ConfigureAwait(true);
        if (SignInViewModel.CanEnroll(status, out error))
          return await signInViewModel.SignInAsync((Func<CancellationToken, Task<ISignInInformationModel>>) (t => this.signInManager.SignInAsync(validUrl, status == OrganizationEnrollmentStatus.CanEnrollExistingOrganization, new CancellationToken?(t))), oktaOrganization.Id).ConfigureAwait(true);
      }
      catch (OktaWebException ex)
      {
        signInViewModel.Logger.WriteException("The webserver returned an error while signing in.", ex.InnerException ?? (Exception) ex);
      }
      catch (HttpRequestException ex)
      {
        string errorMessage = ex.Message.StartsWith("The remote name could not be resolved", StringComparison.OrdinalIgnoreCase) ? "The remote name could not be resolved." : ex.Message;
        signInViewModel.Logger.WriteException("The webserver returned an error while signing in.", ex.InnerException ?? (Exception) ex);
        signInViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(errorMessage, ex.HResult, nameof (SignInNewOrgInternal));
      }
      catch (Exception ex) when (!ex.IsCritical(signInViewModel.Logger))
      {
        signInViewModel.Logger.WriteException("An error occurred while signing in...", ex);
        signInViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return error;
    }

    private async Task<SignInResult> SignInAsync(
      Func<CancellationToken, Task<ISignInInformationModel>> signInMethod,
      string orgId)
    {
      SignInViewModel signInViewModel = this;
      ISignInInformationModel signInInfo = (ISignInInformationModel) null;
      bool isSignInCancelled;
      using (BrowserSignInRedirectViewModel signInRedirectVM = new BrowserSignInRedirectViewModel())
      {
        Task<ISignInInformationModel> task = signInMethod(signInRedirectVM.SignInCancellationToken);
        signInViewModel.CanGoBack = false;
        signInViewModel.RedirectedToBrowser = true;
        signInViewModel.CancelSignInTaskCommand = signInRedirectVM.CancelSignInTaskCommand;
        signInViewModel.FirePropertyChangedEvent("CanGoBack");
        signInViewModel.FirePropertyChangedEvent("RedirectedToBrowser");
        signInViewModel.FirePropertyChangedEvent("CancelSignInTaskCommand");
        signInInfo = await task.ConfigureAwait(false);
        isSignInCancelled = signInRedirectVM.IsSignInCancelled;
      }
      signInViewModel.RedirectedToBrowser = false;
      if (signInInfo == null || signInInfo.SignInFailed)
      {
        string errorMessage = string.Empty;
        ISignInInformationModel informationModel = signInInfo;
        SignInErrorCode errorCode = informationModel != null ? informationModel.ErrorCode : SignInErrorCode.Unknown;
        if (isSignInCancelled)
        {
          errorCode = SignInErrorCode.UserCancelled;
          signInViewModel.Logger.WriteInfoEx("Sign in was cancelled by the user", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SignInViewModel.cs", nameof (SignInAsync));
        }
        else
        {
          signInViewModel.Logger.WriteWarningEx(string.Format("Invalid sign in info: {0}|{1}|{2}", (object) (signInInfo == null), (object) signInInfo?.SignInFailed, (object) signInInfo?.ErrorCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SignInViewModel.cs", nameof (SignInAsync));
          errorMessage = signInInfo?.ErrorMessage;
          if (SignInViewModel.ShouldReportError(signInInfo))
            signInViewModel.AnalyticsProvider.TrackErrorWithLogs("Sign in failed - Message: " + errorMessage, sourceMethodName: nameof (SignInAsync));
        }
        return SignInResult.CreateError(errorMessage, errorCode);
      }
      UserInformation userInformation = signInInfo.GetUserInformation(orgId);
      if (userInformation == null)
      {
        signInViewModel.Logger.WriteErrorEx("GetUserInformation returned null from sign in info.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SignInViewModel.cs", nameof (SignInAsync));
        return SignInResult.CreateError(Resources.ErrorMessageGeneric);
      }
      int num = await signInViewModel.clientAccountManager.AddOrUpdateUserInformation(userInformation).ConfigureAwait(false) ? 1 : 0;
      return SignInResult.Create(signInInfo, userInformation.Login, orgId);
    }

    private static bool ShouldReportError(ISignInInformationModel signInInfo) => signInInfo == null || signInInfo.ErrorCode == SignInErrorCode.Unknown || signInInfo.ErrorCode == SignInErrorCode.UserMismatch;

    private static bool CanEnroll(OrganizationEnrollmentStatus status, out SignInResult error)
    {
      switch (status)
      {
        case OrganizationEnrollmentStatus.InvalidUrl:
          error = SignInResult.CreateError(Resources.ErrorMessageUrl, SignInErrorCode.InvalidUrl);
          return false;
        case OrganizationEnrollmentStatus.UrlNotTrusted:
          error = SignInResult.CreateError(Resources.ErrorMessageUrlNotTrusted, SignInErrorCode.InvalidUrl);
          return false;
        case OrganizationEnrollmentStatus.EnrollmentNotSupported:
          error = SignInResult.CreateError(Resources.ErrorMessageOrgNotOnIdx, SignInErrorCode.EnrollmentNotSupported);
          return false;
        case OrganizationEnrollmentStatus.CanEnrollNewOrganization:
        case OrganizationEnrollmentStatus.CanEnrollExistingOrganization:
          error = new SignInResult();
          return true;
        default:
          error = SignInResult.CreateError();
          return false;
      }
    }

    private void StartSignInTelemetry(string url, string userId)
    {
      this.StartTelemetryTracking();
      this.AnalyticsProvider.AddOperationData<AppTelemetryDataKey>(AppTelemetryDataKey.OIDCUrlConfiguration, string.IsNullOrEmpty(this.DefaultSignInURL) ? (object) "UserProvided" : (this.DefaultSignInURL.Equals(url, StringComparison.OrdinalIgnoreCase) ? (object) "AdminProvided" : (object) "UserOverride"));
      this.AnalyticsProvider.AddOperationData<AppTelemetryDataKey>(AppTelemetryDataKey.OIDCFlowType, string.IsNullOrEmpty(userId) ? (object) "NewUser" : (object) "ExistingUser");
    }

    private void EndSignInTelemetry(SignInResult result)
    {
      if (result.IsSuccess)
      {
        string str = Okta.Devices.SDK.Extensions.TelemetryExtensions.NormalizeOrgName(result.EnrollmentParameters.SignInUrl);
        this.AnalyticsProvider.AddOperationData((TelemetryDataKey.OktaUserId, (object) result.EnrollmentParameters.UserId), (TelemetryDataKey.OktaOrgId, (object) result.EnrollmentParameters.OrgId), (TelemetryDataKey.OktaOrgDomain, (object) str));
        this.EndTelemetryTracking(result, TelemetryEventStatus.Success, (string) null);
      }
      else
        this.EndTelemetryTracking(result, SignInViewModel.GetFailureStatus(result.ErrorCode), result.ErrorCode.ToString());
    }
  }
}
