// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.FeatureSettings
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using System;
using System.Collections.Generic;

namespace Okta.Authenticator.NativeApp.Configuration
{
  public class FeatureSettings : IFeatureSettings
  {
    private readonly IConfigurationManager configurationManager;
    private readonly ILogger logger;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly Lazy<HashSet<FeatureType>> configuredFeatures;
    private readonly Lazy<DeviceHealthOptions> deviceHealthOptions;

    public FeatureSettings(
      IConfigurationManager configurationManager,
      ILogger logger,
      IAnalyticsProvider analytics)
    {
      this.configurationManager = configurationManager;
      this.logger = logger;
      this.analyticsProvider = analytics;
      this.configuredFeatures = new Lazy<HashSet<FeatureType>>(new Func<HashSet<FeatureType>>(this.LoadConfiguredFeatures), true);
      this.deviceHealthOptions = new Lazy<DeviceHealthOptions>(new Func<DeviceHealthOptions>(this.LoadDeviceHealthSetting), true);
    }

    public DeviceHealthOptions DeviceHealthUISetting => this.deviceHealthOptions.Value;

    internal HashSet<FeatureType> ConfiguredFeatures => this.configuredFeatures.Value;

    public bool IsFeatureEnabled(FeatureType feature) => feature != FeatureType.Unknown && this.configuredFeatures.Value.Contains(feature);

    private HashSet<FeatureType> LoadConfiguredFeatures()
    {
      try
      {
        return this.configurationManager.GetConfigFlags<FeatureType>(this.logger);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("An error occurred while retrieving configured features", ex, this.analyticsProvider);
        return new HashSet<FeatureType>();
      }
    }

    private DeviceHealthOptions LoadDeviceHealthSetting()
    {
      try
      {
        return this.configurationManager.GetDeviceHealthOptions(this.logger);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("An error occurred while retrieving configured features", ex, this.analyticsProvider);
        return DeviceHealthOptions.Enabled;
      }
    }
  }
}
