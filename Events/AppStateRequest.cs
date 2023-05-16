// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.AppStateRequest
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;

namespace Okta.Authenticator.NativeApp.Events
{
  public struct AppStateRequest
  {
    public AppStateRequest(
      ApplicationStateType newState,
      bool forceActivation = false,
      TemporaryApplicationStateType temporaryStateType = TemporaryApplicationStateType.None)
    {
      this.State = newState;
      this.ForceActivation = forceActivation;
      this.TemporaryStateType = temporaryStateType;
    }

    public ApplicationStateType State { get; }

    public bool ForceActivation { get; }

    public TemporaryApplicationStateType TemporaryStateType { get; }
  }
}
