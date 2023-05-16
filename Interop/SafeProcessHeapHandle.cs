// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.SafeProcessHeapHandle
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;

namespace Okta.Authenticator.NativeApp.Interop
{
  internal class SafeProcessHeapHandle : SafeHandleZeroOrMinusOneIsInvalid
  {
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public SafeProcessHeapHandle()
      : base(true)
    {
    }

    protected override bool ReleaseHandle() => NativeMethods.FreeProcessHeapMemory(this.handle) == 0;
  }
}
