// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.EnrollmentError
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using Okta.Devices.SDK.WebClient.Errors;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public struct EnrollmentError
  {
    public EnrollmentError(PlatformErrorCode errorCode)
      : this(EnrollmentFailureReason.CryptoError, (int) errorCode)
    {
    }

    public EnrollmentError(OktaApiErrorCode errorCode)
      : this(EnrollmentFailureReason.ApiError, (int) errorCode)
    {
    }

    private EnrollmentError(EnrollmentFailureReason reason, int errorCode)
    {
      this.Reason = reason;
      this.ErrorCode = errorCode;
    }

    public EnrollmentFailureReason Reason { get; }

    public int ErrorCode { get; }
  }
}
