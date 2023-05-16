// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.ISignInInformationModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public interface ISignInInformationModel
  {
    bool SignInFailed { get; }

    string ErrorMessage { get; }

    SignInErrorCode ErrorCode { get; }

    string UserId { get; }

    string SignInURL { get; }

    string AccessToken { get; }

    string SignInEmail { get; }

    UserInformation GetUserInformation(string orgId);
  }
}
