// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.DialogViewStateResult
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;

namespace Okta.Authenticator.NativeApp.Events
{
  public struct DialogViewStateResult
  {
    public DialogViewStateResult(DialogViewType viewType, bool result)
    {
      this.ViewType = viewType;
      this.DialogResult = result;
    }

    public DialogViewType ViewType { get; }

    public bool DialogResult { get; }
  }
}
