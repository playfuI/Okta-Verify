// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.OktaVerifyConfigurationManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using System;
using System.IO;
using System.Text;

namespace Okta.Authenticator.NativeApp.Configuration
{
  internal class OktaVerifyConfigurationManager : IConfigurationManager
  {
    private const string RegistryRenderModeKey = "SoftwareRender";
    private readonly RegistryKey userRoot;

    public OktaVerifyConfigurationManager()
      : this("Okta\\OktaVerify", Registry.CurrentUser, "Software\\Okta", "Okta Verify", "Okta Device Access", "Okta Verify", OktaVerifyConfigurationManager.GetNameBasedOnBuildType("OVSvc"), OktaVerifyConfigurationManager.GetNameBasedOnBuildType("OVStore"), "Plugins")
    {
    }

    protected OktaVerifyConfigurationManager(
      string applicationStorageFolder,
      RegistryKey userRootKey,
      string vendorRegistryRoot,
      string registryKeyAppName,
      string registryKeyOktaDeviceAccess,
      string loggingSource,
      string sandboxPrefix,
      string dbFileNamePrefix,
      string pluginsFolder)
    {
      this.userRoot = userRootKey;
      this.ApplicationDataFolder = applicationStorageFolder;
      this.ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), applicationStorageFolder);
      this.ApplicationStoreFileNamePrefix = dbFileNamePrefix;
      this.LogDirectory = this.BuildUserFilePath("Logs");
      this.VendorRegistryRoot = vendorRegistryRoot;
      this.ApplicationRegistryKey = registryKeyAppName;
      this.OktaDeviceAccessRegistryKey = registryKeyAppName;
      this.ApplicationRegistryRoot = Path.Combine(vendorRegistryRoot, registryKeyAppName);
      this.OktaDeviceAccessRegistryRoot = Path.Combine(vendorRegistryRoot, registryKeyOktaDeviceAccess);
      this.EventLogSource = loggingSource;
      this.SandboxPrefix = sandboxPrefix;
      this.UserPluginManifestFileDirectory = this.BuildUserFilePath(pluginsFolder);
      this.GlobalPluginManifestFileDirectory = this.BuildGlobalFilePath(pluginsFolder);
    }

    public string ApplicationDataPath { get; }

    public string ApplicationDataFolder { get; }

    public string LogDirectory { get; }

    public string VendorRegistryRoot { get; }

    public string ApplicationRegistryKey { get; }

    public string OktaDeviceAccessRegistryKey { get; }

    public string ApplicationStoreFileNamePrefix { get; }

    public string SandboxPrefix { get; }

    public string EventLogSource { get; }

    public string UserPluginManifestFileDirectory { get; }

    public string GlobalPluginManifestFileDirectory { get; }

    internal string ApplicationRegistryRoot { get; }

    internal string OktaDeviceAccessRegistryRoot { get; }

    public bool IsSandboxDisabled(ILogger logger) => this.IsUserKeySetToTrueInRegistry(logger, "DisableSandbox");

    public static OktaVerifyConfigurationManager GetCustomUserConfiguration(
      IConfigurationManager configurationManager,
      RegistryKey userKey,
      string appDataFolder)
    {
      return new OktaVerifyConfigurationManager(appDataFolder, userKey, configurationManager.VendorRegistryRoot, configurationManager.ApplicationRegistryKey, configurationManager.OktaDeviceAccessRegistryKey, configurationManager.EventLogSource, configurationManager.SandboxPrefix, configurationManager.ApplicationStoreFileNamePrefix, Path.GetFileName(configurationManager.GlobalPluginManifestFileDirectory));
    }

    public static bool UseSoftwareRender()
    {
      try
      {
        string path = Path.Combine("Software\\Okta", "Okta Verify");
        return !string.IsNullOrWhiteSpace(Registry.CurrentUser.GetRegistryValue(path, "SoftwareRender")?.ToString());
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        return false;
      }
    }

    public string GenerateSandboxName(ILogger logger, string userSid, bool appendRandomization = true)
    {
      StringBuilder stringBuilder = new StringBuilder(this.SandboxPrefix);
      string str = userSid.Substring(userSid.LastIndexOf('-'));
      stringBuilder.Append(stringBuilder.Length + str.Length > 15 ? str.Substring(0, 15 - stringBuilder.Length) : str);
      stringBuilder.Append('-');
      if (appendRandomization)
        stringBuilder.Append(DateTime.Now.ToString("ffff"));
      logger.WriteInfoEx("Generate Sandbox Name : " + stringBuilder.ToString(), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\OktaVerifyConfigurationManager.cs", nameof (GenerateSandboxName));
      return stringBuilder.ToString();
    }

    public virtual T TryGetMachineRegistryConfig<T>(
      ILogger logger,
      string subNode,
      string valueName,
      T defaultValue,
      Func<object, T> converter)
    {
      logger.EnsureNotNull(nameof (logger));
      string path = subNode == null ? this.ApplicationRegistryRoot : this.ApplicationRegistryRoot + "\\" + subNode;
      T obj;
      return OktaVerifyConfigurationManager.TryGetRegistryValue<T>(logger, Registry.LocalMachine, path, valueName, converter, out obj) ? obj : defaultValue;
    }

    public virtual T TryGetRegistryConfig<T>(
      ILogger logger,
      string subNode,
      string valueName,
      T defaultValue,
      Func<object, T> converter)
    {
      logger.EnsureNotNull(nameof (logger));
      string path = subNode == null ? this.ApplicationRegistryRoot : this.ApplicationRegistryRoot + "\\" + subNode;
      T registryConfig;
      if (OktaVerifyConfigurationManager.TryGetRegistryValue<T>(logger, this.userRoot, path, valueName, converter, out registryConfig))
        return registryConfig;
      T obj;
      return OktaVerifyConfigurationManager.TryGetRegistryValue<T>(logger, Registry.LocalMachine, path, valueName, converter, out obj) ? obj : defaultValue;
    }

    public virtual void EnsureRegistryRootExists(ILogger logger)
    {
      logger.EnsureNotNull(nameof (logger));
      RegistryKey disposable = (RegistryKey) null;
      try
      {
        disposable = this.userRoot.OpenSubKey(this.ApplicationRegistryRoot);
        if (disposable != null)
          return;
        logger.WriteInfoEx("Creating registry key HKCU\\" + this.ApplicationRegistryRoot, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\OktaVerifyConfigurationManager.cs", nameof (EnsureRegistryRootExists));
        disposable = this.userRoot.CreateSubKey(this.ApplicationRegistryRoot);
      }
      finally
      {
        disposable.SafeDispose();
      }
    }

    public virtual bool RegistryKeyExists(ILogger logger, RegistryKey registryKey, string keyPath)
    {
      logger.EnsureNotNull(nameof (logger));
      RegistryKey disposable = (RegistryKey) null;
      try
      {
        disposable = registryKey.OpenSubKey(keyPath);
        if (disposable != null)
          return true;
        logger.WriteInfoEx("Registry key does not exist: " + keyPath, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\OktaVerifyConfigurationManager.cs", nameof (RegistryKeyExists));
        return false;
      }
      finally
      {
        disposable.SafeDispose();
      }
    }

    public bool IsOktaDeviceAccessEnrolled()
    {
      try
      {
        string str1 = Registry.LocalMachine.GetRegistryValue(this.OktaDeviceAccessRegistryRoot, "ClientId")?.ToString();
        string str2 = Registry.LocalMachine.GetRegistryValue(this.OktaDeviceAccessRegistryRoot, "ClientSecret")?.ToString();
        string str3 = Registry.LocalMachine.GetRegistryValue(this.OktaDeviceAccessRegistryRoot, "OrgUrl")?.ToString();
        return !string.IsNullOrWhiteSpace(str1) && !string.IsNullOrWhiteSpace(str2) && !string.IsNullOrWhiteSpace(str3);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        return false;
      }
    }

    public virtual void EnsureApplicationDataFolderExists(ILogger logger)
    {
      logger.EnsureNotNull(nameof (logger));
      if (Directory.Exists(this.ApplicationDataPath))
        return;
      logger.WriteInfoEx("Creating app data folder...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\OktaVerifyConfigurationManager.cs", nameof (EnsureApplicationDataFolderExists));
      Directory.CreateDirectory(this.ApplicationDataPath);
    }

    public string GetSandboxDirectory(ILogger logger)
    {
      string machineRegistryConfig = this.TryGetMachineRegistryConfig<string>(logger, "SandboxLocationOverride", (string) null);
      return string.IsNullOrEmpty(machineRegistryConfig) ? this.BuildGlobalFilePath("Sandbox", Environment.UserName) : Path.Combine(machineRegistryConfig, "Sandbox", Environment.UserName);
    }

    private static bool TryGetRegistryValue<T>(
      ILogger logger,
      RegistryKey rootKey,
      string path,
      string valueName,
      Func<object, T> converter,
      out T value)
    {
      try
      {
        object registryValue = rootKey.GetRegistryValue(path, valueName);
        if (registryValue != null)
        {
          value = converter == null ? (T) registryValue : converter(registryValue);
          logger.WriteInfoEx(string.Format("Read registry key {0}\\{1}\\{2} with type {3} and value {4}", (object) rootKey, (object) path, (object) valueName, (object) typeof (T).Name, (object) value), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\OktaVerifyConfigurationManager.cs", nameof (TryGetRegistryValue));
          return true;
        }
      }
      catch (Exception ex) when (!ex.IsCritical(logger))
      {
        logger.WriteException(string.Format("Failed to get registry key {0}\\{1}\\{2} with type {3}", (object) rootKey, (object) path, (object) valueName, (object) typeof (T).Name), ex);
      }
      logger.WriteInfoEx(string.Format("Failed to read registry key {0}\\{1}\\{2} with type {3}, returning default value {4}", (object) rootKey, (object) path, (object) valueName, (object) typeof (T).Name, null), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Configuration\\OktaVerifyConfigurationManager.cs", nameof (TryGetRegistryValue));
      value = default (T);
      return false;
    }

    private static string GetNameBasedOnBuildType(string value) => !Okta.Authenticator.NativeApp.Extensions.IsTestBuild ? value : value + "_DBG";

    private string BuildUserFilePath(string pathSuffix) => Path.Combine(this.ApplicationDataPath, pathSuffix);

    private string BuildGlobalFilePath(string pathSuffix1, string pathSuffix2 = null)
    {
      string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
      return pathSuffix2 != null ? Path.Combine(folderPath, this.ApplicationDataFolder, pathSuffix1, pathSuffix2) : Path.Combine(folderPath, this.ApplicationDataFolder, pathSuffix1);
    }
  }
}
