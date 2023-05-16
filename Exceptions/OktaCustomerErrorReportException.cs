// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Exceptions.OktaCustomerErrorReportException
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System;

namespace Okta.Authenticator.NativeApp.Exceptions
{
  public class OktaCustomerErrorReportException : OktaException
  {
    public OktaCustomerErrorReportException()
    {
    }

    public OktaCustomerErrorReportException(string message)
      : base(message)
    {
    }

    public OktaCustomerErrorReportException(string message, Exception innerException)
      : base(message, innerException)
    {
    }
  }
}
