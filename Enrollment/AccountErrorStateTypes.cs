// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.AccountErrorStateTypes
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  [Flags]
  public enum AccountErrorStateTypes
  {
    None = 0,
    UserVerificationKeyDoesNotExist = 1,
    UserVerificationRequiredByServer = 2,
    AccountReenrollmentRequired = 4,
    AccountInvalidated = 16, // 0x00000010
  }
}
