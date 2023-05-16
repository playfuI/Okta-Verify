// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.IOidcFactory
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Oidc.Abstractions;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public interface IOidcFactory
  {
    IOktaClient GetOidcClient(string signInUrl);

    UserAgentHandler UserAgentHandler { get; }
  }
}
