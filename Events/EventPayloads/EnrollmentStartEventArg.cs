// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.EnrollmentStartEventArg
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public struct EnrollmentStartEventArg
  {
    private EnrollmentStartEventArg(
      EnrollStartEventType enrollType,
      string orgDomain = null,
      string orgId = null,
      string appName = null,
      string userEmail = null,
      string userId = null,
      string accountId = null)
    {
      this.EnrollType = enrollType;
      this.AccountDomain = orgDomain;
      this.AccountOrgId = orgId;
      this.AppName = appName;
      this.UserEmail = userEmail;
      this.UserId = userId;
      this.AccountId = accountId;
    }

    public static EnrollmentStartEventArg AsManualEnrollmentRequest() => new EnrollmentStartEventArg(EnrollStartEventType.AddAccount);

    public static EnrollmentStartEventArg AsUserVerificationEnrollmentRequest(
      EnrollStartEventType uvUpdateType,
      IOktaAccount account)
    {
      return new EnrollmentStartEventArg(uvUpdateType, account?.Enrollment?.ExternalUrl ?? account?.Organization?.InternalUrl, account?.OrgId, userEmail: account?.UserLogin, userId: account?.UserId, accountId: account?.AccountId);
    }

    public static EnrollmentStartEventArg AsJITEnrollmentRequest(
      string orgDomain,
      string orgId,
      string appName)
    {
      return new EnrollmentStartEventArg(EnrollStartEventType.JustInTimeEnrollment, orgDomain, orgId, appName);
    }

    public static EnrollmentStartEventArg AsReEnrollmentRequest(IOktaAccount account)
    {
      string orgDomain = account?.Enrollment?.ExternalUrl ?? account?.Organization?.InternalUrl;
      string orgId = account?.OrgId;
      string accountId1 = account?.AccountId;
      string userLogin = account?.UserLogin;
      string userId = account?.UserId;
      string accountId2 = accountId1;
      return new EnrollmentStartEventArg(EnrollStartEventType.ReEnrollment, orgDomain, orgId, userEmail: userLogin, userId: userId, accountId: accountId2);
    }

    public EnrollStartEventType EnrollType { get; }

    public string AccountDomain { get; }

    public string AccountOrgId { get; }

    public string AppName { get; }

    public string UserEmail { get; }

    public string UserId { get; }

    public string AccountId { get; }
  }
}
