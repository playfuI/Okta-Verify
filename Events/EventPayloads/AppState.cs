// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.AppState
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public class AppState
  {
    public AppState(ComputingStateType stateType)
      : this(stateType, StartupArgumentType.Unknown)
    {
    }

    public AppState(ComputingStateType stateType, StartupArgumentType argumentType)
    {
      this.StateType = stateType;
      this.StateArgument = argumentType;
    }

    public ComputingStateType StateType { get; }

    public StartupArgumentType StateArgument { get; }
  }
}
