// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AccountAddedViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.OktaVerify.Windows.Core.Properties;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AccountAddedViewModel : BaseViewModel
  {
    private readonly IOktaAccount account;
    private readonly IClientAccountManager accountManager;

    public AccountAddedViewModel(IOktaAccount account)
    {
      this.account = account;
      this.accountManager = AppInjector.Get<IClientAccountManager>();
    }

    public string CurrentEnrollEmail => this.account.UserLogin;

    public bool IsWinHelloEnabled => this.account.IsUserVerificationEnabled;

    public bool IsDefaultEnabled => this.accountManager.IsAccountSetAsDefault(this.account.AccountId);

    public string WinHelloEnabledLabel => ResourceExtensions.ExtractCompositeResource(Resources.EnrollWindowsHelloEnabled);

    public string DefaultEnabledLabel => ResourceExtensions.ExtractCompositeResource(Resources.EnrollDefaultAccountSet);
  }
}
