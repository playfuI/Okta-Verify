// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.DeviceNotSupportedViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Prism.Commands;
using System;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class DeviceNotSupportedViewModel : AsyncInlineOperationViewModel<bool>
  {
    public DeviceNotSupportedViewModel() => this.ConfirmCommand = (ICommand) new DelegateCommand(new Action(this.OnMessageConfirmed));

    public ICommand ConfirmCommand { get; }

    private void OnMessageConfirmed() => this.TrySetResult(true);
  }
}
