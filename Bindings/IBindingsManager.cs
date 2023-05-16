// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Bindings.IBindingsManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK.Bindings;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Bindings
{
  public interface IBindingsManager
  {
    bool StartBinding<TBinding>() where TBinding : AuthenticatorBinding;

    bool StopBinding<TBinding>() where TBinding : AuthenticatorBinding;

    Task ProcessUriActivationAsync(Uri uri);

    Task ProcessUriSignalActivationAsync(string messageInfo);

    void ProcessBindingRefresh();
  }
}
