// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Installation.AppInstallationManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Models;
using Okta.Authenticator.NativeApp.Sandbox;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.AutoUpdate.Executor;
using Okta.AutoUpdate.Shim;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Installation
{
  public class AppInstallationManager : IAppInstallationManager, IDisposable
  {
    private const int DefaultUpdateCheckIntervalSeconds = 3600;
    private const string ShellFolderRegistryPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders";
    private readonly IConfigurationManager configurationManager;
    private readonly Okta.Devices.SDK.ILogger logger;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly ISingletonHandler singletonHandler;
    private readonly IClientStorageManager storageManager;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IBuildSettingsConfig buildSettingsConfig;
    private readonly Lazy<bool> lazyBetaChannelPreConfiguredFetcher;
    private readonly Lazy<int?> lazyBucketIdFetcher;
    private readonly Lazy<string> lazyWovVersionFetcher;
    private bool disposed;
    private IUpdateClient updateClient;

    public AppInstallationManager(
      Okta.Devices.SDK.ILogger logger,
      IConfigurationManager configurationManager,
      IAnalyticsProvider analyticsProvider,
      ISingletonHandler singletonHandler,
      IClientStorageManager storageManager,
      IApplicationStateMachine stateMachine,
      IBuildSettingsConfig buildSettingsConfig)
    {
      this.logger = logger;
      this.configurationManager = configurationManager;
      this.analyticsProvider = analyticsProvider;
      this.singletonHandler = singletonHandler;
      this.storageManager = storageManager;
      this.stateMachine = stateMachine;
      this.buildSettingsConfig = buildSettingsConfig;
      this.lazyBetaChannelPreConfiguredFetcher = new Lazy<bool>(new Func<bool>(this.HasAdminPreConfiguredReleaseChannel), true);
      this.lazyBucketIdFetcher = new Lazy<int?>(new Func<int?>(this.GetBucketIdFromRegistry), true);
      this.lazyWovVersionFetcher = new Lazy<string>(new Func<string>(this.GetWovVersion), true);
      this.Initialize();
    }

    public bool IsReleaseChannelPreConfigured => this.lazyBetaChannelPreConfiguredFetcher.Value;

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    public async Task<bool> RemoveInstallationForAllUsers()
    {
      bool flag1 = true;
      foreach ((string userName, string userSid) in AuthenticatorSandboxHandler.GetAllUserAccounts(this.logger))
      {
        bool flag = flag1;
        flag1 = flag & await this.RemoveInstallationForUser(userName, userSid).ConfigureAwait(false);
      }
      return flag1;
    }

    public async Task<bool> GetBetaProgramStateAsync()
    {
      int num1;
      if (num1 != 0 && this.buildSettingsConfig.GetReleaseChannel != ReleaseChannel.GA)
        return false;
      try
      {
        if (this.IsReleaseChannelPreConfigured)
        {
          bool? betaProgramSetting = this.configurationManager.GetAdminConfiguredBetaProgramSetting(this.logger);
          bool flag = true;
          return betaProgramSetting.GetValueOrDefault() == flag & betaProgramSetting.HasValue;
        }
        OktaVerifySettingsModel verifySettingsModel = await this.storageManager.GetAppSettings().ConfigureAwait(false);
        int num2;
        if (verifySettingsModel == null)
        {
          num2 = 0;
        }
        else
        {
          bool? enrolledInBetaProgram = verifySettingsModel.EnrolledInBetaProgram;
          bool flag = true;
          num2 = enrolledInBetaProgram.GetValueOrDefault() == flag & enrolledInBetaProgram.HasValue ? 1 : 0;
        }
        return num2 != 0;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Failed to check the Beta program state", ex);
      }
      return false;
    }

    public async Task<bool> ConfigureBetaProgramSettingAsync(bool enable)
    {
      if (this.buildSettingsConfig.GetReleaseChannel != ReleaseChannel.GA)
      {
        this.logger.WriteInfoEx(string.Format("Beta Program Cannot be configured for {0}.", (object) this.buildSettingsConfig.GetReleaseChannel), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (ConfigureBetaProgramSettingAsync));
        return false;
      }
      if (!this.IsReleaseChannelPreConfigured)
        return await this.storageManager.UpdateAppSettings((Action<OktaVerifySettingsModel>) (appSettings => appSettings.EnrolledInBetaProgram = new bool?(enable))).ConfigureAwait(false);
      this.logger.WriteInfoEx("Beta Program already pre-configured ignoring update request.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (ConfigureBetaProgramSettingAsync));
      return false;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing && this.updateClient != null)
      {
        this.updateClient.OnAutoUpdateMessageNotification -= new EventHandler<MessageNotificationEventArgs>(this.HandleAutoUpdateMessageNotification);
        this.updateClient.SafeDispose();
      }
      this.disposed = true;
    }

    private bool HasAdminPreConfiguredReleaseChannel()
    {
      if (this.buildSettingsConfig.GetReleaseChannel != ReleaseChannel.GA)
        return true;
      bool? betaProgramSetting = this.configurationManager.GetAdminConfiguredBetaProgramSetting(this.logger);
      if (!betaProgramSetting.HasValue)
        return false;
      this.logger.WriteInfoEx(string.Format("Admin Has Configured BETA Program: {0}", (object) betaProgramSetting.Value), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (HasAdminPreConfiguredReleaseChannel));
      return true;
    }

    private int? GetBucketIdFromRegistry()
    {
      int result1;
      if (int.TryParse(this.configurationManager.TryGetMachineRegistryConfig<string>(this.logger, "AutoUpdateBucketIdOverride", (string) null), out result1))
        return new int?(result1);
      int result2;
      return !int.TryParse(this.configurationManager.TryGetMachineRegistryConfig<string>(this.logger, "AutoUpdateDeferredByDays", (string) null), out result2) ? new int?() : new int?(19 + result2);
    }

    private string GetWovVersion() => this.configurationManager.TryGetMachineRegistryConfig<string>(this.logger, "AutoUpdateClientVersionOverride", DevicesSdk.AssemblyInformation.ApplicationVersion.ToString());

    private Okta.AutoUpdate.Executor.LogLevel GetLogLevel() => (Okta.AutoUpdate.Executor.LogLevel) this.configurationManager.GetLogLevel(this.logger);

    private Task InitializeAutoUpdate() => Task.Run((Action) (() =>
    {
      int registryConfig = this.configurationManager.TryGetRegistryConfig<int>(this.logger, "AutoUpdatePollingInSecond", 3600, (Func<object, int>) (x => Convert.ToInt32(x, (IFormatProvider) CultureInfo.InvariantCulture)));
      this.updateClient = (IUpdateClient) new UpdateClient("Okta", "OktaUpdate", this.GetLogLevel(), registryConfig * 1000);
      this.updateClient.Initialize(new Func<Task<IUpdateClientData>>(this.GetUpdateClientData));
      this.updateClient.OnAutoUpdateMessageNotification += new EventHandler<MessageNotificationEventArgs>(this.HandleAutoUpdateMessageNotification);
    }));

    private async Task<bool> RemoveInstallationForUser(string userName, string userSid)
    {
      string base64String = Convert.ToBase64String(userName.CreateSha256Hash());
      this.logger.WriteInfoEx("Removing Okta Verify for " + base64String + " " + Convert.ToBase64String(userSid.CreateSha256Hash()) + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveInstallationForUser));
      using (RegistryKey userKey = Registry.Users.OpenSubKey(userSid))
      {
        if (userKey == null)
        {
          this.logger.WriteDebugEx("Skipping account that has no registry hive.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveInstallationForUser));
          return true;
        }
        (bool Exists, string Path) applicationDataFolder = this.GetOktaApplicationDataFolder(userKey);
        if (applicationDataFolder.Path == null)
          return !applicationDataFolder.Exists;
        IConfigurationManager userConfiguration = (IConfigurationManager) OktaVerifyConfigurationManager.GetCustomUserConfiguration(this.configurationManager, userKey, applicationDataFolder.Path);
        bool flag = this.RemoveSandboxAccounts(userConfiguration);
        try
        {
          Directory.Delete(applicationDataFolder.Path, true);
        }
        catch (Exception ex) when (!ex.IsCritical(this.logger))
        {
          this.logger.WriteException("Failed to delete oktaVerify LocalAppData folder for " + base64String, ex);
          this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        }
        return this.CleanUserRegistrySettings(userSid, userConfiguration) & flag;
      }
    }

    private bool RemoveSandboxAccounts(IConfigurationManager userConfigManager) => AuthenticatorSandboxHandler.RemoveSandboxAccountsBasedOnDirectory(this.logger, userConfigManager.GetSandboxDirectory(this.logger));

    private (bool Exists, string Path) GetOktaApplicationDataFolder(RegistryKey userKey)
    {
      string str = (string) null;
      using (RegistryKey registryKey = userKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Shell Folders", false))
      {
        if (registryKey == null)
        {
          this.logger.WriteWarningEx("Unable to get local app data folder, key does not exist.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (GetOktaApplicationDataFolder));
          return (true, (string) null);
        }
        str = registryKey.GetValue("Local AppData") as string;
      }
      if (string.IsNullOrEmpty(str))
      {
        this.logger.WriteWarningEx("Unable to get local app data folder, no data in registry.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (GetOktaApplicationDataFolder));
        return (true, (string) null);
      }
      if (!Directory.Exists(str))
      {
        this.logger.WriteDebugEx("Unable to get local app data folder, user profile might not be created.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (GetOktaApplicationDataFolder));
        return (false, (string) null);
      }
      string path = Path.Combine(str, this.configurationManager.ApplicationDataFolder);
      if (Directory.Exists(path))
        return (true, path);
      this.logger.WriteDebugEx("Unable to get Okta Verify folder, Okta Verify was probably not used.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (GetOktaApplicationDataFolder));
      return (false, (string) null);
    }

    private bool CleanUserRegistrySettings(string userSid, IConfigurationManager configManager)
    {
      RegistryKey registryKey = (RegistryKey) null;
      try
      {
        registryKey = Registry.Users.OpenSubKey(userSid + "\\" + configManager.VendorRegistryRoot, true);
        if (registryKey == null)
          return true;
        registryKey.DeleteSubKeyTree(configManager.ApplicationRegistryKey, false);
        return true;
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        this.logger.WriteExceptionAsWarning("Unable to delete reg keys for user " + userSid + ".", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      finally
      {
        registryKey?.Dispose();
      }
      return false;
    }

    private void HandleAutoUpdateMessageNotification(
      object sender,
      MessageNotificationEventArgs messageNotificationEventArgs)
    {
      this.logger.WriteDebugEx(string.Format("Received auto update notification of type {0}.", (object) messageNotificationEventArgs.NotificationType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (HandleAutoUpdateMessageNotification));
      switch (messageNotificationEventArgs.NotificationType)
      {
        case NotificationType.AutoUpdateUnhandledException:
          if (messageNotificationEventArgs.Databag == null || messageNotificationEventArgs.Databag.Count == 0)
          {
            this.analyticsProvider.TrackErrorWithLogsAndAppData(messageNotificationEventArgs.Exception);
            break;
          }
          this.analyticsProvider.TrackErrorWithLogs(messageNotificationEventArgs.Exception, messageNotificationEventArgs.Databag);
          break;
        case NotificationType.AutoUpdateFolderCleanupFailure:
        case NotificationType.AutoUpdateUpdateClientFailure:
        case NotificationType.AutoUpdateFound:
        case NotificationType.AutoUpdateDownloadProgress:
        case NotificationType.AutoUpdateDownloadCompleted:
        case NotificationType.AutoUpdateInstallationStarted:
        case NotificationType.AutoUpdateBatteryStatusCheckFailed:
        case NotificationType.AutoUpdateStartingWindowsService:
          if (messageNotificationEventArgs.Exception != null)
            messageNotificationEventArgs.Databag.Add("ExceptionData", messageNotificationEventArgs.Exception.ToString());
          this.analyticsProvider.TrackEvent(messageNotificationEventArgs.NotificationType.ToString(), messageNotificationEventArgs.Databag);
          break;
        case NotificationType.AutoUpdatePeriodicCheckFailure:
          if (messageNotificationEventArgs.Exception != null)
            messageNotificationEventArgs.Databag.Add("ExceptionData", messageNotificationEventArgs.Exception.ToString());
          this.analyticsProvider.TrackEvent(messageNotificationEventArgs.NotificationType.ToString(), messageNotificationEventArgs.Databag);
          this.updateClient.Dispose();
          this.InitializeAutoUpdate();
          break;
      }
    }

    internal async Task<IUpdateClientData> GetUpdateClientData()
    {
      IClientAccountManager clientAccountManager = AppInjector.Get<IClientAccountManager>();
      if (clientAccountManager == null)
      {
        this.analyticsProvider.TrackErrorWithLogs("AutoupdateGetClientData:clientAccountManagerIsNull", sourceMethodName: nameof (GetUpdateClientData));
        return (IUpdateClientData) null;
      }
      Uri uri = (await clientAccountManager.GetPrimaryOrganization().ConfigureAwait(false)).Item1;
      if (uri == (Uri) null)
        return (IUpdateClientData) null;
      UpdateClientData updateClientData1 = new UpdateClientData();
      updateClientData1.ArtifactType = "WINDOWS_OKTA_VERIFY";
      updateClientData1.BaseUrl = uri;
      UpdateClientData updateClientData2 = updateClientData1;
      updateClientData2.ReleaseChannel = await this.GetReleaseChannelAsync().ConfigureAwait(false);
      updateClientData1.ArtifactVersion = this.lazyWovVersionFetcher.Value;
      updateClientData1.WindowsServiceName = "Okta Auto Update Service";
      updateClientData1.BucketId = this.lazyBucketIdFetcher.Value;
      UpdateClientData updateClientData = updateClientData1;
      updateClientData2 = (UpdateClientData) null;
      updateClientData1 = (UpdateClientData) null;
      return (IUpdateClientData) updateClientData;
    }

    private async Task<ReleaseChannel> GetReleaseChannelAsync()
    {
      ReleaseChannel channel = this.buildSettingsConfig.GetReleaseChannel;
      this.logger.WriteInfoEx(string.Format("Read release channel from build settings file: {0}", (object) channel), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (GetReleaseChannelAsync));
      return channel != ReleaseChannel.GA ? channel : (await this.GetBetaProgramStateAsync().ConfigureAwait(false) ? ReleaseChannel.EA : channel);
    }

    private void Initialize()
    {
      this.stateMachine.RegisterDeferral(ComputingStateType.Loading, (ComputingStateDeferral) (_ => this.InitializeAutoUpdate()), "initializing auto update");
      this.stateMachine.RegisterDeferral(ComputingStateType.ShuttingDown, new ComputingStateDeferral(this.HandleShutdown), "installation manager shutdown");
    }

    private async Task ReportErrorBeforeShutdown(string[] arguments)
    {
      if (arguments == null || arguments.Length <= 1)
      {
        this.logger.WriteInfoEx("Received error report through command line without title, discarding.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (ReportErrorBeforeShutdown));
      }
      else
      {
        string message = arguments[1];
        this.logger.WriteWarningEx("Received error report through command line: " + message + " " + string.Join(" ", arguments), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (ReportErrorBeforeShutdown));
        this.analyticsProvider.TrackErrorWithLogsAndAppData((Exception) new OktaErrorReportException(message, nameof (ReportErrorBeforeShutdown)));
        await Task.Delay(4000).ConfigureAwait(false);
      }
    }

    private async Task RemoveAllBeforeShutdown()
    {
      bool removedEnrollments = true;
      AuthenticatorAccountManager manager = (AuthenticatorAccountManager) null;
      await DevicesSdk.EnsureInitialized().ConfigureAwait(false);
      try
      {
        this.logger.WriteDebugEx("Removing enrollments for current user.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveAllBeforeShutdown));
        int found = 0;
        int removed = 0;
        manager = new AuthenticatorAccountManager();
        foreach (IDeviceEnrollment enrollment in await manager.GetEnrollments().ConfigureAwait(false))
        {
          ++found;
          if (await ClientAccountManager.DeleteAccount(enrollment.AuthenticatorEnrollmentId, (IAuthenticatorAccountManager) manager, this.logger, this.analyticsProvider).ConfigureAwait(false) == AuthenticatorOperationResult.Success)
          {
            ++removed;
          }
          else
          {
            this.logger.WriteWarningEx("Failed to cleanly remove enrollment " + enrollment.AuthenticatorEnrollmentId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveAllBeforeShutdown));
            removedEnrollments = false;
          }
        }
        this.logger.WriteDebugEx(string.Format("{0}: Removed {1} out of {2} enrollments.", removedEnrollments ? (object) "Success" : (object) "Failure", (object) removed, (object) found), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveAllBeforeShutdown));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Failed to clean account for current user.", ex);
        removedEnrollments = false;
      }
      finally
      {
        manager?.Dispose();
      }
      this.storageManager.SafeDispose();
      bool isRunningElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
      bool flag = isRunningElevated;
      if (flag)
        flag = await this.RemoveInstallationForAllUsers().ConfigureAwait(false);
      if (flag)
      {
        this.logger.WriteDebugEx("Successfully removed all installations.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveAllBeforeShutdown));
        manager = (AuthenticatorAccountManager) null;
      }
      else
      {
        this.logger.WriteDebugEx(string.Format("Failed to remove all installations. IsRunningElevated: {0}", (object) isRunningElevated), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Installation\\AppInstallationManager.cs", nameof (RemoveAllBeforeShutdown));
        manager = (AuthenticatorAccountManager) null;
      }
    }

    private async Task HandleShutdown(ComputingStateContext context)
    {
      switch (context.Command)
      {
        case StartupArgumentType.ReportError:
          await this.ReportErrorBeforeShutdown(context.Arguments).ConfigureAwait(false);
          break;
        case StartupArgumentType.RemoveAll:
          this.singletonHandler.DemoteOrSignalShutdown();
          await this.RemoveAllBeforeShutdown().ConfigureAwait(false);
          break;
      }
    }
  }
}
