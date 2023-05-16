// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.DialogViewState`1
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public class DialogViewState<TPayload> : IDialogViewState where TPayload : class
  {
    public DialogViewState(DialogViewType viewType, bool showDialog = true, TPayload payload = null)
    {
      this.ViewType = viewType;
      this.Payload = payload;
      this.ShowDialog = showDialog;
    }

    public DialogViewType ViewType { get; }

    public TPayload Payload { get; }

    object IDialogViewState.Payload => (object) this.Payload;

    public bool ShowDialog { get; }
  }
}
