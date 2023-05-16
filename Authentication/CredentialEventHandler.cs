// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.CredentialEventHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class CredentialEventHandler : ICredentialEventHandler
  {
    public Func<CredentialEventType, CredentialEventPayload, Task<CredentialUsageContext>> OnUserInteractionRequested { get; internal set; }

    public Action<CredentialEventType, CredentialEventPayload> OnCredentialEventUpdate { get; internal set; }

    public Func<CredentialEventType, AuthenticationRequiredEventPayload, Task<(IDeviceToken, bool)>> OnAuthenticationRequired { get; internal set; }
  }
}
