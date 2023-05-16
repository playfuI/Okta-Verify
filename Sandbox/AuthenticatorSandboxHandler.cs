// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Sandbox.AuthenticatorSandboxHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Authenticator.NativeApp.Interop;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Okta.Authenticator.NativeApp.Sandbox
{
  internal static class AuthenticatorSandboxHandler
  {
    private const string RegistryPathProfileKey = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList";
    private const string RegistryNameProfilePath = "ProfileImagePath";

    public static bool RemoveSandboxAccountsBasedOnDirectory(
      ILogger logger,
      string directory,
      string validAccountToSkip = null)
    {
      if (!Directory.Exists(directory))
        return true;
      bool flag = true;
      foreach (string directory1 in Directory.GetDirectories(directory))
        flag = AuthenticatorSandboxHandler.RemoveSandboxAccount(logger, new DirectoryInfo(directory1).Name, validAccountToSkip) & flag;
      return flag;
    }

    public static bool RemoveOrphanedSandboxAccountsMatchingPrefix(
      ILogger logger,
      string sandboxPrefix,
      string validAccountToSkip = null)
    {
      bool flag = true;
      foreach ((string str, string _) in AuthenticatorSandboxHandler.GetAllUserAccounts(logger))
      {
        if (str.StartsWith(sandboxPrefix, StringComparison.OrdinalIgnoreCase))
          flag = AuthenticatorSandboxHandler.RemoveSandboxAccount(logger, str, validAccountToSkip) & flag;
      }
      return flag;
    }

    public static bool RemoveOrphanedSandboxAccountsThroughRegistry(
      ILogger logger,
      string directory,
      string validAccountToSkip = null)
    {
      RegistryKey registryKey1 = (RegistryKey) null;
      bool flag = true;
      try
      {
        registryKey1 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList", false);
        foreach (string subKeyName in registryKey1.GetSubKeyNames())
        {
          using (RegistryKey registryKey2 = registryKey1.OpenSubKey(subKeyName, false))
          {
            if (registryKey2.GetValue("ProfileImagePath", (object) null) is string path)
            {
              if (!string.IsNullOrEmpty(path))
              {
                if (path.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
                  flag = AuthenticatorSandboxHandler.RemoveSandboxAccount(logger, new DirectoryInfo(path).Name, validAccountToSkip) & flag;
              }
            }
          }
        }
      }
      finally
      {
        registryKey1?.Dispose();
      }
      return flag;
    }

    public static IEnumerable<(string Name, string Sid)> GetAllUserAccounts(ILogger logger)
    {
      try
      {
        return NativeLibrary.GetUsersInfo(logger).CheckedWithWarning();
      }
      catch (Exception ex) when (!ex.IsCritical(logger))
      {
        logger.WriteException("Failed to get all user accounts.", ex);
        return Enumerable.Empty<(string, string)>();
      }
    }

    private static bool RemoveSandboxAccount(
      ILogger logger,
      string accountName,
      string validAccountToSkip)
    {
      if (accountName.Equals(validAccountToSkip, StringComparison.OrdinalIgnoreCase))
      {
        logger.WriteInfoEx("Skipping removal of active service account " + accountName + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxHandler.cs", nameof (RemoveSandboxAccount));
        return true;
      }
      bool flag = AuthenticatorSandbox.RemoveSandboxAccount(accountName);
      logger.WriteInfoEx(string.Format("Service account {0} removed: {1}", (object) accountName, (object) flag), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Sandbox\\AuthenticatorSandboxHandler.cs", nameof (RemoveSandboxAccount));
      return flag;
    }
  }
}
