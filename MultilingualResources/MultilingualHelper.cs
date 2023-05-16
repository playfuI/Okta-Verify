// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.MultilingualResources.MultilingualHelper
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Authenticator.NativeApp.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.MultilingualResources
{
  internal static class MultilingualHelper
  {
    private const string RegistryLocaleKey = "CustomLocale";

    public static bool SetCulture(string language) => MultilingualHelper.SetCulture(CultureInfo.GetCultureInfo(language));

    public static bool SetCulture(CultureInfo culture)
    {
      if (Okta.Authenticator.NativeApp.Interop.NativeMethods.SetThreadUILanguage((ushort) culture.LCID) == (ushort) 0)
        return false;
      Thread.CurrentThread.CurrentUICulture = culture;
      CultureInfo.DefaultThreadCurrentUICulture = culture;
      XmlLanguage language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
      FrameworkElement.LanguageProperty.OverrideMetadata(typeof (FrameworkElement), (PropertyMetadata) new FrameworkPropertyMetadata((object) language));
      return true;
    }

    public static bool CheckIfCustomLocaleRequested()
    {
      try
      {
        string localeValue;
        if (MultilingualHelper.TryGetRegistryLocaleSetting(out localeValue))
          return MultilingualHelper.SetCulture(localeValue);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        return false;
      }
      return false;
    }

    private static bool TryGetRegistryLocaleSetting(out string localeValue)
    {
      string path = Path.Combine("Software\\Okta", "Okta Verify");
      object registryValue = Registry.CurrentUser.GetRegistryValue(path, "CustomLocale");
      localeValue = registryValue?.ToString();
      return !string.IsNullOrWhiteSpace(localeValue);
    }
  }
}
