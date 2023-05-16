// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.DeviceHealthViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class DeviceHealthViewModel : BaseViewModel
  {
    private const int CheckOsConfigInterval = 24;
    private readonly IClientSignInManager signInManager;
    private readonly IDeviceConfigurationManager deviceConfigurationManager;
    private readonly bool launchFromAccountListView;
    private Version cachedOsVersion;

    public DeviceHealthViewModel(bool fromAccountListView = false)
    {
      this.signInManager = AppInjector.Get<IClientSignInManager>();
      this.deviceConfigurationManager = AppInjector.Get<IDeviceConfigurationManager>();
      this.launchFromAccountListView = fromAccountListView;
      this.BackToSettingsViewCommand = (ICommand) new DelegateCommand(new Action(this.BackToSettingsView));
      this.LaunchHelpLinkCommand = (ICommand) new DelegateCommand(new Action(this.LaunchHelpLink));
      this.LastFetchTime = new DateTime();
      this.InitializationTask = this.InitializeHealthState();
    }

    public IEnumerable<DeviceHealthAttributeViewModel> AllDeviceHealthAttributes
    {
      get
      {
        ObservableCollection<DeviceHealthAttributeViewModel> healthAttributes = new ObservableCollection<DeviceHealthAttributeViewModel>();
        if (this.IsShowOSUpdate)
          healthAttributes.Add(this.SystemHealth);
        healthAttributes.Add(this.BiometricsHealth);
        healthAttributes.Add(this.DiskEncryptionHealth);
        return (IEnumerable<DeviceHealthAttributeViewModel>) healthAttributes;
      }
    }

    public bool SettingsLoaded { get; private set; }

    public string DeviceName { get; private set; }

    public string OverallStatusLabel { get; private set; }

    public bool IsOverallStatusHealthy { get; private set; }

    public ICommand BackToSettingsViewCommand { get; }

    public ICommand LaunchHelpLinkCommand { get; }

    public DeviceHealthAttributeViewModel SystemHealth { get; private set; }

    public DeviceHealthAttributeViewModel BiometricsHealth { get; private set; }

    public DeviceHealthAttributeViewModel DiskEncryptionHealth { get; private set; }

    public bool IsShowOSUpdate { get; private set; }

    internal Task InitializationTask { get; }

    internal DateTime LastFetchTime { get; set; }

    internal bool OsVersionFetchSuccess { get; private set; }

    internal async Task InitializeHealthState()
    {
      DeviceHealthViewModel deviceHealthViewModel1 = this;
      deviceHealthViewModel1.IsShowOSUpdate = deviceHealthViewModel1.deviceConfigurationManager.IsOSUpdateEnabled;
      deviceHealthViewModel1.SettingsLoaded = false;
      (string, Version, bool?, bool) settings = await Task.Run<(string, Version, bool?, bool)>(new Func<(string, Version, bool?, bool)>(deviceHealthViewModel1.LoadLocalDeviceSettings)).ConfigureAwait(false);
      TimeSpan timeSpan = DateTime.UtcNow - deviceHealthViewModel1.LastFetchTime;
      if (!deviceHealthViewModel1.launchFromAccountListView || deviceHealthViewModel1.deviceConfigurationManager.IsDeviceHealthCheckEnabled && timeSpan.TotalHours >= 24.0)
      {
        Version version = await deviceHealthViewModel1.deviceConfigurationManager.GetLatestVersion().ConfigureAwait(false);
        deviceHealthViewModel1.cachedOsVersion = version;
        if (deviceHealthViewModel1.cachedOsVersion != (Version) null)
        {
          deviceHealthViewModel1.LastFetchTime = DateTime.UtcNow;
          deviceHealthViewModel1.OsVersionFetchSuccess = true;
        }
        else
          deviceHealthViewModel1.OsVersionFetchSuccess = false;
      }
      deviceHealthViewModel1.DeviceName = settings.Item1;
      deviceHealthViewModel1.SystemHealth = deviceHealthViewModel1.BuildSystemHealthVM(settings.Item2, deviceHealthViewModel1.cachedOsVersion);
      deviceHealthViewModel1.DiskEncryptionHealth = deviceHealthViewModel1.BuildDiskEncryptionVM(settings.Item3);
      DeviceHealthViewModel deviceHealthViewModel2 = deviceHealthViewModel1;
      DeviceHealthAttributeViewModel attributeViewModel;
      if (!settings.Item4)
      {
        string compositeResource1 = ResourceExtensions.ExtractCompositeResource(Resources.DeviceHealthEnableWinHello);
        string compositeResource2 = ResourceExtensions.ExtractCompositeResource(Resources.DeviceHealthWinHelloHelpsProtectData);
        string configurationLink = deviceHealthViewModel1.deviceConfigurationManager.WindowsHelloConfigurationLink;
        string compositeResource3 = ResourceExtensions.ExtractCompositeResource(Resources.DeviceHealthEnableWinHello);
        string remediationLink = configurationLink;
        int num = deviceHealthViewModel1.launchFromAccountListView ? 1 : 0;
        attributeViewModel = new DeviceHealthAttributeViewModel(compositeResource1, false, compositeResource2, compositeResource3, remediationLink, launchFromAccountListView: num != 0);
      }
      else
        attributeViewModel = new DeviceHealthAttributeViewModel(ResourceExtensions.ExtractCompositeResource(Resources.DeviceHealthWinHelloEnabled), true, deviceHealthViewModel1.launchFromAccountListView);
      deviceHealthViewModel2.BiometricsHealth = attributeViewModel;
      deviceHealthViewModel1.UpdateOverallStatus();
      deviceHealthViewModel1.SettingsLoaded = true;
      deviceHealthViewModel1.FireViewModelChangedEvent();
      settings = ();
    }

    private void BackToSettingsView()
    {
      MainViewStateEvent mainViewStateEvent = this.EventAggregator.GetEvent<MainViewStateEvent>();
      if (this.launchFromAccountListView)
        mainViewStateEvent.Publish(new MainViewState(MainViewType.DeviceHealth, (INotifyPropertyChanged) this, false));
      else
        mainViewStateEvent.Publish(new MainViewState(MainViewType.Settings, (INotifyPropertyChanged) new SettingsViewModel()));
    }

    private (string Name, Version Current, bool? IsDiskEncrypted, bool IsWinHelloSet) LoadLocalDeviceSettings()
    {
      this.Logger.WriteInfoEx("Loading local device settings...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\DeviceHealthViewModel.cs", nameof (LoadLocalDeviceSettings));
      return (this.deviceConfigurationManager.DeviceName, this.deviceConfigurationManager.GetOSVersion(), this.deviceConfigurationManager.IsDiskEncrypted(), this.signInManager.CheckIfWindowsHelloSignInConfigured());
    }

    private DeviceHealthAttributeViewModel BuildSystemHealthVM(Version current, Version latest)
    {
      if ((!(current != (Version) null) ? 0 : ((object) latest != null ? (latest.CompareTo(current) <= 0 ? 1 : 0) : 0)) != 0)
        return new DeviceHealthAttributeViewModel(Resources.DeviceHealthWindowsUpToDate, true, this.launchFromAccountListView);
      if (latest != (Version) null)
      {
        string healthUpdateWindows1 = Resources.DeviceHealthUpdateWindows;
        string keepYourDeviceSafe = Resources.DeviceHealthKeepYourDeviceSafe;
        string configurationLink = this.deviceConfigurationManager.WindowsUpdateConfigurationLink;
        string str1 = string.Format("{0}: {1}", (object) Resources.DeviceHealthYourOSVersion, (object) current);
        string str2 = string.Format("{0}: {1}", (object) Resources.DeviceHealthLatestOSVersion, (object) latest);
        string healthUpdateWindows2 = Resources.DeviceHealthUpdateWindows;
        string remediationLink = configurationLink;
        string currentStateHint = str1;
        string remediationHint = str2;
        int num = this.launchFromAccountListView ? 1 : 0;
        return new DeviceHealthAttributeViewModel(healthUpdateWindows1, false, keepYourDeviceSafe, healthUpdateWindows2, remediationLink, currentStateHint, remediationHint, num != 0);
      }
      string couldntCheckOsVersion = Resources.DeviceHealthCouldntCheckOSVersion;
      string configurationLink1 = this.deviceConfigurationManager.WindowsUpdateConfigurationLink;
      string problemCheckingUpdates = Resources.DeviceHealthProblemCheckingUpdates;
      string str = string.Format("{0}: {1}", (object) Resources.DeviceHealthYourOSVersion, (object) current);
      string healthCheckForUpdates = Resources.DeviceHealthCheckForUpdates;
      string remediationLink1 = configurationLink1;
      string currentStateHint1 = str;
      int num1 = this.launchFromAccountListView ? 1 : 0;
      return new DeviceHealthAttributeViewModel(couldntCheckOsVersion, false, problemCheckingUpdates, healthCheckForUpdates, remediationLink1, currentStateHint1, launchFromAccountListView: num1 != 0);
    }

    private DeviceHealthAttributeViewModel BuildDiskEncryptionVM(bool? isDiskEncrypted)
    {
      bool? nullable1 = isDiskEncrypted;
      bool flag1 = true;
      if (nullable1.GetValueOrDefault() == flag1 & nullable1.HasValue)
        return new DeviceHealthAttributeViewModel(Resources.DeviceHealthDiskEncrypted, true, this.launchFromAccountListView, Visibility.Hidden);
      bool? nullable2 = isDiskEncrypted;
      bool flag2 = false;
      if (!(nullable2.GetValueOrDefault() == flag2 & nullable2.HasValue))
        return new DeviceHealthAttributeViewModel(Resources.DeviceHealthCouldntCheckEncryption, false, Resources.DeviceHealthProblemCheckingEncryption, launchFromAccountListView: this.launchFromAccountListView, isHideSeparator: Visibility.Hidden);
      string diskNotEncrypted = Resources.DeviceHealthDiskNotEncrypted;
      string providesProtection = Resources.DeviceHealthDiskEncryptionProvidesProtection;
      string configurationLink = this.deviceConfigurationManager.EncrytionConfigurationLink;
      string deviceHealthLearnMore = Resources.DeviceHealthLearnMore;
      string remediationLink = configurationLink;
      int num = this.launchFromAccountListView ? 1 : 0;
      return new DeviceHealthAttributeViewModel(diskNotEncrypted, false, providesProtection, deviceHealthLearnMore, remediationLink, launchFromAccountListView: num != 0, isHideSeparator: Visibility.Hidden);
    }

    private void UpdateOverallStatus()
    {
      List<DeviceHealthAttributeViewModel> source = new List<DeviceHealthAttributeViewModel>();
      if (this.IsShowOSUpdate)
        source.Add(this.SystemHealth);
      source.Add(this.DiskEncryptionHealth);
      source.Add(this.BiometricsHealth);
      int num = source.Count<DeviceHealthAttributeViewModel>((Func<DeviceHealthAttributeViewModel, bool>) (a => !a.IsHealthy));
      if (!this.SystemHealth.IsHealthy && !this.OsVersionFetchSuccess)
        --num;
      this.IsOverallStatusHealthy = num == 0;
      this.OverallStatusLabel = this.IsOverallStatusHealthy ? Resources.DeviceHealthUpToDate : string.Format("{0}: {1}", (object) Resources.DeviceHealthWarnings, (object) num);
    }

    private void LaunchHelpLink() => this.LaunchLink("https://help.okta.com/okta_help.htm?type=eu&id=ext-device-health-windows");
  }
}
