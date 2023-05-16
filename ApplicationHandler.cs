// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using IdentityModel.OidcClient.Browser;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Models;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Authenticator.NativeApp.ViewModels;
using Okta.Authenticator.NativeApp.Views;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Oidc.Abstractions;
using Okta.Oidc.Wpf;
using Okta.OktaVerify.Windows.Core.Properties;
using Okta.OktaVerify.Windows.Core.UI.Themes;
using OktaVerify.Bridge.Contracts;
using OktaVerify.Bridge.Events;
using Prism.Events;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Okta.Authenticator.NativeApp
{
  public class ApplicationHandler : IApplicationHandler, IApplicationInteraction
  {
    private readonly ILogger logger;
    private readonly IEventAggregator eventAggregator;
    private readonly IClientStorageManager storageManager;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly ISystemSettingsManager systemSettingsManager;
    private readonly IApplicationStateMachine stateMachine;
    private App app;
    private SubscriptionToken themeUpdateToken;

    public ApplicationHandler(
      ILogger logger,
      IEventAggregator eventAggregator,
      IClientStorageManager storageManager,
      IAnalyticsProvider analyticsProvider,
      ISystemSettingsManager systemSettingsManager,
      IApplicationStateMachine stateMachine)
    {
      this.logger = logger;
      this.eventAggregator = eventAggregator;
      this.storageManager = storageManager;
      this.analyticsProvider = analyticsProvider;
      this.systemSettingsManager = systemSettingsManager;
      this.stateMachine = stateMachine;
      this.Initialize();
    }

    public AppThemeSettingType DefaultAppTheme => AppThemeSettingType.System;

    public JumpList JumpList
    {
      get => JumpList.GetJumpList((Application) this.app) ?? new JumpList();
      set => JumpList.SetJumpList((Application) this.app, value);
    }

    protected App App
    {
      get
      {
        if (this.app == null)
          this.app = Application.Current as App;
        return this.app;
      }
    }

    public OktaClientConfiguration SetDefaultSystemBrowser(OktaClientConfiguration configuration)
    {
      configuration.EnsureNotNull(nameof (configuration));
      SystemBrowserResponseOptions browserOptions = new SystemBrowserResponseOptions(SystemBrowserResponseType.Redirect, redirectUrl: configuration.OktaDomain + "/oktaverify-auth/success");
      configuration.Browser = (IBrowser) new DefaultSystemBrowser(browserOptions, (Action) (() =>
      {
        this.logger.WriteInfoEx("Interaction with the browser complete", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\ApplicationHandler.cs", nameof (SetDefaultSystemBrowser));
        this.stateMachine.TransitionTo(AppStateRequestType.BringToFocus);
      }), MicrosoftExtensionLogger.CreateFromOktaLogger(this.logger));
      return configuration;
    }

    public OktaClientConfiguration SetDefaultIntegratedBrowser(OktaClientConfiguration configuration)
    {
      configuration.EnsureNotNull(nameof (configuration));
      configuration.Browser = (IBrowser) new DefaultIntegratedBrowser(Resources.SigningIn, this.App.MainWindow.Width - 80.0, this.App.MainWindow.Height - 100.0, this.App.MainWindow.Top + 50.0, this.App.MainWindow.Left + 40.0);
      return configuration;
    }

    public async Task<AppThemeSettingType> GetSavedAppTheme()
    {
      try
      {
        OktaVerifySettingsModel verifySettingsModel = await this.storageManager.GetAppSettings().ConfigureAwait(false);
        return verifySettingsModel != null ? verifySettingsModel.AppTheme : AppThemeSettingType.Unknown;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("An error occurred while checking the app theme.", ex);
      }
      return AppThemeSettingType.Unknown;
    }

    public async Task<bool> TryUpdateAppTheme(AppThemeSettingType themeName)
    {
      bool updated = false;
      try
      {
        updated = this.ApplyTheme(themeName);
        if (updated)
          updated = await this.storageManager.UpdateAppSettings((Action<OktaVerifySettingsModel>) (settings => settings.AppTheme = themeName)).ConfigureAwait(false);
        this.logger.WriteInfoEx(string.Format("Theme settings updated: {0}", (object) updated), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\ApplicationHandler.cs", nameof (TryUpdateAppTheme));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteErrorEx("An error occurred while updating the app theme: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\ApplicationHandler.cs", nameof (TryUpdateAppTheme));
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return updated;
    }

    public bool InvokeUri(string address)
    {
      Uri result;
      if (!Uri.TryCreate(address, UriKind.Absolute, out result))
      {
        this.logger.WriteErrorEx("Failed to invoke link to " + address + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\ApplicationHandler.cs", nameof (InvokeUri));
        return false;
      }
      this.InvokeUriInternal(result).AsBackgroundTask(string.Format("launching uri {0}", (object) result));
      return true;
    }

    public void InvokeOnUIThread(Action action) => this.InvokeOnUIThread(action, CancellationToken.None);

    public void InvokeOnUIThread(Action action, CancellationToken cancellationToken)
    {
      if (action == null)
        return;
      this.App.Dispatcher.Invoke(action, DispatcherPriority.Normal, cancellationToken);
    }

    public T InvokeOnUIThread<T>(Func<T> action) => this.InvokeOnUIThread<T>(action, CancellationToken.None);

    public T InvokeOnUIThread<T>(Func<T> action, CancellationToken cancellationToken) => action != null ? this.App.Dispatcher.Invoke<T>(action, DispatcherPriority.Normal, cancellationToken) : default (T);

    public Task InvokeOnUIThreadAsync(Func<Task> action) => this.InvokeOnUIThreadAsync(action, CancellationToken.None);

    public Task InvokeOnUIThreadAsync(Func<Task> action, CancellationToken cancellationToken) => this.InvokeOnUIThreadAsync(action, DispatcherPriority.Normal, cancellationToken);

    Task IApplicationInteraction.InvokeOnUIThread(
      Func<Task> operation,
      CancellationToken cancellationToken)
    {
      return this.InvokeOnUIThreadAsync(operation, cancellationToken);
    }

    public void ShowAboutWindow()
    {
      AboutView child;
      if (this.GetChildWindow<AboutView>(out child))
        child.ShowDialog();
      else
        child.Activate();
    }

    public void ShowReportIssueWindow()
    {
      FeedbackView child;
      if (!this.GetChildWindow<FeedbackView>(out child))
        return;
      using (child)
        child.ShowCustomerReportWindow();
    }

    public void ShowSettings()
    {
      this.stateMachine.TransitionTo(AppStateRequestType.Activate);
      this.eventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.Settings, (INotifyPropertyChanged) new SettingsViewModel()));
    }

    internal AppThemeType DetermineAppTheme(AppThemeSettingType settingTheme)
    {
      SystemTheme defaultSystemTheme = this.systemSettingsManager.GetDefaultSystemTheme();
      switch (settingTheme)
      {
        case AppThemeSettingType.Light:
          return defaultSystemTheme != SystemTheme.Dark ? AppThemeType.Light : AppThemeType.LightOnDark;
        case AppThemeSettingType.Dark:
          return defaultSystemTheme != SystemTheme.Dark ? AppThemeType.DarkOnLight : AppThemeType.Dark;
        default:
          SystemTheme defaultAppTheme = this.systemSettingsManager.GetDefaultAppTheme();
          return defaultSystemTheme == defaultAppTheme ? (defaultSystemTheme != SystemTheme.Dark ? AppThemeType.Light : AppThemeType.Dark) : (defaultAppTheme != SystemTheme.Dark ? AppThemeType.LightOnDark : AppThemeType.DarkOnLight);
      }
    }

    private void Initialize() => this.eventAggregator.GetEvent<AppStateEvent>().Subscribe(new Action<AppState>(this.OnAppStateUpdated));

    private async Task InitializeAppTheme()
    {
      ApplicationHandler applicationHandler = this;
      // ISSUE: explicit non-virtual call
      AppThemeSettingType settingTheme = await __nonvirtual (applicationHandler.GetSavedAppTheme()).ConfigureAwait(false);
      if (settingTheme == AppThemeSettingType.Unknown)
      {
        // ISSUE: explicit non-virtual call
        settingTheme = __nonvirtual (applicationHandler.DefaultAppTheme);
      }
      applicationHandler.ApplyTheme(settingTheme);
      applicationHandler.themeUpdateToken = applicationHandler.eventAggregator.GetEvent<SystemThemeEvent>().Subscribe(new Action(applicationHandler.OnSystemThemeUpdated));
    }

    private bool ApplyTheme(AppThemeSettingType settingTheme) => ThemeResourceDictionary.EnsureThemeApplied((Application) this.App, this.DetermineAppTheme(settingTheme));

    private async Task InvokeOnUIThreadAsync(
      Func<Task> action,
      DispatcherPriority priority,
      CancellationToken cancellationToken)
    {
      if (action == null)
        return;
      await this.App.Dispatcher.Invoke<Task>(action, priority, cancellationToken).ConfigureAwait(false);
    }

    private void OnAppStateUpdated(AppState newState)
    {
      switch (newState.StateType)
      {
        case ComputingStateType.Bootstrapping:
          this.InitializeAppTheme().AsBackgroundTask("Application theme initialization", this.logger, this.analyticsProvider);
          break;
        case ComputingStateType.ShuttingDown:
          if (this.themeUpdateToken == null)
            break;
          this.eventAggregator.GetEvent<SystemThemeEvent>().Unsubscribe(this.themeUpdateToken);
          break;
      }
    }

    private async void OnSystemThemeUpdated()
    {
      AppThemeSettingType settingTheme = await this.GetSavedAppTheme().ConfigureAwait(false);
      switch (settingTheme)
      {
        case AppThemeSettingType.Unknown:
        case AppThemeSettingType.System:
          this.ApplyTheme(settingTheme);
          break;
      }
    }

    private async Task<bool> InvokeUriInternal(Uri uri)
    {
      Process process = (Process) null;
      bool flag;
      try
      {
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        process = new Process()
        {
          StartInfo = new ProcessStartInfo(uri.ToString()),
          EnableRaisingEvents = true
        };
        process.Exited += (EventHandler) ((s, a) =>
        {
          this.logger.WriteDebugEx(string.Format("Process {0} launching {1} exited: {2}", (object) process.Id, (object) uri, (object) process.ExitCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\ApplicationHandler.cs", nameof (InvokeUriInternal));
          taskCompletionSource.TrySetResult(true);
        });
        process.Start();
        flag = await taskCompletionSource.Task.ConfigureAwait(false);
      }
      finally
      {
        process?.Dispose();
      }
      return flag;
    }

    private bool GetChildWindow<TChild>(out TChild child) where TChild : Window, new()
    {
      child = this.App.MainWindow.OwnedWindows.OfType<TChild>().FirstOrDefault<TChild>();
      if ((object) child != null)
        return false;
      ref TChild local = ref child;
      TChild child1 = new TChild();
      child1.Owner = this.App.MainWindow;
      local = child1;
      return true;
    }
  }
}
