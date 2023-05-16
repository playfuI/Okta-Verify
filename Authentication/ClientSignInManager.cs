// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.ClientSignInManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Results;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UI.Handlers;
using Okta.Authenticator.NativeApp.UI.Models;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Authenticator.NativeApp.ViewModels;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Credentials;
using Okta.Devices.SDK.Extensions;
using Okta.Oidc.Abstractions;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Provider;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class ClientSignInManager : IClientSignInManager
  {
    private const int AuthenticationFallbackCancellationOverrideSeconds = 10;
    private const int DefaultKeySize = 2048;
    private const DeviceCredentialType DefaultKeyType = DeviceCredentialType.Rsa;
    private static readonly IList<LoginPromptType> LoginPromptParameters = (IList<LoginPromptType>) new List<LoginPromptType>()
    {
      LoginPromptType.Login
    };
    private readonly ILogger logger;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IApplicationHandler applicationHandler;
    private readonly IConfigurationManager configurationManager;
    private readonly IEventAggregator eventAggregator;
    private readonly IOidcFactory oidcFactory;
    private readonly IClientStorageManager storageManager;
    private readonly IAuthenticationRequestManager authenticationRequestManager;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IWindowActivationHandler windowHandler;
    private readonly Lazy<ICredentialOptions> credentialOptionsInitializer;
    private readonly IDictionary<string, DateTime> onAuthenticationRequiredCancellationMap;
    private string defaultSignInUrl;
    private bool? canSignInWithWindowsHello;
    private ICredentialEventHandler credentialEventHandler;

    public ClientSignInManager(
      IAnalyticsProvider analytics,
      IApplicationHandler application,
      IConfigurationManager configurationManager,
      ILogger logger,
      IClientStorageManager storageManager,
      IEventAggregator aggregator,
      IOidcFactory oidcFactory,
      IAuthenticationRequestManager authenticationRequestManager,
      IApplicationStateMachine stateMachine,
      IWindowActivationHandler windowHandler)
    {
      this.applicationHandler = application;
      this.analyticsProvider = analytics;
      this.configurationManager = configurationManager;
      this.logger = logger;
      this.eventAggregator = aggregator;
      this.oidcFactory = oidcFactory;
      this.storageManager = storageManager;
      this.authenticationRequestManager = authenticationRequestManager;
      this.stateMachine = stateMachine;
      this.windowHandler = windowHandler;
      this.onAuthenticationRequiredCancellationMap = (IDictionary<string, DateTime>) new Dictionary<string, DateTime>();
      this.credentialOptionsInitializer = new Lazy<ICredentialOptions>(new Func<ICredentialOptions>(this.InitializeCredentialOptions), true);
    }

    public string DefaultSignInUrl
    {
      get
      {
        if (this.defaultSignInUrl == null)
          this.defaultSignInUrl = this.configurationManager.TryGetRegistryConfig<string>(this.logger, "SignInUrl", string.Empty);
        return this.defaultSignInUrl;
      }
    }

    public ICredentialOptions CredentialOptions => this.credentialOptionsInitializer.Value;

    public bool CanSignInWithWindowsHello
    {
      get
      {
        if (!this.canSignInWithWindowsHello.HasValue)
          this.canSignInWithWindowsHello = new bool?(!this.CredentialOptions.KeyCreationFlags.HasFlag((System.Enum) AuthenticatorKeyCreationFlags.DisableWindowsHello) && WindowsHelloCredentialManager.CheckIsWindowsHelloSupported() == WindowsHelloCredentialManager.WindowsHelloSupportStatus.Supported);
        return this.canSignInWithWindowsHello.Value;
      }
    }

    public bool CheckIfWindowsHelloSignInConfigured()
    {
      bool? nullable = WindowsHelloCredentialManager.CheckIsWindowsHelloConfigured();
      bool flag = true;
      return nullable.GetValueOrDefault() == flag & nullable.HasValue;
    }

    public async Task<ISignInInformationModel> SignInAsync(
      string url,
      bool multiAccount,
      CancellationToken? cancellationToken = null)
    {
      return await this.SignInAsync(url, (string) null, multiAccount, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ISignInInformationModel> SignInWithExistingAccountAsync(
      string url,
      string loginHint,
      string userId,
      bool multiAccount,
      CancellationToken? cancellationToken = null)
    {
      loginHint.EnsureNotNullOrBlank(loginHint);
      userId.EnsureNotNullOrBlank(userId);
      ISignInInformationModel informationModel = await this.SignInAsync(url, loginHint, multiAccount, cancellationToken).ConfigureAwait(false);
      if (informationModel.UserId == null)
        return (ISignInInformationModel) ClientSignInManager.CreateErrorResponse(Resources.ErrorMessageGeneric);
      if (informationModel.UserId.Equals(userId, StringComparison.Ordinal))
        return informationModel;
      this.logger.WriteWarningEx("Signed in user " + informationModel.UserId + " does not match user on account with id " + userId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInWithExistingAccountAsync));
      return (ISignInInformationModel) ClientSignInManager.CreateErrorResponse(Resources.ErrorMessageWrongUser, SignInErrorCode.UserMismatch);
    }

    private static SignInInformationModel CreateErrorResponse(
      string errorMessage = null,
      SignInErrorCode errorCode = SignInErrorCode.Unknown)
    {
      return new SignInInformationModel()
      {
        SignInFailed = true,
        ErrorMessage = errorMessage ?? Resources.ErrorMessageGeneric,
        ErrorCode = errorCode
      };
    }

    private async Task<CredentialUsageContext> OnUserInteractionRequested(
      CredentialEventType eventType,
      CredentialEventPayload payload)
    {
      this.logger.WriteInfoEx(string.Format("Received a user interaction requested callback of type {0} for {1}.", (object) eventType, (object) payload.RequestDomain), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnUserInteractionRequested));
      string requester = payload.RequestDomain ?? string.Empty;
      if (eventType != CredentialEventType.KeyAccess && eventType != CredentialEventType.KeyCreation)
        this.logger.WriteWarningEx(string.Format(" Received an unexpected event: {0} from {1}", (object) eventType, (object) requester), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnUserInteractionRequested));
      string description = (string) null;
      string message;
      if (eventType == CredentialEventType.KeyCreation)
      {
        this.stateMachine.TransitionTo(AppStateRequestType.BringToFocus, new ApplicationStateContext()
        {
          ForceActivation = true
        });
        message = Resources.KeyCreationUsageContext.CultureFormat((object) requester);
        description = Resources.KeyUsageDescription.CultureFormat((object) requester);
      }
      else
      {
        AuthenticationRequestContext authenticationRequestContext = await this.authenticationRequestManager.StartOrUpdateAuthenticationWithUserInteraction(payload).ConfigureAwait(false);
        message = Resources.KeyLoadUsageContext.CultureFormat((object) requester);
      }
      NativeWindowModel mainWindow = this.windowHandler.MainWindow;
      CredentialUsageContext credentialUsageContext = new CredentialUsageContext(mainWindow != null ? mainWindow.Handle : IntPtr.Zero, message, (IApplicationInteraction) this.applicationHandler, description);
      requester = (string) null;
      description = (string) null;
      return credentialUsageContext;
    }

    [AnalyticsScenario(ScenarioType.FastPassAuthentication)]
    private void OnCredentialEventUpdate(
      CredentialEventType eventType,
      CredentialEventPayload payload)
    {
      this.logger.WriteInfoEx(string.Format("Received a credential event update callback of type {0} for {1}.", (object) eventType, (object) payload.RequestDomain), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnCredentialEventUpdate));
      this.OnCredentialEventUpdateAsync(eventType, payload).AsBackgroundTask("Handle credential updates");
    }

    private async Task OnCredentialEventUpdateAsync(
      CredentialEventType eventType,
      CredentialEventPayload payload)
    {
      if (eventType == CredentialEventType.KeyAccess)
      {
        if (payload.IsFinal)
          this.authenticationRequestManager.TryUpdateAuthentication(payload);
        else
          this.authenticationRequestManager.StartOrUpdateAuthentication(payload);
      }
      this.logger.WriteInfoEx(string.Format("Credential event {0} received: Final: {1} Status: {2}", (object) eventType, (object) payload.IsFinal, (object) payload.ErrorCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnCredentialEventUpdateAsync));
      if (payload.ErrorCode == PlatformErrorCode.None)
        return;
      switch (payload.ErrorCode)
      {
        case PlatformErrorCode.UserCancelled:
          this.logger.WriteInfoEx("Sign in cancelled by the user", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnCredentialEventUpdateAsync));
          break;
        default:
          this.logger.WriteWarningEx(string.Format("Failed to process credential event {0}|{1} for {2}: {3}", (object) eventType, (object) payload.CredentialType, (object) payload.RequestDomain, (object) payload.ErrorCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnCredentialEventUpdateAsync));
          break;
      }
    }

    [AnalyticsScenario(ScenarioType.AccountUpdate)]
    private async Task<(IDeviceToken, bool)> OnAuthenticationRequired(
      CredentialEventType type,
      AuthenticationRequiredEventPayload payload)
    {
      this.logger.WriteInfoEx(string.Format("Received an authentication requried callback of type {0} for {1}|{2}.", (object) type, (object) payload?.UserId, (object) payload?.RequestDomain), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnAuthenticationRequired));
      if (type != CredentialEventType.SilentAuthenticationFailed)
      {
        string str = string.Format("Received authentication required callback with unexpected type {0}.", (object) type);
        this.logger.WriteWarningEx(str, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnAuthenticationRequired));
        this.analyticsProvider.TrackErrorWithLogs(str, sourceMethodName: nameof (OnAuthenticationRequired));
        return ((IDeviceToken) null, false);
      }
      if (string.IsNullOrEmpty(payload?.RequestDomain))
      {
        string str = "Received authentication required callback with empty domain.";
        this.logger.WriteWarningEx(str, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnAuthenticationRequired));
        this.analyticsProvider.TrackErrorWithLogs(str, sourceMethodName: nameof (OnAuthenticationRequired));
        return ((IDeviceToken) null, false);
      }
      if (!this.AllowOidcFallback(type, payload))
      {
        this.logger.WriteInfoEx(string.Format("Ignoring authentication required callback for error {0}", (object) payload.ErrorCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnAuthenticationRequired));
        return ((IDeviceToken) null, true);
      }
      UserInformation userInformation = (UserInformation) null;
      BrowserSignInRedirectViewModel redirectVM = (BrowserSignInRedirectViewModel) null;
      try
      {
        if (!string.IsNullOrEmpty(payload.UserId))
        {
          userInformation = await this.storageManager.Store.TryGetDataAsync<UserInformation>(payload.UserId).ConfigureAwait(false);
          if (userInformation != null && !userInformation.Id.Equals(payload.UserId, StringComparison.Ordinal))
          {
            this.logger.WriteWarningEx("User id " + payload.UserId + " in AuthenticationRequiredEventPayload does not match user id " + userInformation.Id + " on the account.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnAuthenticationRequired));
            userInformation = (UserInformation) null;
          }
        }
        redirectVM = new BrowserSignInRedirectViewModel();
        MainViewStateEvent mainViewEvent = this.eventAggregator.GetEvent<MainViewStateEvent>();
        MainViewState mainViewState = new MainViewState(MainViewType.BrowserRedirect, (INotifyPropertyChanged) redirectVM);
        bool multiAccount = false;
        Task<ISignInInformationModel> task = userInformation == null ? this.SignInAsync(payload.RequestDomain, multiAccount, new CancellationToken?(redirectVM.SignInCancellationToken)) : this.SignInWithExistingAccountAsync(payload.RequestDomain, userInformation.Login, userInformation.Id, multiAccount, new CancellationToken?(redirectVM.SignInCancellationToken));
        mainViewEvent?.Publish(mainViewState);
        ISignInInformationModel informationModel = await task.ConfigureAwait(true);
        mainViewState.Activate = false;
        mainViewEvent?.Publish(mainViewState);
        if (informationModel != null && !informationModel.SignInFailed)
          return ((IDeviceToken) new OktaAccessToken(informationModel.AccessToken), false);
        this.eventAggregator.GetEvent<BannerNotificationEvent>()?.Publish(new BannerNotification(BannerType.Error, Resources.ErrorMessageSignInFailed));
        this.logger.WriteWarningEx(string.Format("Authentication required callback failed sign in: {0}:{1}.", (object) informationModel?.ErrorCode, (object) informationModel?.ErrorMessage), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (OnAuthenticationRequired));
        return ((IDeviceToken) null, informationModel.ErrorCode == SignInErrorCode.UserCancelled || informationModel.ErrorCode == SignInErrorCode.UserMismatch || informationModel.ErrorCode == SignInErrorCode.AccessDenied);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("An error occurred while handling the authentication callback", ex);
      }
      finally
      {
        redirectVM?.Dispose();
      }
      return ((IDeviceToken) null, false);
    }

    private async Task<ISignInInformationModel> SignInAsync(
      string url,
      string loginHint,
      bool multiAccount,
      CancellationToken? cancellationToken = null)
    {
      string validURL;
      if (!Okta.Authenticator.NativeApp.Extensions.NormalizeWebAddress(url, out validURL))
        return (ISignInInformationModel) ClientSignInManager.CreateErrorResponse(Resources.ErrorMessageUrl, SignInErrorCode.InvalidUrl);
      IOktaClient oktaClient = this.oidcFactory.GetOidcClient(validURL);
      this.logger.WriteDebugEx("Getting access token.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInAsync));
      (LoginResult, string, SignInErrorCode) clientSignIn = await this.SignInClientAsync(oktaClient, loginHint, multiAccount, cancellationToken).ConfigureAwait(false);
      if (clientSignIn.Item1 == null || clientSignIn.Item1.IsError)
        return (ISignInInformationModel) ClientSignInManager.CreateErrorResponse(clientSignIn.Item2, clientSignIn.Item3);
      this.logger.WriteDebugEx("Getting user info.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInAsync));
      (UserInfoResult, string) valueTuple = await this.GetUserInfoAsync(oktaClient, clientSignIn.Item1).ConfigureAwait(false);
      if (valueTuple.Item1 == null || valueTuple.Item1.IsError)
        return (ISignInInformationModel) ClientSignInManager.CreateErrorResponse(valueTuple.Item2);
      return (ISignInInformationModel) new SignInInformationModel()
      {
        UserId = valueTuple.Item1.Claims.GetValueOrDefault("sub"),
        UserClaims = valueTuple.Item1.Claims,
        SignInURL = validURL,
        AccessToken = clientSignIn.Item1.AccessToken,
        SignInEmail = valueTuple.Item1.Claims.GetValueOrDefault("preferred_username")
      };
    }

    private async Task<(LoginResult Result, string Error, SignInErrorCode Code)> SignInClientAsync(
      IOktaClient oktaClient,
      string loginHint = null,
      bool multiAccount = false,
      CancellationToken? externalCancellationToken = null)
    {
      LoginResult loginResult = (LoginResult) null;
      string errorMessage = string.Empty;
      SignInErrorCode code = SignInErrorCode.Unknown;
      LoginOptions loginOptions = new LoginOptions()
      {
        LoginHint = loginHint
      };
      if (multiAccount)
        loginOptions.LoginPromptOptions = ClientSignInManager.LoginPromptParameters;
      if (oktaClient == null)
        return (loginResult, errorMessage, code);
      CancellationTokenSource signInCancellation = (CancellationTokenSource) null;
      int nRetries = 0;
      Random rnd = new Random();
      bool shouldRetry;
      do
      {
        shouldRetry = false;
        errorMessage = string.Empty;
        try
        {
          CancellationToken? nullable = externalCancellationToken;
          if (!externalCancellationToken.HasValue)
          {
            signInCancellation = new CancellationTokenSource();
            nullable = new CancellationToken?(signInCancellation.Token);
          }
          loginResult = await oktaClient.LoginAsync(loginOptions, nullable.Value).ConfigureAwait(false);
          if (nRetries > 0)
            this.logger.WriteDebugEx(string.Format("Got OIDC loopback listener after {0} retries", (object) nRetries), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
        }
        catch (ArgumentNullException ex)
        {
          errorMessage = Resources.ErrorMessageUrl;
          code = SignInErrorCode.InvalidUrl;
          this.logger.WriteExceptionAsWarning("Failed sign in with ArgumentNullException", (Exception) ex);
        }
        catch (ArgumentException ex)
        {
          errorMessage = Resources.ErrorMessageUrl;
          code = SignInErrorCode.InvalidUrl;
          this.logger.WriteExceptionAsWarning("Failed sign in with ArgumentException", (Exception) ex);
        }
        catch (ProviderException ex)
        {
          errorMessage = Resources.ErrorMessageUrl;
          code = SignInErrorCode.InvalidUrl;
          this.logger.WriteExceptionAsWarning("Failed sign in with ProviderException", (Exception) ex);
        }
        catch (TaskCanceledException ex)
        {
          this.logger.WriteWarningEx("LoginAsync was cancelled with cancellation token: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
          code = SignInErrorCode.Cancelled;
        }
        catch (HttpListenerException ex)
        {
          this.logger.WriteWarningEx("LoginAsync failed with an http listener exception: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
          errorMessage = Resources.ErrorMessageGeneric;
          code = SignInErrorCode.Unknown;
          shouldRetry = true;
        }
        catch (InvalidOperationException ex)
        {
          if (ex.Message.StartsWith((string) (StringEnum) ErrorType.DiscoveryUnknownError, StringComparison.OrdinalIgnoreCase))
          {
            errorMessage = Resources.ErrorMessageUrl;
            code = SignInErrorCode.InvalidUrl;
            this.logger.WriteWarningEx("Failed sign on in LoginAsync with invalid operation exception, most likely endpoint cannot be reached.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
          }
          else
          {
            errorMessage = Resources.ErrorMessageGeneric;
            this.logger.WriteException("Failed sign in on LoginAsync with invalid operation exception.", (Exception) ex);
            this.analyticsProvider.TrackErrorWithLogsAndAppData((Exception) ex);
          }
        }
        catch (Exception ex) when (!ex.IsCritical(this.logger))
        {
          errorMessage = Resources.ErrorMessageGeneric;
          this.logger.WriteException("Failed sign in on LoginAsync with generic exception.", ex);
          this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        }
        finally
        {
          signInCancellation?.Dispose();
        }
        if (shouldRetry)
        {
          if (nRetries++ < 30)
            await Task.Delay(rnd.Next(10)).ConfigureAwait(true);
          else
            shouldRetry = false;
        }
      }
      while (shouldRetry);
      if (loginResult != null && loginResult.IsError)
      {
        if (loginResult.Error.Equals("UserCancel", StringComparison.OrdinalIgnoreCase))
        {
          code = SignInErrorCode.Cancelled;
          this.logger.WriteInfoEx("Enrollment was cancelled by user.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
        }
        else if (loginResult.Error.Equals("access_denied", StringComparison.OrdinalIgnoreCase))
        {
          code = SignInErrorCode.AccessDenied;
          errorMessage = Resources.ErrorMessageAccessDenied;
          this.logger.WriteErrorEx("Failed to login. " + errorMessage, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
        }
        else if (loginResult.Error.Contains("Microsoft.IdentityModel.Tokens.SecurityTokenExpiredException"))
        {
          code = SignInErrorCode.TokenExpired;
          errorMessage = Resources.ErrorMessageClientTimeSkewLine1 + Environment.NewLine + Environment.NewLine + Resources.ErrorMessageClientTimeSkewLine2;
          this.logger.WriteErrorEx("Failed to login. " + code.ToString(), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
        }
        else
        {
          StringBuilder stringBuilder = new StringBuilder();
          stringBuilder.Append("Failed to login with " + loginResult.Error + ".");
          if (loginResult.TokenResponse != null)
            stringBuilder.Append(string.Format("{0}[{1} {2} {3}]", (object) Environment.NewLine, (object) loginResult.TokenResponse.Error, (object) loginResult.TokenResponse.ErrorDescription, (object) loginResult.TokenResponse.ErrorType));
          errorMessage = Resources.ErrorMessageGeneric;
          this.logger.WriteErrorEx(stringBuilder.ToString(), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
        }
      }
      this.logger.WriteDebugEx(string.Format("Got enrollment result: {0} {1}", (object) loginResult?.IsError, (object) loginResult?.Error), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (SignInClientAsync));
      return (loginResult, errorMessage, code);
    }

    private async Task<(UserInfoResult Result, string Error)> GetUserInfoAsync(
      IOktaClient oktaClient,
      LoginResult loginResult)
    {
      UserInfoResult userInfoResult = (UserInfoResult) null;
      string errorMessage = string.Empty;
      if (oktaClient == null || loginResult == null)
        return (userInfoResult, errorMessage);
      try
      {
        userInfoResult = await oktaClient.GetUserInfoAsync(loginResult.AccessToken).ConfigureAwait(false);
        if (userInfoResult != null)
        {
          if (!userInfoResult.IsError)
            goto label_9;
        }
        errorMessage = Resources.ErrorMessageGeneric;
      }
      catch (InvalidOperationException ex)
      {
        errorMessage = Resources.ErrorMessageGeneric;
        this.logger.WriteException("Failed GetUserInfoAsync", (Exception) ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData((Exception) ex);
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        errorMessage = Resources.ErrorMessageGeneric;
        this.logger.WriteException("An error occurred while getting the user info", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
label_9:
      return (userInfoResult, errorMessage);
    }

    private ICredentialOptions InitializeCredentialOptions()
    {
      this.credentialEventHandler = (ICredentialEventHandler) new CredentialEventHandler()
      {
        OnUserInteractionRequested = new Func<CredentialEventType, CredentialEventPayload, Task<CredentialUsageContext>>(this.OnUserInteractionRequested),
        OnCredentialEventUpdate = new Action<CredentialEventType, CredentialEventPayload>(this.OnCredentialEventUpdate),
        OnAuthenticationRequired = new Func<CredentialEventType, AuthenticationRequiredEventPayload, Task<(IDeviceToken, bool)>>(this.OnAuthenticationRequired)
      };
      return (ICredentialOptions) new AuthenticatorCredentialOptions(this.configurationManager.GetKeyCreationFlagsFromRegistry(this.logger), this.credentialEventHandler, 2048, DeviceCredentialType.Rsa);
    }

    private bool AllowOidcFallback(
      CredentialEventType type,
      AuthenticationRequiredEventPayload payload)
    {
      DateTime utcNow = DateTime.UtcNow;
      if (type != CredentialEventType.SilentAuthenticationFailed || payload == null || string.IsNullOrEmpty(payload.AuthenticatorEnrollmentId) || payload.ErrorCode != PlatformErrorCode.UserCancelled)
        return true;
      DateTime dateTime;
      bool flag1 = this.onAuthenticationRequiredCancellationMap.TryGetValue(payload.AuthenticatorEnrollmentId, out dateTime);
      bool flag2 = DateTime.Compare(dateTime.AddSeconds(10.0), utcNow) < 0;
      this.logger.WriteDebugEx(string.Format("Temp: {0}, {1}, {2}, {3}, {4}", (object) flag1, (object) flag2, (object) utcNow, (object) dateTime, (object) 10), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\ClientSignInManager.cs", nameof (AllowOidcFallback));
      this.onAuthenticationRequiredCancellationMap[payload.AuthenticatorEnrollmentId] = utcNow;
      return flag1 && !flag2;
    }
  }
}
