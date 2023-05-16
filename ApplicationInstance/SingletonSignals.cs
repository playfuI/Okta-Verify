// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.SingletonSignals
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  [Flags]
  public enum SingletonSignals
  {
    None = 0,
    Activate = 1,
    MoveToSystemTray = 2,
    URIActivate = 4,
    CheckBindings = 8,
    OpenReportIssueWindow = 16, // 0x00000010
    OpenSettingsWindow = 32, // 0x00000020
    OpenAboutWindow = 64, // 0x00000040
    ActivationRequestCallback = 128, // 0x00000080
    Shutdown = 65535, // 0x0000FFFF
  }
}
