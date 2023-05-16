// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Injector.AppInjector
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;

namespace Okta.Authenticator.NativeApp.Injector
{
  public class AppInjector
  {
    private static BaseContainer container;

    public static void Initialize(BaseContainer container)
    {
      if (container == null)
        return;
      AppInjector.container = container;
      AppInjector.container.Initialize();
    }

    public static T Get<T>() where T : class
    {
      try
      {
        BaseContainer container = AppInjector.container;
        return container != null ? container.GetInstance<T>() : default (T);
      }
      catch (ObjectDisposedException ex)
      {
        return default (T);
      }
    }

    public static void SafeDispose()
    {
      BaseContainer container = AppInjector.container;
      if (container == null)
        return;
      container.SafeDispose();
    }
  }
}
