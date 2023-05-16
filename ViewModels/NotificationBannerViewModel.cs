// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.NotificationBannerViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class NotificationBannerViewModel : BaseViewModel
  {
    private bool isNotificationVisible;

    public BannerType BannerType { get; private set; }

    public string Message { get; private set; }

    public bool IsNotificationVisible
    {
      get => this.isNotificationVisible;
      set => this.UpdateNotificationState(value);
    }

    public virtual void ShowBanner(string message, BannerType bannerType = BannerType.Error)
    {
      this.isNotificationVisible = true;
      this.Message = message;
      this.BannerType = bannerType;
      this.FireChangeEvent();
    }

    public void UpdateNotificationState(bool isEnabled)
    {
      this.isNotificationVisible = isEnabled;
      if (!isEnabled)
      {
        this.BannerType = BannerType.Unknown;
        this.Message = (string) null;
      }
      this.FireChangeEvent();
    }

    private void FireChangeEvent()
    {
      this.FirePropertyChangedEvent("IsNotificationVisible");
      this.FirePropertyChangedEvent("Message");
      this.FirePropertyChangedEvent("BannerType");
    }
  }
}
