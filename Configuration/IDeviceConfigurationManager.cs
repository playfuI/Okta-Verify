// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.IDeviceConfigurationManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Configuration
{
  public interface IDeviceConfigurationManager
  {
    string DeviceName { get; }

    string WindowsUpdateConfigurationLink { get; }

    string EncrytionConfigurationLink { get; }

    string SignInOptionsConfigurationLink { get; }

    string WindowsHelloConfigurationLink { get; }

    bool IsDeviceHealthCheckEnabled { get; }

    bool IsPhishingResistanceMessageEnabled { get; }

    bool IsOSUpdateEnabled { get; }

    Version GetOSVersion();

    Task<Version> GetLatestVersion();

    bool? IsDiskEncrypted();
  }
}
