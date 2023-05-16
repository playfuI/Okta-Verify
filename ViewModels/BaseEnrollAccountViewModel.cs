// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.BaseEnrollAccountViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInfo;
using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public abstract class BaseEnrollAccountViewModel : BaseViewModel
  {
    internal static int EnrollmentSuccessConfirmationDelay = 2000;

    protected BaseEnrollAccountViewModel(string orgDomain, string orgId)
    {
      this.ClientAccountManager = AppInjector.Get<IClientAccountManager>();
      this.SignInManager = AppInjector.Get<IClientSignInManager>();
      this.ApplicationInfoManager = AppInjector.Get<IApplicationInfoManager>();
      this.SignInUrl = orgDomain;
      this.OrgId = orgId;
      this.WindowsHelloSupported = this.SignInManager.CanSignInWithWindowsHello;
    }

    public bool EnrollmentInProcess { get; protected set; }

    public string SignInUrl { get; protected set; }

    public string OrgId { get; protected set; }

    public bool WindowsHelloSupported { get; }

    public virtual string EnrollingText => string.Empty;

    public string ExtraVerificationEnabledText => !this.WindowsHelloSupported ? Resources.UserVerificationAddedPasscode : Resources.UserVerificationAddedWindowsHello;

    public EnrollViewState EnrollViewState { get; private set; }

    public BaseViewModel CurrentEnrollViewModel { get; private set; }

    public string CurrentEnrollEmail { get; private set; }

    protected IClientAccountManager ClientAccountManager { get; }

    protected IClientSignInManager SignInManager { get; }

    protected IApplicationInfoManager ApplicationInfoManager { get; }

    internal void FailEnrollment(string errorMessage = null, bool isCancellation = false) => this.PublishEndEnrollmentEvent(this.GetEnrollmentFailureEventArg(this.OrgId, errorMessage, isCancellation));

    protected void PublishEndEnrollmentEvent(EnrollmentEndEventArg arg) => this.EventAggregator.GetEvent<AccountEnrollEndEvent>()?.Publish(arg);

    protected abstract Task StartEnrollment(
      EnrollmentParameters enrollmentParameters,
      bool uvRequired);

    protected abstract Task<SignInResult> StartSignIn();

    protected async Task<bool> InitiateAllowExtraVerification(string userEmail, bool uvRequired) => await this.UseInlineViewModel<bool>(EnrollViewState.AllowExtraVerification, (AsyncInlineOperationViewModel<bool>) new AllowWindowsHelloViewModel(userEmail, this.WindowsHelloSupported, uvRequired)).ConfigureAwait(true);

    protected void UpdateCurrentState(EnrollViewState viewState, BaseViewModel viewModel)
    {
      this.CurrentEnrollViewModel = viewModel;
      this.EnrollViewState = viewState;
      this.FirePropertyChangedEvent("CurrentEnrollViewModel");
      this.FirePropertyChangedEvent("EnrollViewState");
    }

    protected async Task<bool> InitiateWindowsHelloConfiguration(
      string enrollEmail,
      bool uvRequired)
    {
      bool flag;
      using (ConfigureWindowsHelloViewModel viewModel = new ConfigureWindowsHelloViewModel(enrollEmail, uvRequired))
        flag = await this.UseInlineViewModel<bool>(EnrollViewState.ConfigureWindowsHello, (AsyncInlineOperationViewModel<bool>) viewModel).ConfigureAwait(true);
      return flag;
    }

    protected async Task<bool> CanUseBiometrics(string userEmail, bool uvRequired)
    {
      if (!this.WindowsHelloSupported)
        return false;
      bool flag = this.SignInManager.CheckIfWindowsHelloSignInConfigured();
      if (!flag)
        flag = await this.InitiateWindowsHelloConfiguration(userEmail, uvRequired).ConfigureAwait(true);
      if (flag)
        flag = await this.InitiateAllowExtraVerification(userEmail, uvRequired).ConfigureAwait(true);
      return flag;
    }

    protected void InitializeEnrollment()
    {
      this.FireViewModelChangedEvent();
      this.InitializeEnrollmentFlows().AsBackgroundTask("starting " + this.GetType().Name + " enrollment", this.Logger, this.AnalyticsProvider);
    }

    protected async Task<TResult> UseInlineViewModel<TResult>(
      EnrollViewState viewState,
      AsyncInlineOperationViewModel<TResult> model,
      BaseViewModel parentViewModel = null)
    {
      this.UpdateCurrentState(viewState, parentViewModel ?? (BaseViewModel) model);
      return await model?.ResultTask;
    }

    protected async Task<SignInResult> SignIn(
      EnrollViewState viewState,
      SignInViewModel viewModel,
      BaseViewModel parentViewModel = null)
    {
      SignInResult signInResult = await this.UseInlineViewModel<SignInResult>(viewState, (AsyncInlineOperationViewModel<SignInResult>) viewModel, parentViewModel).ConfigureAwait(false);
      if (signInResult.IsSuccess)
      {
        this.OrgId = signInResult.EnrollmentParameters.OrgId;
        this.CurrentEnrollEmail = signInResult.EnrollmentParameters.UserEmail;
      }
      return signInResult;
    }

    protected virtual void HandleFailedSignIn(SignInErrorCode errorCode, string errorMessage = null)
    {
      bool flag = errorCode == SignInErrorCode.Unknown && string.IsNullOrEmpty(errorMessage);
      bool isCancellation = errorCode == SignInErrorCode.UserCancelled || errorCode == SignInErrorCode.UserAbandoned;
      this.FailEnrollment(flag ? Resources.ErrorMessageGeneric : errorMessage, isCancellation);
    }

    protected virtual EnrollmentEndEventArg GetEnrollmentFailureEventArg(
      string orgId,
      string errorMessage,
      bool isCancellation)
    {
      return EnrollmentEndEventArg.AsEnrollmentFailure(orgId, errorMessage, isCancellation);
    }

    protected virtual async Task InitializeEnrollmentFlows()
    {
      BaseEnrollAccountViewModel accountViewModel = this;
      SignInResult signInResult = await accountViewModel.StartSignIn().ConfigureAwait(true);
      EnrollmentParameters enrollParams;
      if (!signInResult.IsSuccess)
      {
        accountViewModel.HandleFailedSignIn(signInResult.ErrorCode, signInResult.ErrorMessage);
        enrollParams = new EnrollmentParameters();
      }
      else
      {
        enrollParams = signInResult.EnrollmentParameters;
        bool uvRequired = await accountViewModel.ClientAccountManager.IsAccountUserVerificationRequired(enrollParams.OrgId, enrollParams.SignInUrl, enrollParams.AccessToken, false).ConfigureAwait(true);
        bool inRemote = accountViewModel.ApplicationInfoManager.CheckIfInRemoteSession();
        (bool flag, string errorMessage) = await accountViewModel.ShouldProceedEnrolling(uvRequired, inRemote, enrollParams.UserId).ConfigureAwait(true);
        if (flag)
        {
          await accountViewModel.StartEnrollment(enrollParams, uvRequired).ConfigureAwait(true);
          enrollParams = new EnrollmentParameters();
        }
        else
        {
          accountViewModel.Logger.WriteInfoEx(string.Format("Cancelling the enrollment... UV Required: {0} - In remote session: {1}", (object) uvRequired, (object) inRemote), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\BaseEnrollAccountViewModel.cs", nameof (InitializeEnrollmentFlows));
          accountViewModel.FailEnrollment(errorMessage);
          enrollParams = new EnrollmentParameters();
        }
      }
    }

    protected virtual async Task<(bool ProceedEnrolling, string DenialReason)> ShouldProceedEnrolling(
      bool uvRequired,
      bool inRemoteSession,
      string userId)
    {
      if (!uvRequired)
        return (true, string.Empty);
      if (inRemoteSession)
        return (false, ResourceExtensions.ExtractCompositeResource(Resources.ErrorMessageCannotEnrollInRemoteSession));
      if (this.SignInManager.CanSignInWithWindowsHello)
        return (true, string.Empty);
      int num = await this.UseInlineViewModel<bool>(EnrollViewState.DeviceNotSupported, (AsyncInlineOperationViewModel<bool>) new DeviceNotSupportedViewModel()).ConfigureAwait(true) ? 1 : 0;
      return (false, string.Empty);
    }
  }
}
