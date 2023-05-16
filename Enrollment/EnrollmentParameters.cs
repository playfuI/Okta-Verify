// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.EnrollmentParameters
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public struct EnrollmentParameters
  {
    public EnrollmentParameters(
      Uri signInUri,
      string accessToken,
      string userId,
      string userEmail,
      string orgId)
    {
      this.SignInUri = signInUri;
      this.AccessToken = accessToken;
      this.UserId = userId;
      this.UserEmail = userEmail;
      this.OrgId = orgId;
    }

    public Uri SignInUri { get; }

    public string SignInUrl => this.SignInUri?.AbsoluteUri;

    public string AccessToken { get; }

    public string UserId { get; }

    public string UserEmail { get; }

    public string OrgId { get; }
  }
}
