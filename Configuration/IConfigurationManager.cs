// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.IConfigurationManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Devices.SDK;
using System;

namespace Okta.Authenticator.NativeApp.Configuration
{
  public interface IConfigurationManager
  {
    string ApplicationDataPath { get; }

    string ApplicationDataFolder { get; }

    string UserPluginManifestFileDirectory { get; }

    string GlobalPluginManifestFileDirectory { get; }

    string ApplicationStoreFileNamePrefix { get; }

    string LogDirectory { get; }

    string VendorRegistryRoot { get; }

    string ApplicationRegistryKey { get; }

    string OktaDeviceAccessRegistryKey { get; }

    string SandboxPrefix { get; }

    string EventLogSource { get; }

    T TryGetMachineRegistryConfig<T>(
      ILogger logger,
      string subNode,
      string valueName,
      T defaultValue,
      Func<object, T> converter);

    T TryGetRegistryConfig<T>(
      ILogger logger,
      string subNode,
      string valueName,
      T defaultValue,
      Func<object, T> converter);

    void EnsureRegistryRootExists(ILogger logger);

    void EnsureApplicationDataFolderExists(ILogger logger);

    string GenerateSandboxName(ILogger logger, string userSid, bool appendRandomization = true);

    bool IsSandboxDisabled(ILogger logger);

    string GetSandboxDirectory(ILogger logger);

    bool IsOktaDeviceAccessEnrolled();

    bool RegistryKeyExists(ILogger logger, RegistryKey registryKey, string keyPath);
  }
}
