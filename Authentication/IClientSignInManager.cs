// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.IClientSignInManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public interface IClientSignInManager
  {
    string DefaultSignInUrl { get; }

    ICredentialOptions CredentialOptions { get; }

    bool CanSignInWithWindowsHello { get; }

    bool CheckIfWindowsHelloSignInConfigured();

    Task<ISignInInformationModel> SignInAsync(
      string url,
      bool multiAccount,
      CancellationToken? cancellationToken = null);

    Task<ISignInInformationModel> SignInWithExistingAccountAsync(
      string url,
      string loginHint,
      string userId,
      bool multiAccount,
      CancellationToken? cancellationToken = null);
  }
}
