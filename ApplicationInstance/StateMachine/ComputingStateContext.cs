// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine.ComputingStateContext
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

namespace Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine
{
  public class ComputingStateContext
  {
    public ComputingStateContext()
      : this(StartupArgumentType.Unknown, (string[]) null)
    {
    }

    public ComputingStateContext(StartupArgumentType command, string[] arguments)
    {
      this.Command = command;
      this.Arguments = arguments;
    }

    public StartupArgumentType Command { get; private set; }

    public string[] Arguments { get; private set; }
  }
}
