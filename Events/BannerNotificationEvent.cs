// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.BannerNotificationEvent
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Prism.Events;
using System;

namespace Okta.Authenticator.NativeApp.Events
{
  public class BannerNotificationEvent : PubSubEvent<BannerNotification>
  {
    private readonly ThreadOption notifThreadOption = ThreadOption.UIThread;

    public void SubscribeToNotifications(Action<BannerNotification> notificationAction) => this.Subscribe(notificationAction, this.notifThreadOption);
  }
}
