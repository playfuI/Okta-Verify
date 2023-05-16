// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.OktaAccountExtensions
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK.Extensions.User;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public static class OktaAccountExtensions
  {
    public static string TryGetShortName(this IOktaAccount account)
    {
      string shortName;
      return !UserExtensions.TryGetShortName(account?.UserLogin ?? (string) null, out shortName) ? (string) null : shortName;
    }

    public static bool IsShortNameMatch(this IOktaAccount account, string shortName) => UserExtensions.IsShortNameMatch(account?.UserLogin ?? (string) null, shortName);
  }
}
