// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.CopyDataStruct
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using System;
using System.Runtime.InteropServices;

namespace Okta.Authenticator.NativeApp.Interop
{
  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  internal struct CopyDataStruct
  {
    public IntPtr DataType;
    public int DataLength;
    public IntPtr Data;

    public CopyDataStruct(SingletonSignals type, int size, GCHandle data)
    {
      this.DataType = (IntPtr) (int) type;
      this.DataLength = size;
      this.Data = data.AddrOfPinnedObject();
    }

    public static CopyDataStruct FromPtr(IntPtr intPtr) => Marshal.PtrToStructure<CopyDataStruct>(intPtr);

    public string AsUnicodeString() => Marshal.PtrToStringUni(this.Data, this.DataLength / 2);

    public bool IsOfType(SingletonSignals type) => (SingletonSignals) (int) this.DataType == type;
  }
}
