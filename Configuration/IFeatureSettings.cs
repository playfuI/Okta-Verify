﻿// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Configuration.IFeatureSettings
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

namespace Okta.Authenticator.NativeApp.Configuration
{
  public interface IFeatureSettings
  {
    DeviceHealthOptions DeviceHealthUISetting { get; }

    bool IsFeatureEnabled(FeatureType feature);
  }
}
