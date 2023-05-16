﻿// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.SafeProcessHeapIntArrayHandle
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Runtime.InteropServices;

namespace Okta.Authenticator.NativeApp.Interop
{
  internal class SafeProcessHeapIntArrayHandle : SafeProcessHeapHandle
  {
    public int[] ToIntArray(int size)
    {
      int[] destination = new int[size];
      Marshal.Copy(this.handle, destination, 0, size);
      return destination;
    }
  }
}
