// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.JITOnboardingViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.OktaVerify.Windows.Core.Properties;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class JITOnboardingViewModel : OnboardingViewModel
  {
    public JITOnboardingViewModel(string signInUrl)
      : base(signInUrl)
    {
      this.SignInViewModel = new SignInViewModel();
      this.CurrentStep = OnboardingStep.JITWelcome;
      (this.MessageBeforeUrl, this.MessageAfterUrl) = ResourceExtensions.GetTwoPartsWithoutPlaceholder(Resources.AccountNotFoundAboutMessage);
    }

    public string MessageBeforeUrl { get; }

    public string MessageAfterUrl { get; }

    public SignInViewModel SignInViewModel { get; }

    public bool CanGoBack => false;

    protected override void AddAccount()
    {
      this.CurrentStep = OnboardingStep.AddAccount;
      this.SignInViewModel.SignInCommand.Execute((object) this.SignInUrl);
    }

    protected override void BackToWelcome() => this.CurrentStep = OnboardingStep.JITWelcome;
  }
}
