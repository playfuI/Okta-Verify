// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInfo.OktaVerifyApplicationInfoManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authenticator.Integrations;
using System;
using System.IO;

namespace Okta.Authenticator.NativeApp.ApplicationInfo
{
  public class OktaVerifyApplicationInfoManager : 
    IApplicationInfoManager,
    IAuthenticatorPluginConfiguration
  {
    private readonly Lazy<string> pluginPathInitializer;
    private readonly Lazy<bool> requireFreshSignal;
    private readonly Lazy<int> signalCollectionTimeout;
    private readonly Lazy<int> reinitializationInterval;
    private readonly ILogger logger;
    private readonly IConfigurationManager configurationManager;
    private readonly IAnalyticsProvider analyticsProvider;

    public OktaVerifyApplicationInfoManager(
      ILogger logger,
      IConfigurationManager configurationManager,
      IAnalyticsProvider analytics)
    {
      this.logger = logger;
      this.configurationManager = configurationManager;
      this.analyticsProvider = analytics;
      this.pluginPathInitializer = new Lazy<string>(new Func<string>(this.GetPluginDirectory), true);
      this.requireFreshSignal = new Lazy<bool>(new Func<bool>(this.GetRequireFreshSignal), true);
      this.signalCollectionTimeout = new Lazy<int>(new Func<int>(this.GetSignalCollectionTimeout), true);
      this.reinitializationInterval = new Lazy<int>(new Func<int>(this.GetReinitializationInterval), true);
    }

    public string PluginDirectory => this.pluginPathInitializer.Value;

    public bool RequireFreshSignal => this.requireFreshSignal.Value;

    public int SignalCollectionTimeout => this.signalCollectionTimeout.Value;

    public int ReinitializationInterval => this.reinitializationInterval.Value;

    public bool CheckIfInRemoteSession()
    {
      try
      {
        return WindowsHelloCredentialManager.CheckIsWindowsHelloSupported() == WindowsHelloCredentialManager.WindowsHelloSupportStatus.NotSupportRemoteSession;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Faied to check remote session: " + ex.Message, ex, this.analyticsProvider);
        return false;
      }
    }

    private string GetPluginDirectory()
    {
      if (this.GetValueFromRegistry("DisablePlugins", 0) == 1)
        return (string) null;
      if (!Directory.Exists(this.configurationManager.GlobalPluginManifestFileDirectory))
        Directory.CreateDirectory(this.configurationManager.GlobalPluginManifestFileDirectory);
      return this.configurationManager.GlobalPluginManifestFileDirectory;
    }

    private bool GetRequireFreshSignal() => this.GetValueFromRegistry("RequireFreshSignal", 1000) == 1;

    private int GetSignalCollectionTimeout() => this.GetValueFromRegistry("CollectionTimeout", 5000);

    private int GetReinitializationInterval() => this.GetValueFromRegistry("ReInitializationInterval", 3600);

    private int GetValueFromRegistry(string key, int defaultValue) => this.configurationManager.TryGetMachineRegistryConfig<int>(this.logger, "Integrations", key, defaultValue, (Func<object, int>) null);
  }
}
