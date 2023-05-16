// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AccountVerifiedViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Bindings;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AccountVerifiedViewModel : BaseConfirmDialogViewModel
  {
    internal static int ConfirmationTimeout = 20000;

    public AccountVerifiedViewModel(BindingInformationModel bindingInformation)
      : base(DialogViewType.AccountVerified)
    {
      this.ConfirmMainText = Resources.EnrollVerifiedReturnToApp.CultureFormat((object) bindingInformation?.AppName);
      this.ConfirmLabel = Resources.ButtonDoneText;
      this.UserInfo = bindingInformation?.UserEmail;
      this.InitializeConfirmTask();
    }

    private void InitializeConfirmTask() => Task.Delay(AccountVerifiedViewModel.ConfirmationTimeout).ContinueWith(new Action<Task>(this.OnConfirmTimedOut));

    private void OnConfirmTimedOut(Task task) => this.OnCancelled();
  }
}
