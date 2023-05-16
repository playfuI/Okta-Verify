// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.ConfigurationManagerExtensions
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authenticator;
using Okta.Devices.SDK.Credentials;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Win32;
using System;
using System.Collections.Generic;

namespace Okta.Authenticator.NativeApp.Configuration
{
  public static class ConfigurationManagerExtensions
  {
    private const char ConfigFlagsSeparator = ';';

    public static T TryGetMachineRegistryConfig<T>(
      this IConfigurationManager manager,
      ILogger logger,
      string valueName,
      T defaultValue)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetMachineRegistryConfig<T>(logger, valueName, defaultValue, (Func<object, T>) null);
    }

    public static T TryGetRegistryConfig<T>(
      this IConfigurationManager manager,
      ILogger logger,
      string valueName,
      T defaultValue)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetRegistryConfig<T>(logger, valueName, defaultValue, (Func<object, T>) null);
    }

    public static T TryGetMachineRegistryConfig<T>(
      this IConfigurationManager manager,
      ILogger logger,
      string valueName,
      T defaultValue,
      Func<object, T> converter)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetMachineRegistryConfig<T>(logger, (string) null, valueName, defaultValue, converter);
    }

    public static T TryGetRegistryConfig<T>(
      this IConfigurationManager manager,
      ILogger logger,
      string valueName,
      T defaultValue,
      Func<object, T> converter)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetRegistryConfig<T>(logger, (string) null, valueName, defaultValue, converter);
    }

    public static ILogger CreateLoggerWithConfiguredLogLevel(
      this IConfigurationManager manager,
      string sourceName,
      string logName,
      Okta.Devices.SDK.LogLevel defaultValue = Okta.Devices.SDK.LogLevel.Warning)
    {
      manager.EnsureNotNull(nameof (manager));
      using (EventViewerLogger eventViewerLogger = new EventViewerLogger(sourceName, logName, defaultValue))
      {
        Okta.Devices.SDK.LogLevel logLevel = manager.GetLogLevel((ILogger) eventViewerLogger, defaultValue);
        return (ILogger) new EventViewerLogger(sourceName, logName, logLevel);
      }
    }

    public static Okta.Devices.SDK.LogLevel GetLogLevel(
      this IConfigurationManager manager,
      ILogger logger,
      Okta.Devices.SDK.LogLevel defaultValue = Okta.Devices.SDK.LogLevel.Warning)
    {
      return manager.TryGetRegistryConfig<Okta.Devices.SDK.LogLevel>(logger, "LogLevel", defaultValue, new Func<object, Okta.Devices.SDK.LogLevel>(ConfigurationManagerExtensions.RegistryLogLevelConverter));
    }

    public static AuthenticatorKeyCreationFlags GetKeyCreationFlagsFromRegistry(
      this IConfigurationManager manager,
      ILogger logger)
    {
      return (AuthenticatorKeyCreationFlags) manager.TryGetMachineRegistryConfig<int>(logger, "KeyCreationFlags", 0);
    }

    public static JustInTimeEnrollmentType GetEnrollmentTypeFromRegistry(
      this IConfigurationManager manager,
      ILogger logger)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetRegistryConfig<JustInTimeEnrollmentType>(logger, "JustInTimeEnrollmentConfiguration", JustInTimeEnrollmentType.Default, (Func<object, JustInTimeEnrollmentType>) (o => ConfigurationManagerExtensions.RegistryEnumConverter<JustInTimeEnrollmentType>(o, JustInTimeEnrollmentType.Default)));
    }

    public static LoopbackBindingOperationMode GetLoopbackOperationMode(
      this IConfigurationManager manager,
      ILogger logger)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetMachineRegistryConfig<LoopbackBindingOperationMode>(logger, "LoopbackBindingMode", LoopbackBindingOperationMode.EnabledHttp, (Func<object, LoopbackBindingOperationMode>) (o => ConfigurationManagerExtensions.RegistryEnumConverter<LoopbackBindingOperationMode>(o, LoopbackBindingOperationMode.EnabledHttp)));
    }

    public static CallerBinaryValidationType GetCallerBinaryValidationType(
      this IConfigurationManager manager,
      ILogger logger)
    {
      manager.EnsureNotNull(nameof (manager));
      return manager.TryGetMachineRegistryConfig<CallerBinaryValidationType>(logger, "CallerBinaryValidationMode", CallerBinaryValidationType.WarnOnUnsigned, (Func<object, CallerBinaryValidationType>) (o => ConfigurationManagerExtensions.RegistryEnumConverter<CallerBinaryValidationType>(o, CallerBinaryValidationType.WarnOnUnsigned)));
    }

    public static Uri GetWebAddressFromRegistry(
      this IConfigurationManager manager,
      ILogger logger,
      string key)
    {
      manager.EnsureNotNull(nameof (manager));
      string validURL;
      Uri result;
      return !Okta.Authenticator.NativeApp.Extensions.NormalizeWebAddress(manager.TryGetRegistryConfig<string>(logger, key, (string) null), out validURL) || !Uri.TryCreate(validURL, UriKind.Absolute, out result) ? (Uri) null : result;
    }

    public static AnalyticsConfigurationType GetAnalyticsConfigurationFromRegistry(
      this IConfigurationManager manager,
      ILogger logger)
    {
      manager.EnsureNotNull(nameof (manager));
      bool result;
      if (!bool.TryParse(manager.TryGetMachineRegistryConfig<string>(logger, "ReportToAppCenter", (string) null), out result))
        return AnalyticsConfigurationType.Default;
      return !result ? AnalyticsConfigurationType.Disabled : AnalyticsConfigurationType.Enabled;
    }

    public static bool? GetAdminConfiguredBetaProgramSetting(
      this IConfigurationManager manager,
      ILogger logger)
    {
      manager.EnsureNotNull(nameof (manager));
      bool result;
      return !bool.TryParse(manager.TryGetMachineRegistryConfig<string>(logger, "EnrollInBetaProgram", (string) null), out result) ? new bool?() : new bool?(result);
    }

    public static bool ShouldLaunchDebugger(this IConfigurationManager manager, ILogger logger) => manager.IsUserKeySetToTrueInRegistry(logger, "ForceDebugger");

    public static bool IsSandboxDisabled(this IConfigurationManager manager, ILogger logger) => manager.IsMachineKeySetToTrueInRegistry(logger, "DisableSandbox");

    public static bool ShouldUseSystemBrowser(this IConfigurationManager manager, ILogger logger) => !manager.IsUserKeySetToTrueInRegistry(logger, "OIDCUseIntegratedBrowser");

    public static bool IsMachineKeySetToTrueInRegistry(
      this IConfigurationManager manager,
      ILogger logger,
      string key)
    {
      return manager.TryGetMachineRegistryConfig<int>(logger, key, 0) == 1;
    }

    public static bool IsUserKeySetToTrueInRegistry(
      this IConfigurationManager manager,
      ILogger logger,
      string key)
    {
      return manager.TryGetRegistryConfig<int>(logger, key, 0) == 1;
    }

    public static HashSet<TFlagEnum> GetConfigFlags<TFlagEnum>(
      this IConfigurationManager manager,
      ILogger logger)
      where TFlagEnum : struct
    {
      HashSet<TFlagEnum> configFlags = new HashSet<TFlagEnum>();
      string machineRegistryConfig = manager.TryGetMachineRegistryConfig<string>(logger, "FeatureFlags", string.Empty);
      char[] chArray = new char[1]{ ';' };
      foreach (string str in machineRegistryConfig.Split(chArray))
      {
        TFlagEnum result;
        if (Enum.TryParse<TFlagEnum>(str, out result) && !configFlags.Contains(result))
          configFlags.Add(result);
      }
      return configFlags;
    }

    public static DeviceHealthOptions GetDeviceHealthOptions(
      this IConfigurationManager manager,
      ILogger logger)
    {
      return manager.TryGetMachineRegistryConfig<DeviceHealthOptions>(logger, "DeviceHealthOptions", DeviceHealthOptions.Enabled, (Func<object, DeviceHealthOptions>) (o => ConfigurationManagerExtensions.RegistryEnumConverter<DeviceHealthOptions>(o, DeviceHealthOptions.Enabled)));
    }

    private static T RegistryEnumConverter<T>(object value, T defaultValue) where T : struct
    {
      if (value is string s)
      {
        T result1;
        if (Enum.TryParse<T>(s, true, out result1) && Enum.IsDefined(typeof (T), (object) result1))
          return result1;
        int result2;
        if (int.TryParse(s, out result2) && Enum.IsDefined(typeof (T), (object) result2))
          return (T) (ValueType) result2;
      }
      return value is int num && Enum.IsDefined(typeof (T), (object) num) ? (T) value : defaultValue;
    }

    private static Okta.Devices.SDK.LogLevel RegistryLogLevelConverter(object value)
    {
      switch (value)
      {
        case string s:
          Okta.Devices.SDK.LogLevel result1;
          if (Enum.TryParse<Okta.Devices.SDK.LogLevel>(s, true, out result1) && Enum.IsDefined(typeof (Okta.Devices.SDK.LogLevel), (object) result1))
            return result1;
          int result2;
          if (int.TryParse(s, out result2))
            return ConfigurationManagerExtensions.FindClosestLogLevel(result2);
          break;
        case int num:
          return ConfigurationManagerExtensions.FindClosestLogLevel(num);
      }
      return Okta.Devices.SDK.LogLevel.None;
    }

    private static Okta.Devices.SDK.LogLevel FindClosestLogLevel(int value)
    {
      for (; value > 0; --value)
      {
        if (Enum.IsDefined(typeof (Okta.Devices.SDK.LogLevel), (object) value))
          return (Okta.Devices.SDK.LogLevel) value;
      }
      return Okta.Devices.SDK.LogLevel.None;
    }
  }
}
