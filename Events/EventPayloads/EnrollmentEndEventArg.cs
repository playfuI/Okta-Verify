// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.EnrollmentEndEventArg
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public struct EnrollmentEndEventArg
  {
    private EnrollmentEndEventArg(
      EnrollEndEventType enrollType,
      string orgId,
      string accountId = null,
      string enrollMessage = null,
      IDeviceEnrollment enrollment = null)
    {
      this.EnrollType = enrollType;
      this.Enrollment = enrollment;
      this.AccountId = accountId;
      this.AccountOrgId = orgId;
      this.EnrollMessage = enrollMessage;
    }

    public static EnrollmentEndEventArg AsAccountUpdate(
      bool isSuccess,
      string orgId,
      string accountId,
      string message)
    {
      return new EnrollmentEndEventArg(isSuccess ? EnrollEndEventType.AccountUpdated : EnrollEndEventType.AccountUpdateFailed, orgId, accountId, message);
    }

    public static EnrollmentEndEventArg AsAccountAdded(IDeviceEnrollment enrollment) => new EnrollmentEndEventArg(EnrollEndEventType.AccountAdded, enrollment?.OrganizationId, enrollment?.AuthenticatorEnrollmentId, enrollment: enrollment);

    public static EnrollmentEndEventArg AsEnrollmentFailure(
      string orgId,
      string message,
      bool isCancellation = false)
    {
      return new EnrollmentEndEventArg(isCancellation ? EnrollEndEventType.EnrollmentCancelled : EnrollEndEventType.EnrollmentFailed, orgId, enrollMessage: message);
    }

    public static EnrollmentEndEventArg AsAccountUpdateCancelled(string orgId, string accountId) => new EnrollmentEndEventArg(EnrollEndEventType.AccountUpdateCancelled, orgId, accountId);

    public EnrollEndEventType EnrollType { get; }

    public IDeviceEnrollment Enrollment { get; }

    public string EnrollMessage { get; }

    public string AccountId { get; }

    public string AccountOrgId { get; }
  }
}
