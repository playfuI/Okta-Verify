// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.DeviceConfigurationManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Signals;
using Okta.Devices.SDK.WebClient;
using Okta.Devices.SDK.Windows.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Configuration
{
  public class DeviceConfigurationManager : IDeviceConfigurationManager
  {
    private const string SettingsLinkOSUpdate = "ms-settings:windowsupdate";
    private const string SettingsLinkDiskEncryption = "ms-settings:deviceencryption";
    private const string SettingsLinkSignInOptions = "ms-settings:signinoptions";
    private const string SettingsLinkSignInOptionsFace = "ms-settings:signinoptions-launchfaceenrollment";
    private const string SettingsLinkSignInOptionsFingerprint = "ms-settings:signinoptions-launchfingerprintenrollment";
    private const string OSCheckMaxAttemptsEvent = "OSCheckMaxAttemptsReached";
    private const int OSCheckMaxAttempts = 5;
    private readonly ILogger logger;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IFeatureSettings featureSettings;
    private readonly Lazy<WindowsBiometricTypes> sensorsInitializer;
    private readonly IClientAccountManager accountsManager;
    private readonly IPublicKeyList publicKeyList;
    private string deviceName;
    private bool? isPhishingResistanceMessageEnabled;

    public DeviceConfigurationManager(
      ILogger logger,
      IAnalyticsProvider analyticsProvider,
      IFeatureSettings featureSettings,
      IClientAccountManager accountsManager,
      IPublicKeyList publicKeyList)
    {
      this.logger = logger;
      this.analyticsProvider = analyticsProvider;
      this.featureSettings = featureSettings;
      this.accountsManager = accountsManager;
      this.sensorsInitializer = new Lazy<WindowsBiometricTypes>(new Func<WindowsBiometricTypes>(this.GetSupportedSensors));
      this.publicKeyList = publicKeyList;
    }

    public string DeviceName
    {
      get
      {
        if (this.deviceName == null)
          this.deviceName = this.RetrieveDeviceName();
        return this.deviceName;
      }
    }

    public string WindowsHelloConfigurationLink => DeviceConfigurationManager.SelectWindowsHelloConfigurationLink(this.sensorsInitializer.Value);

    public string WindowsUpdateConfigurationLink => "ms-settings:windowsupdate";

    public string EncrytionConfigurationLink => "ms-settings:deviceencryption";

    public string SignInOptionsConfigurationLink => "ms-settings:signinoptions";

    public bool IsDeviceHealthCheckEnabled => this.featureSettings.DeviceHealthUISetting != DeviceHealthOptions.Disabled;

    public bool IsOSUpdateEnabled => this.featureSettings.DeviceHealthUISetting != DeviceHealthOptions.HideOSUpdate;

    public bool IsPhishingResistanceMessageEnabled
    {
      get
      {
        if (!this.isPhishingResistanceMessageEnabled.HasValue)
        {
          this.isPhishingResistanceMessageEnabled = new bool?(!this.featureSettings.IsFeatureEnabled(FeatureType.PhishingResistanceMessageOff));
          this.logger.WriteInfoEx(string.Format("Phishing resistance message enabled: {0}", (object) this.isPhishingResistanceMessageEnabled), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\DeviceConfigurationManager.cs", nameof (IsPhishingResistanceMessageEnabled));
        }
        return this.isPhishingResistanceMessageEnabled.Value;
      }
    }

    public Version GetOSVersion()
    {
      try
      {
        Version result;
        if (Version.TryParse(DevicesSdk.DeviceInformation.OsVersion, out result))
          return result;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when retrieving the device OS version", ex, this.analyticsProvider);
      }
      return (Version) null;
    }

    public async Task<Version> GetLatestVersion()
    {
      IList<IOktaAccount> accounts = await this.accountsManager.GetAccounts();
      if (accounts == null || accounts.Count == 0)
        return (Version) null;
      try
      {
        this.logger.WriteInfoEx("Request to fetch OS versions...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\DeviceConfigurationManager.cs", nameof (GetLatestVersion));
        int currentAttempts = 0;
        IEnumerable<string> strings = accounts.Select<IOktaAccount, string>((Func<IOktaAccount, string>) (a => a.Domain)).Distinct<string>();
        IOktaWebClient client = DevicesSdk.WebClient;
        foreach (string urlOrHost in strings)
        {
          string validURL;
          Uri result;
          if (Okta.Authenticator.NativeApp.Extensions.NormalizeWebAddress(urlOrHost, out validURL) && Uri.TryCreate(validURL, UriKind.Absolute, out result))
          {
            (IOktaOsVersionInfo[] versionsInfo, HttpStatusCode httpStatusCode) = await this.FetchLatestVersions(client, result).ConfigureAwait(false);
            ++currentAttempts;
            switch (httpStatusCode)
            {
              case HttpStatusCode.OK:
                return this.SelectLatestVersion(versionsInfo);
              case HttpStatusCode.Unauthorized:
                if (currentAttempts >= 5)
                {
                  this.logger.WriteWarningEx(string.Format("Reached {0} attempts fetching OS versions; abort checking on other orgs...", (object) currentAttempts), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\DeviceConfigurationManager.cs", nameof (GetLatestVersion));
                  this.analyticsProvider.TrackEvent("OSCheckMaxAttemptsReached");
                  return (Version) null;
                }
                continue;
              default:
                this.logger.WriteErrorEx(string.Format("Unexcepted response while fetching OS versions: {0}; abort checking on other orgs...", (object) httpStatusCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\DeviceConfigurationManager.cs", nameof (GetLatestVersion));
                return (Version) null;
            }
          }
        }
        client = (IOktaWebClient) null;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error while fetching OS versions", ex, this.analyticsProvider);
      }
      return (Version) null;
    }

    public bool? IsDiskEncrypted()
    {
      try
      {
        DiskEncryptionType diskEncryptionType = DevicesSdk.DeviceInformation.DiskEncryptionType;
        if (diskEncryptionType != DiskEncryptionType.Unknown)
          return new bool?(diskEncryptionType != DiskEncryptionType.None);
        this.logger.WriteWarningEx("Unable to determine the disk encryption", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\DeviceConfigurationManager.cs", nameof (IsDiskEncrypted));
        return new bool?();
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when checking disk encryption info", ex, this.analyticsProvider);
      }
      return new bool?();
    }

    private static string SelectWindowsHelloConfigurationLink(WindowsBiometricTypes sensors)
    {
      if (sensors.HasFlag((System.Enum) WindowsBiometricTypes.FacialFeatures))
        return "ms-settings:signinoptions-launchfaceenrollment";
      return sensors.HasFlag((System.Enum) WindowsBiometricTypes.Fingerprint) ? "ms-settings:signinoptions-launchfingerprintenrollment" : "ms-settings:signinoptions";
    }

    private WindowsBiometricTypes GetSupportedSensors()
    {
      try
      {
        return WindowsHelloCredentialManager.CheckSupportedSensors();
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when checking supported sensors", ex, this.analyticsProvider);
      }
      return WindowsBiometricTypes.None;
    }

    private string RetrieveDeviceName()
    {
      try
      {
        return DevicesSdk.DeviceInformation.DeviceName;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when retrieving the device name", ex, this.analyticsProvider);
      }
      return string.Empty;
    }

    private async Task<(IOktaOsVersionInfo[] Versions, HttpStatusCode StatusCode)> FetchLatestVersions(
      IOktaWebClient webClient,
      Uri orgUri)
    {
      bool bypassSecureConnection = !this.publicKeyList.IsPinnedUrl(orgUri);
      using (new SecureConnectionOperation(DevicesSdk.ConnectionValidator, orgUri, this.logger, bypassSecureConnection))
      {
        try
        {
          return ((await webClient.GetOktaVerifyConfigurations(orgUri).ConfigureAwait(false)).WindowsVersions, HttpStatusCode.OK);
        }
        catch (OktaWebException ex)
        {
          return ((IOktaOsVersionInfo[]) null, ex.StatusCode);
        }
      }
    }

    private Version SelectLatestVersion(IOktaOsVersionInfo[] versionsInfo)
    {
      IOrderedEnumerable<IOktaOsVersionInfo> source = versionsInfo != null ? ((IEnumerable<IOktaOsVersionInfo>) versionsInfo).Where<IOktaOsVersionInfo>((Func<IOktaOsVersionInfo, bool>) (v => v.IsLatest)).OrderBy<IOktaOsVersionInfo, string>((Func<IOktaOsVersionInfo, string>) (v => v.OsVersion)) : (IOrderedEnumerable<IOktaOsVersionInfo>) null;
      if (!source.Any<IOktaOsVersionInfo>())
        return (Version) null;
      Version osVersion = this.GetOSVersion();
      string toCompare = string.Format("{0}.{1}.{2}", (object) osVersion.Major, (object) osVersion.Minor, (object) osVersion.Build);
      IOktaOsVersionInfo oktaOsVersionInfo = source.FirstOrDefault<IOktaOsVersionInfo>((Func<IOktaOsVersionInfo, bool>) (v => v.OsVersion.CompareTo(toCompare) >= 0));
      return oktaOsVersionInfo == null ? new Version(source.Last<IOktaOsVersionInfo>().OsVersion) : new Version(oktaOsVersionInfo.OsVersion);
    }
  }
}
