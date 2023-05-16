// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.MachineJoinStatusFlags
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;

namespace Okta.Authenticator.NativeApp
{
  [Flags]
  public enum MachineJoinStatusFlags
  {
    None = 0,
    NotDomainJoined = 1,
    NotAzureJoined = 2,
    DomainJoined = 4,
    AzureActiveDirectoryJoined = 8,
    WorkplaceJoined = 16, // 0x00000010
  }
}
