// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInfo.IApplicationInfoManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK.Authenticator.Integrations;

namespace Okta.Authenticator.NativeApp.ApplicationInfo
{
  public interface IApplicationInfoManager : IAuthenticatorPluginConfiguration
  {
    bool CheckIfInRemoteSession();
  }
}
