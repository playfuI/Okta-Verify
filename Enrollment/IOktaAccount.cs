// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.IOktaAccount
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System.Windows.Media.Imaging;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public interface IOktaAccount
  {
    string AccountId { get; }

    string UserFullName { get; }

    string ShortName { get; }

    string UserLogin { get; }

    string UserId { get; }

    string Domain { get; }

    string OrgId { get; }

    BitmapImage Logo { get; }

    bool IsUserVerificationEnabled { get; }

    IDeviceEnrollment Enrollment { get; }

    IOrganizationInformation Organization { get; }

    void UpdateAccountKeyInformation(bool verificationEnabled);
  }
}
