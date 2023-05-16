// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.IAuthenticationRequestManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using Okta.Devices.SDK.Authentication;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public interface IAuthenticationRequestManager
  {
    AuthenticationRequestContext this[string requestId] { get; }

    AuthenticationRequestContext StartOrUpdateAuthentication(BindingEventPayload payload);

    Task<AuthenticationRequestContext> StartOrUpdateAuthenticationWithUserInteraction(
      BindingEventPayload payload);

    AuthenticationRequestContext StartOrUpdateAuthentication(CredentialEventPayload payload);

    Task<AuthenticationRequestContext> StartOrUpdateAuthenticationWithUserInteraction(
      CredentialEventPayload payload);

    AuthenticationRequestContext TryUpdateAuthentication(CredentialEventPayload payload);

    AuthenticationRequestContext EndAuthentication(BindingEventPayload payload, bool isSuccess);
  }
}
