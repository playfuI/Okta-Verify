// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.IWindowsMessageSender
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public interface IWindowsMessageSender
  {
    int SignalInstanceAsync(SingletonSignals signals);

    int SignalInstance(SingletonSignals signals);

    int SendDataToInstance(byte[] data, SingletonSignals messageType);
  }
}
