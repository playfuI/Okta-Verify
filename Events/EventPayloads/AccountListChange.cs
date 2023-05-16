// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.AccountListChange
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public struct AccountListChange
  {
    public AccountListChange(ListChangeType changeType, IOktaAccount account = null)
    {
      this.ChangeType = changeType;
      this.Account = account;
    }

    public ListChangeType ChangeType { get; }

    public IOktaAccount Account { get; }
  }
}
