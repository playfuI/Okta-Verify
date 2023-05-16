// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.ViewStateRequest
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;

namespace Okta.Authenticator.NativeApp.Events
{
  public struct ViewStateRequest
  {
    private readonly object context;

    public ViewStateRequest(MainViewType viewType, object context = null)
    {
      this.ViewType = viewType;
      this.context = context;
    }

    public MainViewType ViewType { get; }

    public TModel GetContext<TModel>() where TModel : class => this.context as TModel;
  }
}
