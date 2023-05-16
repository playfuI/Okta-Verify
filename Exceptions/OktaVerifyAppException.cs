// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Exceptions.OktaVerifyAppException
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System;

namespace Okta.Authenticator.NativeApp.Exceptions
{
  public class OktaVerifyAppException : OktaException
  {
    public OktaVerifyAppException(string message)
      : base(message)
    {
    }

    public OktaVerifyAppException(string message, int hResult)
      : base(message)
    {
      this.HResult = hResult;
    }

    public OktaVerifyAppException(string message, Exception innerException)
      : base(message, innerException)
    {
    }

    public OktaVerifyAppException(string message, Exception innerException, int hResult)
      : base(message, innerException)
    {
      this.HResult = hResult;
    }

    public bool SkipAnalyticsReport { get; set; }
  }
}
