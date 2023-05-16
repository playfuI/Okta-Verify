// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.AccountSettingsModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using SQLite;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public class AccountSettingsModel
  {
    [PrimaryKey]
    public string AccountId { get; set; }

    public bool IsDefaultAccount { get; set; }

    [Ignore]
    public AccountErrorStateTypes DismissedAccountWarnings
    {
      get => (AccountErrorStateTypes) this.DismissedAccountWarningsValue;
      set => this.DismissedAccountWarningsValue = (int) value;
    }

    [Column("DismissedAccountWarnings")]
    public int DismissedAccountWarningsValue { get; set; }
  }
}
