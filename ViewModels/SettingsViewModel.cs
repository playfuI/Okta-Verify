// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.SettingsViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Installation;
using Okta.Authenticator.NativeApp.Models;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK.Extensions;
using Prism.Commands;
using Prism.Events;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class SettingsViewModel : BaseViewModel
  {
    private readonly IEventAggregator eventAggregator;
    private readonly IAnalyticsRepository analyticsRepository;
    private readonly IAppInstallationManager appInstallationManager;
    private readonly IDeviceConfigurationManager deviceConfigurationManager;
    private AppThemeSettingType selectedTheme;

    public SettingsViewModel()
    {
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.analyticsRepository = AppInjector.Get<IAnalyticsRepository>();
      this.appInstallationManager = AppInjector.Get<IAppInstallationManager>();
      this.deviceConfigurationManager = AppInjector.Get<IDeviceConfigurationManager>();
      this.CloseSettingsCommand = (ICommand) new DelegateCommand(new Action(this.CloseSettings));
      this.UpdateThemeCommand = (ICommand) new DelegateCommand<AppThemeSettingType?>(new Action<AppThemeSettingType?>(this.UpdateThemeAction));
      this.UpdateAnalyticsReportCommand = (ICommand) new DelegateCommand<bool?>(new Action<bool?>(this.UpdateAnalyticsAction));
      this.UpdateBetaProgramCommand = (ICommand) new DelegateCommand<bool?>(new Action<bool?>(this.UpdateBetaProgramAction));
      this.ViewDeviceHealthCommand = (ICommand) new DelegateCommand(new Action(this.ViewDeviceHealth));
      this.InitializationTask = this.Initialize();
    }

    public ICommand CloseSettingsCommand { get; }

    public ICommand UpdateThemeCommand { get; }

    public ICommand UpdateAnalyticsReportCommand { get; }

    public ICommand UpdateBetaProgramCommand { get; }

    public ICommand ViewDeviceHealthCommand { get; }

    public bool IsLightThemeSelected => this.selectedTheme == AppThemeSettingType.Light;

    public bool IsDarkThemeSelected => this.selectedTheme == AppThemeSettingType.Dark;

    public bool IsSystemSyncSelected => this.selectedTheme == AppThemeSettingType.System;

    public bool AreAnalyticsRunning { get; private set; }

    public bool CanUpdateAnalytics { get; private set; }

    public bool CanUpdateBetaProgramSetting { get; private set; }

    public bool IsBetaProgramChecked { get; private set; }

    public bool IsDeviceHealthCheckEnabled { get; private set; }

    internal Task InitializationTask { get; }

    private async Task Initialize()
    {
      SettingsViewModel settingsViewModel = this;
      AppThemeSettingType appTheme = await settingsViewModel.ApplicationHandler.GetSavedAppTheme().ConfigureAwait(false);
      if (appTheme == AppThemeSettingType.Unknown)
        appTheme = settingsViewModel.ApplicationHandler.DefaultAppTheme;
      settingsViewModel.UpdateSelectedTheme(appTheme);
      bool flag1 = await settingsViewModel.analyticsRepository.IsEnabled().ConfigureAwait(false);
      settingsViewModel.AreAnalyticsRunning = flag1;
      settingsViewModel.CanUpdateAnalytics = !settingsViewModel.analyticsRepository.IsPreConfigured;
      settingsViewModel.CanUpdateBetaProgramSetting = !settingsViewModel.appInstallationManager.IsReleaseChannelPreConfigured;
      bool flag2 = await settingsViewModel.appInstallationManager.GetBetaProgramStateAsync().ConfigureAwait(false);
      settingsViewModel.IsBetaProgramChecked = flag2;
      settingsViewModel.IsDeviceHealthCheckEnabled = settingsViewModel.deviceConfigurationManager.IsDeviceHealthCheckEnabled;
      settingsViewModel.FireViewModelChangedEvent();
      settingsViewModel.Logger.WriteInfoEx(string.Format("Analytics settings loaded: Analytics running: {0} - Can be updated: {1}", (object) settingsViewModel.AreAnalyticsRunning, (object) settingsViewModel.CanUpdateAnalytics), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SettingsViewModel.cs", nameof (Initialize));
      settingsViewModel.Logger.WriteInfoEx(string.Format("Beta settings loaded: Enrolled: {0} - Can be changed: {1}", (object) settingsViewModel.IsBetaProgramChecked, (object) settingsViewModel.CanUpdateBetaProgramSetting), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SettingsViewModel.cs", nameof (Initialize));
    }

    private void CloseSettings() => this.eventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.Settings, (INotifyPropertyChanged) this, false));

    private void UpdateThemeAction(AppThemeSettingType? appTheme) => this.UpdateTheme(appTheme).AsBackgroundTask("application theme settings update", this.Logger, this.AnalyticsProvider);

    private async Task UpdateTheme(AppThemeSettingType? appTheme)
    {
      SettingsViewModel settingsViewModel = this;
      if (!appTheme.HasValue)
        return;
      AppThemeSettingType? nullable = appTheme;
      AppThemeSettingType selectedTheme = settingsViewModel.selectedTheme;
      if (nullable.GetValueOrDefault() == selectedTheme & nullable.HasValue)
        return;
      bool flag = await settingsViewModel.ApplicationHandler.TryUpdateAppTheme(appTheme.Value).ConfigureAwait(true);
      settingsViewModel.UpdateSelectedTheme(flag ? appTheme.Value : AppThemeSettingType.Unknown);
    }

    private void UpdateSelectedTheme(AppThemeSettingType appTheme)
    {
      this.selectedTheme = appTheme;
      this.FirePropertyChangedEvent("IsLightThemeSelected");
      this.FirePropertyChangedEvent("IsDarkThemeSelected");
      this.FirePropertyChangedEvent("IsSystemSyncSelected");
    }

    private void UpdateAnalyticsAction(bool? enable) => this.UpdateAnalyticsReport(enable).AsBackgroundTask("application error reporting settings update", this.Logger, this.AnalyticsProvider);

    private async Task UpdateAnalyticsReport(bool? enable)
    {
      SettingsViewModel settingsViewModel = this;
      bool prevState = settingsViewModel.AreAnalyticsRunning;
      bool? nullable = enable;
      bool flag1 = prevState;
      if (nullable.GetValueOrDefault() == flag1 & nullable.HasValue)
        return;
      bool flag2 = await settingsViewModel.analyticsRepository.Configure(enable.Value).ConfigureAwait(true);
      settingsViewModel.AreAnalyticsRunning = flag2 ? enable.Value : prevState;
      settingsViewModel.FirePropertyChangedEvent("AreAnalyticsRunning");
      settingsViewModel.Logger.WriteInfoEx(string.Format("Analytics report updated: {0}", (object) flag2), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SettingsViewModel.cs", nameof (UpdateAnalyticsReport));
    }

    private void UpdateBetaProgramAction(bool? enable) => this.UpdateBetaProgramSetting(enable).AsBackgroundTask("application beta program settings update", this.Logger, this.AnalyticsProvider);

    private async Task UpdateBetaProgramSetting(bool? enable)
    {
      SettingsViewModel settingsViewModel = this;
      bool prevState = settingsViewModel.IsBetaProgramChecked;
      bool? nullable = enable;
      bool flag1 = prevState;
      if (nullable.GetValueOrDefault() == flag1 & nullable.HasValue)
        return;
      bool flag2 = await settingsViewModel.appInstallationManager.ConfigureBetaProgramSettingAsync(enable.Value).ConfigureAwait(true);
      settingsViewModel.IsBetaProgramChecked = flag2 ? enable.Value : prevState;
      settingsViewModel.FirePropertyChangedEvent("IsBetaProgramChecked");
      settingsViewModel.Logger.WriteInfoEx(string.Format("Beta enrollment updated: {0}", (object) flag2), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SettingsViewModel.cs", nameof (UpdateBetaProgramSetting));
    }

    private void ViewDeviceHealth()
    {
      if (!this.IsDeviceHealthCheckEnabled)
      {
        this.Logger.WriteWarningEx("Device health disabled; skip check", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SettingsViewModel.cs", nameof (ViewDeviceHealth));
      }
      else
      {
        this.EventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.DeviceHealth, (INotifyPropertyChanged) new DeviceHealthViewModel()));
        this.analyticsRepository?.TrackEvent("ShowDeviceHealthScreenFromSetting");
      }
    }
  }
}
