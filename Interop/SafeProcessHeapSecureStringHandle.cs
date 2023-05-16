// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.SafeProcessHeapSecureStringHandle
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Security;

namespace Okta.Authenticator.NativeApp.Interop
{
  internal class SafeProcessHeapSecureStringHandle : SafeProcessHeapHandle
  {
    public unsafe SecureString ToSecureString(int size) => new SecureString((char*) this.handle.ToPointer(), size);
  }
}
