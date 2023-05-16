// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Bindings.BindingInformationModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Windows.Media.Imaging;

namespace Okta.Authenticator.NativeApp.Bindings
{
  public class BindingInformationModel
  {
    public string AppName { get; set; }

    public string UserEmail { get; set; }

    public string AccountId { get; set; }

    public string RequestReferrer { get; set; }

    public BitmapImage Logo { get; set; }
  }
}
