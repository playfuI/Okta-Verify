// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.TelemetryDataManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.Sandbox;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Windows.Native;
using Okta.Devices.SDK.Windows.Native.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public class TelemetryDataManager : ITelemetryDataManager
  {
    private const string WindowsTypeInformationRegistryPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion";
    private static readonly string[] WindowsTypeInformationValues = new string[3]
    {
      "CompositionEditionID",
      "EditionID",
      "InstallationType"
    };
    private static readonly TimeSpan MaxCachingTime = TimeSpan.FromDays(1.0);
    private readonly ILogger logger;
    private readonly IConfigurationManager configurationManager;
    private readonly IAuthenticatorSandboxManager sandboxManager;
    private readonly IAnalyticsRepository analyticsRepository;
    private readonly IClientAccountManager accountManager;
    private readonly IDictionary<AppTelemetryDataKey, CachedTelemetryDataPoint> dataCache;

    public TelemetryDataManager(
      ILogger logger,
      IConfigurationManager configurationManager,
      IClientAccountManager accountManager,
      IAuthenticatorSandboxManager sandboxManager,
      IAnalyticsRepository analyticsRepository)
    {
      this.logger = logger;
      this.configurationManager = configurationManager;
      this.accountManager = accountManager;
      this.sandboxManager = sandboxManager;
      this.analyticsRepository = analyticsRepository;
      this.dataCache = (IDictionary<AppTelemetryDataKey, CachedTelemetryDataPoint>) new Dictionary<AppTelemetryDataKey, CachedTelemetryDataPoint>();
      this.AddDataPoint(AppTelemetryDataKey.ManagementConfiguration, new Func<(object, bool)>(this.GetManagementStatus));
      this.AddDataPoint(AppTelemetryDataKey.MachineJoinStatus, new Func<(object, bool)>(this.GetMachineJoinStatus));
      this.AddDataPoint(AppTelemetryDataKey.BiometricLogonProviderStatus, new Func<(object, bool)>(TelemetryDataManager.GetWindowsHelloStatus));
      this.AddDataPoint(AppTelemetryDataKey.KeyProtection, new Func<(object, bool)>(this.GetKeyProtectionStatus));
      this.AddDataPoint(AppTelemetryDataKey.OktaOrgDomain, new Func<(object, bool)>(this.GetOrgInformation));
      this.AddDataPoint(AppTelemetryDataKey.EnrolledAccountCount, new Func<(object, bool)>(this.GetAccountCountInformation));
      this.AddDataPoint(AppTelemetryDataKey.EnrolledAccountTypes, new Func<(object, bool)>(this.GetAccountTypesInformation));
      this.AddDataPoint(AppTelemetryDataKey.OperatingSystemType, new Func<(object, bool)>(this.GetWindowsTypeInformation));
      this.AddDataPoint(AppTelemetryDataKey.MachineType, new Func<(object, bool)>(this.GetMachineTypeInformation));
      this.AddDataPoint(AppTelemetryDataKey.DeviceName, new Func<(object, bool)>(this.GetDeviceNameInformation));
      this.AddDataPoint(AppTelemetryDataKey.DeviceTraceId, new Func<(object, bool)>(this.GetDeviceTraceIdInformation));
    }

    private bool NoAccountData => this.accountManager?.Accounts == null || !this.accountManager.AnyAccounts();

    public (AppTelemetryDataKey Key, object Value) this[AppTelemetryDataKey key] => this.GetDataPoint(key);

    public IDictionary<AppTelemetryDataKey, object> GetAllTelemetryDataPoints() => this.GetDataPoints<AppTelemetryDataKey, object>((IEnumerable<AppTelemetryDataKey>) this.dataCache.Keys, (Func<object, AppTelemetryDataKey>) (k => (AppTelemetryDataKey) k), (Func<object, object>) (v => v));

    public IDictionary<string, string> GetAllTelemetryDataPointsAsString() => this.GetDataPointsAsStrings((IEnumerable<AppTelemetryDataKey>) this.dataCache.Keys);

    public IDictionary<AppTelemetryDataKey, object> GetSpecificDataPoints(
      params AppTelemetryDataKey[] keys)
    {
      return this.GetDataPoints<AppTelemetryDataKey, object>((IEnumerable<AppTelemetryDataKey>) ((IEnumerable<AppTelemetryDataKey>) keys).ToArray<AppTelemetryDataKey>(), (Func<object, AppTelemetryDataKey>) (k => (AppTelemetryDataKey) k), (Func<object, object>) (v => v));
    }

    public IDictionary<string, string> GetSpecificDataPointsAsString(
      params AppTelemetryDataKey[] keys)
    {
      return this.GetDataPointsAsStrings((IEnumerable<AppTelemetryDataKey>) ((IEnumerable<AppTelemetryDataKey>) keys).ToArray<AppTelemetryDataKey>());
    }

    public bool AddCustomDataPoint(AppTelemetryDataKey key, object value)
    {
      if (this.dataCache.ContainsKey(key))
        return false;
      this.dataCache.Add(key, new CachedTelemetryDataPoint(key, this.logger, (Func<(object, bool)>) (() => (value, true)), new TimeSpan?(TelemetryDataManager.MaxCachingTime)));
      return true;
    }

    public void PersistDataInLog() => this.logger.WriteInfoEx(this.ToString(), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\TelemetryDataManager.cs", nameof (PersistDataInLog));

    public override string ToString()
    {
      StringBuilder stringBuilder = new StringBuilder("TelemetryDataManger:");
      foreach (KeyValuePair<string, string> dataPointsAsString in (IEnumerable<KeyValuePair<string, string>>) this.GetDataPointsAsStrings((IEnumerable<AppTelemetryDataKey>) this.dataCache.Keys))
        stringBuilder.Append(" " + dataPointsAsString.Key + ":" + dataPointsAsString.Value);
      return stringBuilder.ToString();
    }

    private static (object, bool) GetWindowsHelloStatus()
    {
      WindowsHelloCredentialManager.WindowsHelloSupportStatus helloSupportStatus = WindowsHelloCredentialManager.CheckIsWindowsHelloSupported();
      string str;
      if (helloSupportStatus == WindowsHelloCredentialManager.WindowsHelloSupportStatus.Supported)
      {
        bool? nullable = WindowsHelloCredentialManager.CheckIsWindowsHelloConfigured();
        str = nullable.HasValue ? (nullable.Value ? "Configured" : "NotConfigured") : "SupportedUnknownConfiguration";
      }
      else
        str = helloSupportStatus.ToString();
      return ((object) str, true);
    }

    private (object, bool) GetManagementStatus()
    {
      string str = "NotConfigured";
      string registryConfig = this.configurationManager.TryGetRegistryConfig<string>(this.logger, "SignInUrl", (string) null);
      if (!string.IsNullOrEmpty(registryConfig))
      {
        string validURL;
        str = !Okta.Authenticator.NativeApp.Extensions.NormalizeWebAddress(registryConfig, out validURL) || !Uri.TryCreate(validURL, UriKind.Absolute, out Uri _) ? "Misconfigured" : "Configured";
      }
      return ((object) str, true);
    }

    private (object, bool) GetMachineJoinStatus()
    {
      NativeOperationResult<MachineJoinStatusFlags> machineJoinStatus = NativeLibrary.GetMachineJoinStatus(this.logger);
      return ((object) machineJoinStatus.Result, machineJoinStatus.IsSuccess);
    }

    private (object, bool) GetKeyProtectionStatus() => ((object) string.Format("Sandbox{0} | {1}", (object) this.sandboxManager.SandboxState, NativeLibrary.GetKeyProviderImplementationType("Microsoft Platform Crypto Provider", this.logger).Unchecked() == KeyProviderImplementationTypes.Hardware ? (object) "HardwareProtected" : (object) "NotHardwareProtected"), true);

    private (object, bool) GetOrgInformation()
    {
      string orgUrlOrDomain = this.configurationManager.TryGetRegistryConfig<string>(this.logger, "SignInUrl", (string) null);
      if (string.IsNullOrEmpty(orgUrlOrDomain) && !this.NoAccountData)
        orgUrlOrDomain = this.accountManager.Accounts.FirstOrDefault<IOktaAccount>()?.Domain;
      return string.IsNullOrEmpty(orgUrlOrDomain) ? ((object) null, false) : ((object) Okta.Devices.SDK.Extensions.TelemetryExtensions.NormalizeOrgName(orgUrlOrDomain), true);
    }

    private (object, bool) GetAccountCountInformation() => this.NoAccountData ? ((object) 0, false) : ((object) this.accountManager.Accounts.Count<IOktaAccount>(), true);

    private (object, bool) GetAccountTypesInformation()
    {
      if (this.NoAccountData)
        return ((object) "None", false);
      int num1 = this.accountManager.Accounts.Count<IOktaAccount>();
      int num2 = this.accountManager.Accounts.Count<IOktaAccount>((Func<IOktaAccount, bool>) (a => a.IsUserVerificationEnabled));
      int num3 = this.accountManager.Accounts.GroupBy<IOktaAccount, string>((Func<IOktaAccount, string>) (a => a.OrgId)).Count<IGrouping<string, IOktaAccount>>();
      return ((object) ((num1 == num2 ? "All" : (num2 == 0 ? "No" : "Some")) + "UserVerification" + (num3 < num1 ? " | MultiAccount" : (string) null)), true);
    }

    private (object, bool) GetWindowsTypeInformation()
    {
      IEnumerable<(string, object)> registryValues = Registry.LocalMachine.GetRegistryValues("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", (IEnumerable<string>) TelemetryDataManager.WindowsTypeInformationValues);
      return ((object) string.Join<object>(" ", registryValues.Select<(string, object), object>((Func<(string, object), object>) (v => v.Item2))), registryValues != null && registryValues.Count<(string, object)>() > 0);
    }

    private (object, bool) GetMachineTypeInformation()
    {
      (int TotalMemory, int VirtualMemory, SystemInfoProcessorArchitecture Architecture, int CoreCount) tuple = NativeLibrary.GetSystemInfo(this.logger).Unchecked();
      return ((object) string.Format("CPU_{0}[{1}] {2}MB", (object) tuple.Architecture, (object) tuple.CoreCount, (object) tuple.TotalMemory), true);
    }

    private (object, bool) GetDeviceNameInformation()
    {
      (string, bool) valueTuple = DevicesSdk.IsInitialized ? (DevicesSdk.DeviceInformation.DeviceName, true) : ((string) null, false);
      return ((object) valueTuple.Item1, valueTuple.Item2);
    }

    private (object, bool) GetDeviceTraceIdInformation() => ((object) this.analyticsRepository.DeviceId, true);

    private (AppTelemetryDataKey Key, object Value) GetDataPoint(AppTelemetryDataKey key)
    {
      CachedTelemetryDataPoint telemetryDataPoint;
      return (key, this.dataCache.TryGetValue(key, out telemetryDataPoint) ? (object) telemetryDataPoint : (object) (CachedTelemetryDataPoint) null);
    }

    private IDictionary<string, string> GetDataPointsAsStrings(IEnumerable<AppTelemetryDataKey> keys) => this.GetDataPoints<string, string>(keys, new Func<object, string>(Convert.ToString), new Func<object, string>(Convert.ToString));

    private IDictionary<TKey, TValue> GetDataPoints<TKey, TValue>(
      IEnumerable<AppTelemetryDataKey> keys,
      Func<object, TKey> keyConverter,
      Func<object, TValue> valueConverter)
    {
      IDictionary<TKey, TValue> dataPoints = (IDictionary<TKey, TValue>) new Dictionary<TKey, TValue>();
      if (keys != null)
      {
        foreach (AppTelemetryDataKey key in keys)
        {
          CachedTelemetryDataPoint telemetryDataPoint;
          if (this.dataCache.TryGetValue(key, out telemetryDataPoint))
            dataPoints.Add(keyConverter((object) key), valueConverter(telemetryDataPoint.Value));
        }
      }
      return dataPoints;
    }

    private void AddDataPoint(
      AppTelemetryDataKey key,
      Func<(object, bool)> creator,
      TimeSpan? cacheLifetime = null)
    {
      this.dataCache.Add(key, new CachedTelemetryDataPoint(key, this.logger, creator, cacheLifetime));
    }
  }
}
