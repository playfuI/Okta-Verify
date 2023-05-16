// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.RemoveAccountDialogViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.OktaVerify.Windows.Core.Properties;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class RemoveAccountDialogViewModel : BaseConfirmDialogViewModel
  {
    public RemoveAccountDialogViewModel(IOktaAccount account)
      : base(DialogViewType.RemoveAccountConfirmation)
    {
      this.ConfirmMainText = Resources.RemoveAccountConfirmationText;
      this.ConfirmLabel = Resources.RemoveAccountButtonLabel;
      this.CancelLabel = Resources.ButtonNotNow;
      this.UserInfo = account?.UserLogin;
    }
  }
}
