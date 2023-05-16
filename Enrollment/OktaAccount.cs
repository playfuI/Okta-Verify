// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.OktaAccount
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Extensions.User;
using System;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public class OktaAccount : IOktaAccount
  {
    private readonly string internalDomain;
    private readonly string externalDomain;
    private readonly IUserInformation user;
    private Lazy<BitmapImage> lazyLogo;

    public OktaAccount(
      IUserInformation user,
      IOrganizationInformation org,
      IDeviceEnrollment enrollment,
      bool isUserVerificationEnabled)
    {
      user.EnsureNotNull(nameof (user));
      org.EnsureNotNull(nameof (org));
      enrollment.EnsureNotNull(nameof (enrollment));
      this.user = user;
      this.Organization = org;
      this.Enrollment = enrollment;
      this.IsUserVerificationEnabled = isUserVerificationEnabled;
      this.internalDomain = OktaWebClientExtensions.TryGetFQDN(org.InternalUrl);
      this.externalDomain = OktaWebClientExtensions.TryGetFQDN(enrollment.ExternalUrl);
      this.ShortName = user.GetShortName();
      this.lazyLogo = new Lazy<BitmapImage>(new Func<BitmapImage>(this.LazyImageInit), LazyThreadSafetyMode.PublicationOnly);
    }

    public string AccountId => this.Enrollment.AuthenticatorEnrollmentId;

    public string UserFullName => this.user.FullName;

    public string UserLogin => this.user.Login;

    public string ShortName { get; }

    public string UserId => this.user.Id;

    public string OrgId => this.Organization.Id;

    public string Domain => this.externalDomain ?? this.internalDomain;

    public IDeviceEnrollment Enrollment { get; }

    public IOrganizationInformation Organization { get; }

    public BitmapImage Logo => this.lazyLogo.Value;

    public bool IsUserVerificationEnabled { get; private set; }

    public void UpdateAccountKeyInformation(bool verificationEnabled) => this.IsUserVerificationEnabled = verificationEnabled;

    private BitmapImage LazyImageInit()
    {
      BitmapImage bitmapImage = (BitmapImage) null;
      if (this.Organization.Logo != null && this.Organization.Logo.Length != 0)
      {
        using (MemoryStream memoryStream = new MemoryStream(this.Organization.Logo))
        {
          bitmapImage = new BitmapImage();
          bitmapImage.BeginInit();
          bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
          bitmapImage.StreamSource = (Stream) memoryStream;
          bitmapImage.EndInit();
          if (bitmapImage.CanFreeze)
            bitmapImage.Freeze();
        }
      }
      return bitmapImage;
    }
  }
}
