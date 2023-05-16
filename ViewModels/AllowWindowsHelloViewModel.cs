// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AllowWindowsHelloViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AllowWindowsHelloViewModel : AsyncInlineOperationViewModel<bool>
  {
    public AllowWindowsHelloViewModel(
      string enrollEmail,
      bool windowsHelloSupported,
      bool enforceUserVerification)
    {
      this.CurrentEnrollEmail = enrollEmail;
      this.EnforceUserVerification = enforceUserVerification;
      this.ConfirmEnrollWithBiometricsCommand = (ICommand) new DelegateCommand<string>((Action<string>) (p => this.TrySetResult(bool.Parse(p))));
      this.UpdateExtraVerificationText(windowsHelloSupported);
    }

    public string CurrentEnrollEmail { get; }

    public bool EnforceUserVerification { get; }

    public ICommand ConfirmEnrollWithBiometricsCommand { get; }

    public string EnableExtraVerificationTitle { get; private set; }

    public string ExtraVerificationAddedSecurityText { get; private set; }

    public string ExtraVerificationRequiredText { get; private set; }

    private void UpdateExtraVerificationText(bool windowsHelloSupported)
    {
      this.EnableExtraVerificationTitle = windowsHelloSupported ? Resources.ExtraVerificationWindowsHelloTitle : Resources.ExtraVerificationPasscodeTitle;
      this.ExtraVerificationAddedSecurityText = windowsHelloSupported ? Resources.WindowsHelloExtraVerificationAddedSecurityWindowsHello : Resources.WindowsHelloExtraVerificationAddedSecurityPasscode;
      this.ExtraVerificationRequiredText = windowsHelloSupported ? Resources.ExtraVerificationOrgRequiresWindowsHello : Resources.ExtraVerificationOrgRequiresPasscode;
      this.FirePropertyChangedEvent("EnableExtraVerificationTitle");
      this.FirePropertyChangedEvent("ExtraVerificationAddedSecurityText");
      this.FirePropertyChangedEvent("ExtraVerificationRequiredText");
    }
  }
}
