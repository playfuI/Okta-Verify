// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.EnrollmentStateContext
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public class EnrollmentStateContext
  {
    public EnrollmentStateContext(EnrollmentStateType stateType) => this.EnrollmentState = stateType;

    public EnrollmentStateContext(
      EnrollmentStateType stateType,
      EnrollmentStartEventArg enrollmentArg)
      : this(stateType)
    {
      this.AccountDomain = enrollmentArg.AccountDomain;
      this.AccountOrgId = enrollmentArg.AccountOrgId;
      this.AppName = enrollmentArg.AppName;
      this.UserEmail = enrollmentArg.UserEmail;
      this.UserId = enrollmentArg.UserId;
      this.AccountId = enrollmentArg.AccountId;
    }

    public EnrollmentStateType EnrollmentState { get; }

    public string AccountDomain { get; }

    public string AccountOrgId { get; }

    public string AppName { get; }

    public string UserEmail { get; }

    public string UserId { get; }

    public string AccountId { get; }
  }
}
