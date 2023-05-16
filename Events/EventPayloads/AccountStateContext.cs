// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.AccountStateContext
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public class AccountStateContext
  {
    public AccountStateContext(
      string accountId,
      bool isUVKeyInvalidated,
      bool isPoPKeyInvalidated = false,
      AccountLifecycleEventType lifecycleChange = AccountLifecycleEventType.None)
    {
      this.AccountId = accountId;
      this.IsUVKeyInvalidated = isUVKeyInvalidated;
      this.IsPoPKeyInvalidated = isPoPKeyInvalidated;
      this.LifecycleChange = lifecycleChange;
    }

    public string AccountId { get; }

    public bool IsUVKeyInvalidated { get; }

    public bool IsPoPKeyInvalidated { get; }

    public AccountLifecycleEventType LifecycleChange { get; }
  }
}
