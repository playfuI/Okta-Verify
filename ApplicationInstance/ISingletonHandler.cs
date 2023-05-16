// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.ISingletonHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public interface ISingletonHandler
  {
    bool IsPrimaryInstance { get; }

    bool SignalMainInstance(SingletonSignals signals);

    bool SendDataToMainInstance(byte[] data, SingletonSignals messageType);

    Task<bool> PromoteInstance();

    bool DemoteOrSignalShutdown();
  }
}
