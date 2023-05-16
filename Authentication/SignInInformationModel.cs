// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.SignInInformationModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System.Collections.Generic;
using System.Security.Claims;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class SignInInformationModel : ISignInInformationModel
  {
    public bool SignInFailed { get; internal set; }

    public string ErrorMessage { get; internal set; }

    public SignInErrorCode ErrorCode { get; internal set; }

    public string UserId { get; internal set; }

    public string SignInURL { get; internal set; }

    public string AccessToken { get; internal set; }

    public string SignInEmail { get; internal set; }

    internal IEnumerable<Claim> UserClaims { get; set; }

    public UserInformation GetUserInformation(string orgId) => new UserInformation(this.UserClaims, orgId);
  }
}
