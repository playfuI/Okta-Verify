// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.SignInResult
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using System;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public struct SignInResult
  {
    private SignInResult(
      bool success,
      EnrollmentParameters parameters,
      string message = null,
      SignInErrorCode errorCode = SignInErrorCode.Unknown)
    {
      this.IsSuccess = success;
      this.EnrollmentParameters = parameters;
      this.ErrorMessage = message;
      this.ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public EnrollmentParameters EnrollmentParameters { get; }

    public string ErrorMessage { get; }

    public SignInErrorCode ErrorCode { get; }

    public static SignInResult CreateError(string errorMessage = null, SignInErrorCode errorCode = SignInErrorCode.Unknown) => new SignInResult(false, new EnrollmentParameters(), errorMessage, errorCode);

    public static SignInResult Create(
      ISignInInformationModel signInInfo,
      string userEmail,
      string orgId)
    {
      return signInInfo != null ? new SignInResult(!signInInfo.SignInFailed, new EnrollmentParameters(new Uri(signInInfo.SignInURL), signInInfo.AccessToken, signInInfo.UserId, userEmail, orgId), signInInfo.ErrorMessage, signInInfo.ErrorCode) : SignInResult.CreateError();
    }
  }
}
