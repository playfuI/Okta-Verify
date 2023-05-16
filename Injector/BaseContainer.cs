// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Injector.BaseContainer
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Properties;
using SimpleInjector;
using System;

namespace Okta.Authenticator.NativeApp.Injector
{
  public abstract class BaseContainer : IDisposable
  {
    private readonly Container container = new Container();
    private bool isDisposed;

    public void Initialize()
    {
      this.RegisterDependencies();
      this.container.Verify(!BuildSettings.IsMainBuild ? VerificationOption.VerifyAndDiagnose : VerificationOption.VerifyOnly);
    }

    public T GetInstance<T>() where T : class => this.container.GetInstance<T>();

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.isDisposed)
        return;
      if (disposing)
        this.container.Dispose();
      this.isDisposed = true;
    }

    protected abstract void RegisterDependencies();

    protected void RegisterInstance<T>(T instance) where T : class => this.container.RegisterInstance<T>(instance);

    protected void RegisterSingleton<TInterface, TConcrete>()
      where TInterface : class
      where TConcrete : class, TInterface
    {
      this.container.RegisterSingleton<TInterface, TConcrete>();
    }

    protected void RegisterSingleton<TConcrete>(params Type[] interfaces) where TConcrete : class
    {
      if (interfaces != null && interfaces.Length < 1)
        return;
      Type c = typeof (TConcrete);
      Registration registration = Lifestyle.Singleton.CreateRegistration<TConcrete>(this.container);
      foreach (Type serviceType in interfaces)
      {
        if (!serviceType.IsAssignableFrom(c))
          throw new ArgumentException(c.Name + " does not implement " + serviceType.Name + ".");
        this.container.AddRegistration(serviceType, registration);
      }
    }
  }
}
