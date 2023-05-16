// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Sandbox.AuthenticatorSandboxManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInfo;
using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Credentials;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Telemetry;
using Okta.Devices.SDK.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Sandbox
{
  internal class AuthenticatorSandboxManager : IAuthenticatorSandboxManager, IDisposable
  {
    private static string userId;
    private static string oktaVerifyStorageKey;
    private readonly ILogger logger;
    private readonly IClientStorageManager storageManager;
    private readonly IConfigurationManager configurationManager;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IApplicationStateMachine stateMachine;
    private readonly Lazy<Task<IAuthenticatorSandbox>> sandboxInitializer;
    private bool disposed;

    public AuthenticatorSandboxManager(
      ILogger logger,
      IClientStorageManager storageManager,
      IConfigurationManager configurationManager,
      IAnalyticsProvider analytics,
      IApplicationStateMachine stateMachine)
    {
      this.logger = logger;
      this.storageManager = storageManager;
      this.configurationManager = configurationManager;
      this.analyticsProvider = analytics;
      this.stateMachine = stateMachine;
      this.stateMachine.RegisterDeferral(ComputingStateType.ShuttingDown, new ComputingStateDeferral(this.HandleConfiguration), "sandbox configuration");
      this.sandboxInitializer = new Lazy<Task<IAuthenticatorSandbox>>(new Func<Task<IAuthenticatorSandbox>>(this.InitializeSandbox), true);
    }

    public SandboxIntegrityState SandboxState { get; private set; }

    internal SecureString SecureString { get; private set; }

    internal static string UserId
    {
      get
      {
        if (AuthenticatorSandboxManager.userId == null)
          AuthenticatorSandboxManager.userId = WindowsIdentity.GetCurrent().User.ToString();
        return AuthenticatorSandboxManager.userId;
      }
    }

    internal static string OktaVerifyInfoKey
    {
      get
      {
        if (AuthenticatorSandboxManager.oktaVerifyStorageKey == null)
          AuthenticatorSandboxManager.oktaVerifyStorageKey = Convert.ToBase64String(AuthenticatorSandboxManager.UserId.CreateSha256Hash());
        return AuthenticatorSandboxManager.oktaVerifyStorageKey;
      }
    }

    public async Task<IAuthenticatorSandbox> GetSandbox() => await this.sandboxInitializer.Value.ConfigureAwait(false);

    internal async Task<string> ConfigureSandbox()
    {
      SandboxIntegrityState state = SandboxIntegrityState.Unknown;
      SecureString secure = (SecureString) null;
      string sandboxName = (string) null;
      AuthenticatorSandbox sandbox = (AuthenticatorSandbox) null;
      ApplicationTelemetryOperation operation = new ApplicationTelemetryOperation(AppTelemetryScenario.ApplicationConfiguration);
      try
      {
        OktaVerifyInformation appInfo = await this.storageManager.Store.TryGetDataAsync<OktaVerifyInformation>(AuthenticatorSandboxManager.OktaVerifyInfoKey).ConfigureAwait(false);
        if (appInfo != null && appInfo.SandboxState == SandboxIntegrityState.Ready)
        {
          sandboxName = appInfo.SandboxName;
          (state, sandbox) = await this.InitializeAuthenticatorSandbox(appInfo, true).ConfigureAwait(false);
          switch (state)
          {
            case SandboxIntegrityState.NotExist:
              bool removedSandbox = AuthenticatorSandbox.RemoveSandboxAccount(appInfo.SandboxName);
              bool flag = await this.storageManager.Store.RemoveDataAsync<OktaVerifyInformation>(appInfo.Id).ConfigureAwait(false);
              this.logger.WriteWarningEx(string.Format("Removing broken sandbox {0}, {1}|{2}", (object) appInfo.SandboxName, (object) removedSandbox, (object) flag), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (ConfigureSandbox));
              this.analyticsProvider.TrackErrorWithLogs("Sandbox is in bad state, removing.", sourceMethodName: nameof (ConfigureSandbox));
              sandboxName = (string) null;
              break;
            case SandboxIntegrityState.Ready:
              operation.AddData(AppTelemetryDataKey.SandboxName, (object) appInfo.SandboxName);
              operation.AddData(AppTelemetryDataKey.SandboxState, (object) appInfo.SandboxState);
              operation.Status = TelemetryEventStatus.Success;
              this.MigrateSandboxProfileIfNecessary(sandbox, appInfo.SandboxName, operation);
              return sandboxName;
            default:
              List<DeviceEnrollment> deviceEnrollmentList = await this.storageManager.Store.GetAllAsync<DeviceEnrollment>().ConfigureAwait(false);
              if (deviceEnrollmentList.Count > 0)
              {
                operation.AddData(TelemetryDataKey.OktaDeviceId, (object) deviceEnrollmentList[0].DeviceId);
                operation.Status = TelemetryEventStatus.Abandoned;
                this.logger.WriteErrorEx(string.Format("Sandbox is in broken state {0}, not removing it because of existing enrollments.", (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (ConfigureSandbox));
                this.analyticsProvider.TrackErrorWithLogs("Sandbox is broken with existing keys.", sourceMethodName: nameof (ConfigureSandbox));
                this.MigrateSandboxProfileIfNecessary(sandbox, appInfo.SandboxName, operation);
                return sandboxName;
              }
              goto case SandboxIntegrityState.NotExist;
          }
        }
        if (this.configurationManager.IsSandboxDisabled(this.logger))
        {
          operation.Status = TelemetryEventStatus.Disallowed;
          this.logger.WriteWarningEx("Not creating sandbox since it was disabled in registry.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (ConfigureSandbox));
          return (string) null;
        }
        this.logger.WriteInfoEx("Sandbox is not configured yet, trying to set it up.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (ConfigureSandbox));
        byte[] clientIdentifier = NativeLibrary.CreateClientIdentifier(AuthenticatorSandboxManager.OktaVerifyInfoKey, this.logger);
        sandboxName = this.configurationManager.GenerateSandboxName(this.logger, AuthenticatorSandboxManager.UserId);
        operation.AddData(AppTelemetryDataKey.SandboxName, (object) sandboxName);
        operation.AddData(AppTelemetryDataKey.AttemptSandboxCreation, (object) "true");
        secure = NativeLibrary.LoadClientAssociation(AuthenticatorSandboxManager.OktaVerifyInfoKey, clientIdentifier, this.logger);
        SandboxCreationResult sandboxAccount = AuthenticatorSandbox.CreateSandboxAccount(sandboxName, secure, this.GetSandboxProfileDirectory(sandboxName));
        if (sandboxAccount == SandboxCreationResult.Success)
        {
          this.logger.WriteInfoEx("Successfully configured the Sandbox.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (ConfigureSandbox));
          sandbox = new AuthenticatorSandbox(sandboxName, secure);
          state = this.CheckSandboxIntegrity((IAuthenticatorSandbox) sandbox, true);
        }
        if (state == SandboxIntegrityState.Ready)
        {
          appInfo = new OktaVerifyInformation(AuthenticatorSandboxManager.OktaVerifyInfoKey, clientIdentifier, sandboxName, state);
          int num = await this.storageManager.Store.PutDataAsync<OktaVerifyInformation>(appInfo.Id, appInfo).ConfigureAwait(false) ? 1 : 0;
          operation.AddData(AppTelemetryDataKey.SandboxState, (object) state);
          operation.Status = TelemetryEventStatus.Success;
          return sandboxName;
        }
        bool flag1 = false;
        if (sandboxAccount == SandboxCreationResult.Success)
        {
          flag1 = AuthenticatorSandbox.RemoveSandboxAccount(sandboxName);
          sandboxName = (string) null;
        }
        this.logger.WriteWarningEx(string.Format("Failed to configure the Sandbox: {0}|{1}|{2}.", (object) sandboxAccount, (object) state, (object) flag1), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (ConfigureSandbox));
        this.analyticsProvider.TrackErrorWithLogs("Sandbox could not be configured.", sourceMethodName: nameof (ConfigureSandbox));
        appInfo = new OktaVerifyInformation(AuthenticatorSandboxManager.OktaVerifyInfoKey, state);
        int num1 = await this.storageManager.Store.PutDataAsync<OktaVerifyInformation>(appInfo.Id, appInfo).ConfigureAwait(false) ? 1 : 0;
        operation.AddData(AppTelemetryDataKey.SandboxState, (object) SandboxIntegrityState.NotExist);
        operation.Status = TelemetryEventStatus.Abandoned;
        return (string) null;
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        operation.Status = TelemetryEventStatus.UnknownError;
        this.logger.WriteExceptionAsWarning("Failed to configure application.", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        return sandboxName;
      }
      finally
      {
        secure?.Dispose();
        sandbox?.Dispose();
        operation?.Dispose();
      }
    }

    internal async Task<IAuthenticatorSandbox> InitializeSandbox()
    {
      OktaVerifyInformation appInfo = await this.storageManager.Store.TryGetDataAsync<OktaVerifyInformation>(AuthenticatorSandboxManager.OktaVerifyInfoKey).ConfigureAwait(false);
      bool flag1 = this.configurationManager.IsSandboxDisabled(this.logger);
      bool flag2 = string.IsNullOrEmpty(appInfo?.SandboxName) || appInfo.InstanceIdentifier == null;
      if (flag1 | flag2)
      {
        this.logger.WriteWarningEx(string.Format("Not using sandbox. Created: {0}, Disabled: {1}", (object) !flag2, (object) flag1), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (InitializeSandbox));
        return (IAuthenticatorSandbox) new BaseAuthenticatorSandbox();
      }
      AuthenticatorSandbox authenticatorSandbox;
      (this.SandboxState, authenticatorSandbox) = await this.InitializeAuthenticatorSandbox(appInfo, false).ConfigureAwait(false);
      if (this.SandboxState == SandboxIntegrityState.Ready || this.SandboxState == SandboxIntegrityState.PasswordExpired)
        return (IAuthenticatorSandbox) authenticatorSandbox;
      this.logger.WriteWarningEx(string.Format("Not using sandbox, state is {0}.", (object) this.SandboxState), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (InitializeSandbox));
      authenticatorSandbox?.Dispose();
      return (IAuthenticatorSandbox) new BaseAuthenticatorSandbox();
    }

    [AnalyticsScenario(ScenarioType.Configuration)]
    private async Task HandleConfiguration(ComputingStateContext context)
    {
      if (context.Command != StartupArgumentType.Configure)
        return;
      try
      {
        string validAccountToSkip = await this.ConfigureSandbox().ConfigureAwait(false);
        if (Okta.Authenticator.NativeApp.Extensions.IsTestBuild)
          return;
        this.logger.WriteInfoEx(string.Format("Cleaning old sandbox directories: {0}|{1}|{2}|{3}", (object) AuthenticatorSandboxHandler.RemoveSandboxAccountsBasedOnDirectory(this.logger, Path.Combine(this.configurationManager.ApplicationDataPath, "Sandbox"), validAccountToSkip), (object) AuthenticatorSandboxHandler.RemoveSandboxAccountsBasedOnDirectory(this.logger, this.configurationManager.GetSandboxDirectory(this.logger), validAccountToSkip), (object) AuthenticatorSandboxHandler.RemoveOrphanedSandboxAccountsMatchingPrefix(this.logger, this.configurationManager.GenerateSandboxName(this.logger, AuthenticatorSandboxManager.UserId, false), validAccountToSkip), (object) AuthenticatorSandboxHandler.RemoveOrphanedSandboxAccountsThroughRegistry(this.logger, this.configurationManager.GenerateSandboxName(this.logger, AuthenticatorSandboxManager.UserId, false), validAccountToSkip)), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (HandleConfiguration));
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        this.logger.WriteExceptionAsWarning("Failed to configure application.", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing)
      {
        if (this.sandboxInitializer.IsValueCreated)
          this.GetSandbox().ContinueWith<bool>((Func<Task<IAuthenticatorSandbox>, bool>) (t => t.Result.SafeDispose()), TaskScheduler.Default);
        this.SecureString?.Dispose();
      }
      this.disposed = true;
    }

    private string GetSandboxProfileDirectory(string sandboxName) => this.configurationManager.GetSandboxDirectory(this.logger) + "\\" + sandboxName;

    private async Task<(SandboxIntegrityState State, AuthenticatorSandbox Sandbox)> InitializeAuthenticatorSandbox(
      OktaVerifyInformation appInfo,
      bool loadUserConfig)
    {
      SandboxIntegrityState sandboxIntegrityState = SandboxIntegrityState.Unknown;
      AuthenticatorSandbox sandbox = (AuthenticatorSandbox) null;
      try
      {
        this.SecureString = NativeLibrary.LoadClientAssociation(AuthenticatorSandboxManager.OktaVerifyInfoKey, appInfo.InstanceIdentifier, this.logger);
        sandbox = new AuthenticatorSandbox(appInfo.SandboxName, this.SecureString);
        sandboxIntegrityState = this.CheckSandboxIntegrity((IAuthenticatorSandbox) sandbox, loadUserConfig);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteExceptionAsWarning("InitializeAuthenticatorSandbox throw when trying to check sandbox integrity.", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      if (sandboxIntegrityState == SandboxIntegrityState.PasswordExpired)
        sandboxIntegrityState = await this.UpdateSandboxPassword((IAuthenticatorSandbox) sandbox, appInfo, loadUserConfig).ConfigureAwait(false);
      (SandboxIntegrityState, AuthenticatorSandbox) valueTuple = (sandboxIntegrityState, sandbox);
      sandbox = (AuthenticatorSandbox) null;
      return valueTuple;
    }

    private SandboxIntegrityState CheckSandboxIntegrity(
      IAuthenticatorSandbox sandbox,
      bool loadUserConfig)
    {
      SandboxIntegrityState sandboxIntegrityState = sandbox.CheckFunctionality(loadUserConfig);
      this.logger.WriteDebugEx(string.Format("Sandbox forcing profile load: {0}, state : {1}", (object) loadUserConfig, (object) sandboxIntegrityState), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (CheckSandboxIntegrity));
      return sandboxIntegrityState;
    }

    private async Task<SandboxIntegrityState> UpdateSandboxPassword(
      IAuthenticatorSandbox sandbox,
      OktaVerifyInformation appInfo,
      bool loadUserConfig)
    {
      SandboxIntegrityState result = SandboxIntegrityState.Unknown;
      try
      {
        byte[] clientIdentifier = NativeLibrary.CreateClientIdentifier(AuthenticatorSandboxManager.OktaVerifyInfoKey, this.logger);
        SecureString newPassword = NativeLibrary.LoadClientAssociation(AuthenticatorSandboxManager.OktaVerifyInfoKey, clientIdentifier, this.logger);
        if (sandbox.UpdatePassword(newPassword))
        {
          this.SecureString = newPassword;
          appInfo.InstanceIdentifier = clientIdentifier;
          int num = await this.storageManager.Store.PutDataAsync<OktaVerifyInformation>(appInfo.Id, appInfo).ConfigureAwait(false) ? 1 : 0;
          result = this.CheckSandboxIntegrity(sandbox, loadUserConfig);
        }
        else
          newPassword?.Dispose();
        this.logger.WriteInfoEx(string.Format("Sandbox update result: {0}", (object) result), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxManager.cs", nameof (UpdateSandboxPassword));
      }
      catch (OktaCryptographicException ex)
      {
        this.logger.WriteException("Failed to update sandbox", (Exception) ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData((Exception) ex);
      }
      return result;
    }

    private void MigrateSandboxProfileIfNecessary(
      AuthenticatorSandbox sandbox,
      string sandboxName,
      ApplicationTelemetryOperation operation)
    {
      ProfileMoveResult profileMoveResult = sandbox.MigrateProfileLocation(this.GetSandboxProfileDirectory(sandboxName));
      operation.AddData(AppTelemetryDataKey.SandboxMigration, (object) profileMoveResult);
    }
  }
}
