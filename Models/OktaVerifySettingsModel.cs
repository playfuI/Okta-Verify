// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Models.OktaVerifySettingsModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using SQLite;

namespace Okta.Authenticator.NativeApp.Models
{
  public class OktaVerifySettingsModel
  {
    [PrimaryKey]
    public string Id { get; set; }

    public AppThemeSettingType AppTheme { get; set; }

    public bool? AnalyticsReportChoice { get; set; }

    public bool? EnrolledInBetaProgram { get; set; }
  }
}
