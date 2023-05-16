// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.DisableVerificationDialogViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.OktaVerify.Windows.Core.Properties;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class DisableVerificationDialogViewModel : BaseConfirmDialogViewModel
  {
    public DisableVerificationDialogViewModel(IOktaAccount account)
      : base(DialogViewType.DisableVerificationConfirmation)
    {
      IClientSignInManager clientSignInManager = AppInjector.Get<IClientSignInManager>();
      this.ConfirmMainText = clientSignInManager != null && clientSignInManager.CanSignInWithWindowsHello ? Resources.UserVerificationDisableConfirmationWindowsHello : Resources.UserVerificationDisableConfirmationPasscode;
      this.ConfirmLabel = Resources.ButtonDisable;
      this.CancelLabel = Resources.ButtonNotNow;
      this.UserInfo = account?.UserLogin;
    }
  }
}
