// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.BaseConfirmDialogViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public abstract class BaseConfirmDialogViewModel : BaseViewModel
  {
    protected BaseConfirmDialogViewModel(DialogViewType dialogType)
    {
      this.DialogType = dialogType;
      this.CancelCommand = (ICommand) new DelegateCommand(new Action(this.OnCancelled));
      this.ConfirmCommand = (ICommand) new DelegateCommand(new Action(this.OnConfirmed));
      this.ConfirmTaskSource = new TaskCompletionSource<bool>();
    }

    public DialogViewType DialogType { get; }

    public string ConfirmMainText { get; protected set; }

    public string ConfirmLabel { get; protected set; }

    public string CancelLabel { get; protected set; } = Resources.ButtonCancel;

    public string UserInfo { get; protected set; }

    public bool ShowUserInfo => !string.IsNullOrEmpty(this.UserInfo);

    public ICommand ConfirmCommand { get; }

    public ICommand CancelCommand { get; }

    public Task<bool> ConfirmTask => this.ConfirmTaskSource.Task;

    protected TaskCompletionSource<bool> ConfirmTaskSource { get; }

    protected void OnCancelled() => this.ConfirmTaskSource.TrySetResult(false);

    private void OnConfirmed() => this.ConfirmTaskSource.TrySetResult(true);
  }
}
