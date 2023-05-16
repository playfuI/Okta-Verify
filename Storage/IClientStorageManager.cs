// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Storage.IClientStorageManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Models;
using Okta.Devices.SDK;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Storage
{
  public interface IClientStorageManager
  {
    IStorageManager Store { get; }

    Task<OktaVerifySettingsModel> GetAppSettings();

    Task<bool> UpdateAppSettings(Action<OktaVerifySettingsModel> updateAction);

    void SubscribeToStoreUpdates(Action<StoreChangeType, IList> storeUpdateAction);

    void UnsubscribeFromStoreUpdates(Action<StoreChangeType, IList> storeUpdateAction);
  }
}
