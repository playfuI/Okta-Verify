// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.RegistryExtensions
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace Okta.Authenticator.NativeApp.Configuration
{
  public static class RegistryExtensions
  {
    public static IEnumerable<(string Name, object Value)> GetRegistryValues(
      this RegistryKey rootKey,
      string path,
      IEnumerable<string> valueNames)
    {
      RegistryKey registryKey = (RegistryKey) null;
      List<(string, object)> registryValues = new List<(string, object)>();
      if (valueNames != null)
      {
        if (valueNames.Count<string>() != 0)
        {
          try
          {
            registryKey = rootKey?.OpenSubKey(path, false);
            if (registryKey == null)
              return (IEnumerable<(string, object)>) registryValues;
            string[] valueNames1 = registryKey.GetValueNames();
            foreach (string valueName in valueNames)
            {
              if (((IEnumerable<string>) valueNames1).Contains<string>(valueName))
                registryValues.Add((valueName, registryKey.GetValue(valueName)));
            }
            return (IEnumerable<(string, object)>) registryValues;
          }
          finally
          {
            registryKey?.Dispose();
          }
        }
      }
      return (IEnumerable<(string, object)>) registryValues;
    }

    public static object GetRegistryValue(this RegistryKey rootKey, string path, string valueName)
    {
      RegistryKey registryKey = (RegistryKey) null;
      object registryValue = (object) null;
      try
      {
        registryKey = rootKey?.OpenSubKey(path, false);
        if (registryKey != null)
        {
          if (((IEnumerable<string>) registryKey.GetValueNames()).Contains<string>(valueName))
            registryValue = registryKey.GetValue(valueName);
        }
      }
      finally
      {
        registryKey?.Dispose();
      }
      return registryValue;
    }
  }
}
