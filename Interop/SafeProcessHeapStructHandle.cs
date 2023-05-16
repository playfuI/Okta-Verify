// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.SafeProcessHeapStructHandle
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.Runtime.InteropServices;

namespace Okta.Authenticator.NativeApp.Interop
{
  internal class SafeProcessHeapStructHandle : SafeProcessHeapHandle
  {
    public T[] ToStructArray<T>(int arrayLength)
    {
      int num = Marshal.SizeOf<T>();
      T[] structArray = new T[arrayLength];
      for (int index = 0; index < arrayLength; ++index)
      {
        IntPtr ptr = new IntPtr(this.handle.ToInt64() + (long) (index * num));
        structArray[index] = Marshal.PtrToStructure<T>(ptr);
      }
      return structArray;
    }
  }
}
