// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Bindings.BindingEventHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Authenticator.NativeApp.ViewModels;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authentication;
using Okta.Devices.SDK.Authenticator;
using Okta.Devices.SDK.Exceptions;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Extensions.User;
using Okta.Devices.SDK.WebClient.Errors;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Bindings
{
  public class BindingEventHandler : IBindingEventHandler
  {
    private readonly ILogger logger;
    private readonly IConfigurationManager configurationManager;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly IClientStorageManager storageManager;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IClientAccountManager accountManager;
    private readonly IAuthenticationRequestManager authenticationRequestManager;
    private readonly IAccountStateManager accountStateManager;
    private readonly IDeviceConfigurationManager deviceConfigurationManager;

    public BindingEventHandler(
      ILogger logger,
      IConfigurationManager configurationManager,
      IClientStorageManager storageManager,
      IAnalyticsProvider analyticsProvider,
      IEventAggregator eventAggregator,
      IApplicationStateMachine stateMachine,
      IClientAccountManager accountManager,
      IAuthenticationRequestManager authenticationRequestManager,
      IAccountStateManager accountStateManager,
      IDeviceConfigurationManager deviceConfigurationManager)
    {
      this.logger = logger;
      this.configurationManager = configurationManager;
      this.storageManager = storageManager;
      this.analyticsProvider = analyticsProvider;
      this.eventAggregator = eventAggregator;
      this.stateMachine = stateMachine;
      this.accountManager = accountManager;
      this.authenticationRequestManager = authenticationRequestManager;
      this.accountStateManager = accountStateManager;
      this.deviceConfigurationManager = deviceConfigurationManager;
    }

    Func<BindingEventPayload, Task<bool>> IBindingEventHandler.OnConfirmationRequired => new Func<BindingEventPayload, Task<bool>>(this.OnRequestConfirmationNeeded);

    Func<BindingEventPayload, Task<(IDeviceEnrollment, bool)>> IBindingEventHandler.OnEnrollmentNeeded => new Func<BindingEventPayload, Task<(IDeviceEnrollment, bool)>>(this.OnEnrollmentRequested);

    Func<BindingEventPayload, Task<IDeviceEnrollment>> IBindingEventHandler.OnEnrollmentSelectionNeeded => new Func<BindingEventPayload, Task<IDeviceEnrollment>>(this.OnEnrollmentSelectionRequested);

    Action<BindingEventType, BindingEventPayload> IBindingEventHandler.OnBindingEventUpdate => new Action<BindingEventType, BindingEventPayload>(this.OnBindingEventUpdate);

    Func<JustInTimeEnrollmentType> IBindingEventHandler.OnCheckCanEnroll => new Func<JustInTimeEnrollmentType>(this.OnCheckCanEnroll);

    private void OnBindingEventUpdate(BindingEventType eventType, BindingEventPayload payload)
    {
      this.logger.WriteInfoEx(string.Format("Received a binding event update callback for {0}|{1} from {2}|{3}|{4}", (object) eventType, (object) payload.FailureReason, (object) payload.OrgUrl, (object) payload.OrgId, (object) payload.CorrelationId), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnBindingEventUpdate));
      switch (eventType)
      {
        case BindingEventType.RequestProcessInProgress:
          this.authenticationRequestManager.StartOrUpdateAuthentication(payload);
          this.ShowSignInSpinner(true);
          break;
        case BindingEventType.RequestProcessSucceeded:
          this.HandleBindingRequestResult(payload, true);
          break;
        case BindingEventType.RequestProcessWithEnrollmentSucceeded:
          this.NotifyEnrollmentVerificationUpdate(payload, true).AsBackgroundTask("user verification credential update success", this.logger, this.analyticsProvider);
          break;
        case BindingEventType.RequestProcessFailed:
          if (payload.FailureReason == OktaBindingFailureReason.ServerOriginMismatch && this.deviceConfigurationManager.IsPhishingResistanceMessageEnabled)
            this.HandleOriginMismatch(payload);
          else
            this.HandleBindingRequestResult(payload, false);
          this.HandleSignInError(payload);
          break;
        case BindingEventType.RequestProcessWithEnrollmentFailed:
          this.ShowSignInSpinner(false);
          this.HandleSignInError(payload);
          this.NotifyEnrollmentVerificationUpdate(payload, false).AsBackgroundTask("user verification credential update failure", this.logger, this.analyticsProvider);
          break;
        case BindingEventType.RequestProcessNotStarted:
          this.HandleSignInError(payload);
          break;
        default:
          this.logger.WriteWarningEx(string.Format("Binding event {0} will not be processed.", (object) eventType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnBindingEventUpdate));
          break;
      }
    }

    private async Task<bool> OnRequestConfirmationNeeded(BindingEventPayload payload)
    {
      this.logger.WriteInfoEx("Received a confirmation request for " + payload.OrgUrl + "|" + payload.OrgId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnRequestConfirmationNeeded));
      AuthenticationRequestContext authenticationRequestContext = await this.authenticationRequestManager.StartOrUpdateAuthenticationWithUserInteraction(payload).ConfigureAwait(false);
      IOktaAccount account;
      if (this.accountManager.TryGetAccount(payload.EnrollmentId, out account))
        return await this.FireRequestDialogDisplayWithResult(DialogViewType.SignInConfirmation, payload, account).ConfigureAwait(false);
      this.logger.WriteErrorEx("Account with id " + payload.EnrollmentId + " does not exist, rejecting authentication request.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnRequestConfirmationNeeded));
      return false;
    }

    private async Task<(IDeviceEnrollment enrollment, bool isNewEnrollment)> OnEnrollmentRequested(
      BindingEventPayload payload)
    {
      try
      {
        this.logger.WriteInfoEx(string.Format("Received an enrollment request for {0}|{1}. Hint provided: {2}", (object) payload.OrgUrl, (object) payload.OrgId, (object) !string.IsNullOrEmpty(payload.UserLoginHint)), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentRequested));
        AuthenticationRequestContext authenticationRequestContext = await this.authenticationRequestManager.StartOrUpdateAuthenticationWithUserInteraction(payload).ConfigureAwait(false);
        IOktaAccount oktaAccount = await this.TrySelectAccount(payload).ConfigureAwait(false);
        if (oktaAccount != null)
        {
          this.logger.WriteInfoEx("Found existing enrollment, returning account " + oktaAccount.AccountId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentRequested));
          return (oktaAccount.Enrollment, false);
        }
        return ((await this.accountManager.InvokeEnrollmentFlow(EnrollmentStartEventArg.AsJITEnrollmentRequest(payload.OrgUrl, payload.OrgId, payload.AppName), true).ConfigureAwait(false)).Enrollment, true);
      }
      catch (TaskCanceledException ex)
      {
        this.logger.WriteWarningEx("Cancellation received while processing the enrollment for " + payload.OrgId + ": " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentRequested));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteErrorEx("An error occurred while processing the enrollment for " + payload.OrgId + ": " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentRequested));
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return ();
    }

    private async Task<IDeviceEnrollment> OnEnrollmentSelectionRequested(BindingEventPayload payload)
    {
      try
      {
        this.logger.WriteInfoEx(string.Format("Received an enrollment selection request for {0}|{1}. UserId: {2}, Hint provided: {3}", (object) payload.OrgUrl, (object) payload.OrgId, (object) payload.UserId, (object) !string.IsNullOrEmpty(payload.UserLoginHint)), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentSelectionRequested));
        IOktaAccount oktaAccount = await this.TrySelectAccount(payload).ConfigureAwait(false);
        this.logger.WriteInfoEx("AccountId selected: " + oktaAccount?.AccountId, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentSelectionRequested));
        return oktaAccount?.Enrollment;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteErrorEx("An error occurred while attempting to pick an enrollment for " + payload.OrgId + ": " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (OnEnrollmentSelectionRequested));
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        return (IDeviceEnrollment) null;
      }
    }

    private async Task<IOktaAccount> TrySelectAccount(BindingEventPayload payload)
    {
      bool noUserId = string.IsNullOrWhiteSpace(payload.UserId);
      bool noLoginHint = string.IsNullOrWhiteSpace(payload.UserLoginHint);
      if (noUserId & noLoginHint)
      {
        this.logger.WriteInfoEx("No login hint provided, returning default account.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
        return await this.accountManager.GetDefaultAccount(payload.OrgId, pickOneIfNoDefaultSet: true).ConfigureAwait(false);
      }
      IList<IOktaAccount> source1 = await this.accountManager.GetAccounts((Func<IOktaAccount, bool>) (a => a.OrgId == payload.OrgId)).ConfigureAwait(false);
      if (source1 == null || source1.Count == 0)
      {
        this.logger.WriteInfoEx("No accounts for organization " + payload.OrgId + " found.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
        return (IOktaAccount) null;
      }
      if (!noUserId)
      {
        this.logger.WriteInfoEx("Selecting the account by userId " + payload.UserId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
        return source1.FirstOrDefault<IOktaAccount>((Func<IOktaAccount, bool>) (a => a.UserId.Equals(payload.UserId, StringComparison.Ordinal)));
      }
      if (noLoginHint)
      {
        this.logger.WriteWarningEx("Received request with userId but no matching account and no login hint.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
        this.analyticsProvider.TrackErrorWithLogsAndAppData("Login hint with user Id but no user login hint.", sourceMethodName: nameof (TrySelectAccount));
        return (IOktaAccount) null;
      }
      if (UserExtensions.IsEmail(payload.UserLoginHint, out int _))
      {
        this.logger.WriteInfoEx("Selecting account by full email as login hint.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
        return source1.FirstOrDefault<IOktaAccount>((Func<IOktaAccount, bool>) (a => a.UserLogin.Equals(payload.UserLoginHint, StringComparison.OrdinalIgnoreCase)));
      }
      this.logger.WriteInfoEx("Selecting account by short name as login hint.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
      IEnumerable<IOktaAccount> source2 = source1.Where<IOktaAccount>((Func<IOktaAccount, bool>) (a => a.IsShortNameMatch(payload.UserLoginHint)));
      if (source2.Count<IOktaAccount>() < 2)
      {
        this.logger.WriteInfoEx("There is no more than one existing account matching the short name login hint.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
        return source2.FirstOrDefault<IOktaAccount>();
      }
      this.logger.WriteWarningEx("Multiple matches for provided short name login hint, falling back to default", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
      foreach (IOktaAccount oktaAccount in source2)
      {
        if (this.accountManager.IsAccountSetAsDefault(oktaAccount.AccountId))
        {
          this.logger.WriteInfoEx("Selecting default account matching short name login hint.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (TrySelectAccount));
          return oktaAccount;
        }
      }
      return (IOktaAccount) null;
    }

    private JustInTimeEnrollmentType OnCheckCanEnroll() => this.configurationManager.GetEnrollmentTypeFromRegistry(this.logger);

    private async Task NotifyEnrollmentVerificationUpdate(
      BindingEventPayload payload,
      bool verified)
    {
      this.authenticationRequestManager.EndAuthentication(payload, verified);
      this.logger.WriteInfoEx(string.Format("Account verified: {0}", (object) verified), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (NotifyEnrollmentVerificationUpdate));
      if (!verified)
        return;
      int num = await this.FireRequestDialogDisplayWithResult(DialogViewType.AccountVerified, payload).ConfigureAwait(false) ? 1 : 0;
    }

    private async Task<bool> FireRequestDialogDisplayWithResult(
      DialogViewType viewType,
      BindingEventPayload payload,
      IOktaAccount account = null)
    {
      DialogViewStateEvent viewStateEvent = this.eventAggregator.GetEvent<DialogViewStateEvent>();
      if (viewStateEvent == null)
      {
        this.logger.WriteWarningEx(string.Format("Notification for {0} will not be sent, failed to get DialogViewStateEvent.", (object) viewType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (FireRequestDialogDisplayWithResult));
        return false;
      }
      UserInformation userInformation = await this.storageManager.Store.TryGetDataAsync<UserInformation>(payload.UserId).ConfigureAwait(false);
      if (userInformation == null)
      {
        this.logger.WriteWarningEx(string.Format("Notification for {0} will not be sent, failed to get user information.", (object) viewType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (FireRequestDialogDisplayWithResult));
        return false;
      }
      BindingInformationModel payload1 = new BindingInformationModel()
      {
        AppName = payload.AppName,
        UserEmail = userInformation.Login,
        AccountId = payload.EnrollmentId,
        RequestReferrer = payload.RequestReferrer,
        Logo = account?.Logo
      };
      return await viewStateEvent.RequestDialogDisplayWithResult<BindingInformationModel>(this.eventAggregator, viewType, payload1).ConfigureAwait(false);
    }

    private void ShowSignInSpinner(bool showInProgress)
    {
      DialogViewStateEvent dialogViewStateEvent = this.eventAggregator.GetEvent<DialogViewStateEvent>();
      if (dialogViewStateEvent == null)
        return;
      if (showInProgress)
        dialogViewStateEvent.RequestDialogDisplay<string>(DialogViewType.InProgress, Resources.SigningInProgress);
      else
        dialogViewStateEvent.RequestDialogClosure(DialogViewType.InProgress);
    }

    private void HandleOriginMismatch(BindingEventPayload payload)
    {
      this.authenticationRequestManager.EndAuthentication(payload, false);
      this.ShowSignInSpinner(false);
      try
      {
        this.eventAggregator.GetEvent<ViewStateRequestEvent>().Publish(new ViewStateRequest(MainViewType.OriginMismatch));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when handling the server origin mismatch. result", ex, this.analyticsProvider);
      }
    }

    private void HandleBindingRequestResult(BindingEventPayload payload, bool succeeded)
    {
      AuthenticationRequestContext authResultContext = this.authenticationRequestManager.EndAuthentication(payload, succeeded);
      this.ShowSignInSpinner(false);
      try
      {
        this.EnsureLifecycleStateUpdated(payload.EnrollmentId, succeeded, payload.OktaApiErrorCode);
        this.EnsureInvalidatedKeysStateUpdated(authResultContext);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when handling the binding auth. result", ex, this.analyticsProvider);
      }
    }

    private void HandleSignInError(BindingEventPayload payload)
    {
      bool flag = false;
      if (payload.FailureReason == OktaBindingFailureReason.SandboxUpdateRequired)
        this.HandleSandboxFailure().AsBackgroundTask("sandbox failure handling", this.logger, this.analyticsProvider);
      else if (payload.FailureReason == OktaBindingFailureReason.ClientProfileInactive)
        this.eventAggregator.GetEvent<UserSessionChangedEvent>()?.Publish(UserSessionChangedEventType.InactiveUserProfile);
      else if (payload.FailureReason == OktaBindingFailureReason.ClientTimeSkew)
        flag = true;
      if (!flag)
        return;
      this.eventAggregator.GetEvent<MainViewStateEvent>()?.Publish(new MainViewState(MainViewType.Accounts, (INotifyPropertyChanged) new AccountListViewModel()));
      this.eventAggregator.GetEvent<BannerNotificationEvent>()?.Publish(new BannerNotification(BannerType.Error, Resources.ErrorMessageGeneric));
      this.stateMachine.TransitionTo(AppStateRequestType.BringToFocus);
    }

    private async Task HandleSandboxFailure()
    {
      this.logger.WriteInfoEx("Sandbox is in a bad state, shutting down application.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingEventHandler.cs", nameof (HandleSandboxFailure));
      await Task.Delay(1000).ConfigureAwait(false);
      this.stateMachine.TransitionTo(ComputingStateType.ShuttingDown);
    }

    private bool EnsureInvalidatedKeysStateUpdated(AuthenticationRequestContext authResultContext)
    {
      if ((!authResultContext.HasUserInteraction ? 0 : (authResultContext.KeyInteractions.Count > 0 ? 1 : 0)) == 0)
        return false;
      IEnumerable<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>> source = authResultContext.KeyInteractions.Where<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>>((Func<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>, bool>) (k => k.Value.Error == PlatformErrorCode.KeyNotFound));
      if (source.Count<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>>() == 0)
        return this.accountStateManager.EnsureAccountStateReset(authResultContext.EnrollmentId, resetLifecycle: false);
      if (source.Count<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>>() == authResultContext.KeyInteractions.Count)
      {
        this.accountStateManager.TrackAccountKeyChanges(authResultContext.EnrollmentId, true, true);
        IOktaAccount account;
        if (this.accountManager.TryGetAccount(authResultContext.EnrollmentId, out account))
          this.eventAggregator.GetEvent<ViewStateRequestEvent>().Publish(new ViewStateRequest(MainViewType.AccountDetails, (object) account));
        return true;
      }
      if (!source.Any<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>>((Func<KeyValuePair<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>, bool>) (k => k.Key == VerificationCredentialType.UserPresence)))
        return false;
      AccountStateContext context = new AccountStateContext(authResultContext.EnrollmentId, true);
      this.accountStateManager.TrackAccountKeyChanges(authResultContext.EnrollmentId, false, true);
      this.eventAggregator.GetEvent<ViewStateRequestEvent>().Publish(new ViewStateRequest(MainViewType.UpdateWindowsHelloSettings, (object) context));
      return true;
    }

    private bool EnsureLifecycleStateUpdated(
      string accountId,
      bool succeeded,
      OktaApiErrorCode apiErrorCode)
    {
      if (succeeded)
        return this.accountStateManager.EnsureAccountStateReset(accountId, false);
      this.accountStateManager.TrackAccountApiError(accountId, apiErrorCode);
      IOktaAccount account;
      if (this.accountManager.TryGetAccount(accountId, out account))
        this.eventAggregator.GetEvent<ViewStateRequestEvent>().Publish(new ViewStateRequest(MainViewType.AccountDetails, (object) account));
      return true;
    }
  }
}
