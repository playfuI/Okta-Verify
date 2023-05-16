// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AccountNotFoundViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.OktaVerify.Windows.Core.Properties;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AccountNotFoundViewModel : SignInViewModel
  {
    public AccountNotFoundViewModel(string signInUrl, string appName)
      : base()
    {
      this.SignInUrl = signInUrl;
      this.CurrentAppName = appName;
      (this.MessageBeforeUrl, this.MessageAfterUrl) = ResourceExtensions.GetTwoPartsWithoutPlaceholder(Resources.AccountNotFoundAboutMessage);
    }

    public string MessageBeforeUrl { get; }

    public string MessageAfterUrl { get; }

    public string SignInUrl { get; }

    public string CurrentAppName { get; }

    public override bool CanGoBack => false;
  }
}
