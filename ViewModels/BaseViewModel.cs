// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.BaseViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Prism.Events;
using System.ComponentModel;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class BaseViewModel : INotifyPropertyChanged
  {
    public BaseViewModel()
    {
      this.EventAggregator = AppInjector.Get<IEventAggregator>();
      this.Logger = AppInjector.Get<ILogger>();
      this.AnalyticsProvider = AppInjector.Get<IAnalyticsProvider>();
      this.ApplicationHandler = AppInjector.Get<IApplicationHandler>();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected IEventAggregator EventAggregator { get; }

    protected ILogger Logger { get; }

    protected IAnalyticsProvider AnalyticsProvider { get; }

    protected IApplicationHandler ApplicationHandler { get; }

    protected void FirePropertyChangedEvent(string property)
    {
      PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
      if (propertyChanged == null)
        return;
      propertyChanged((object) this, new PropertyChangedEventArgs(property));
    }

    protected void FireViewModelChangedEvent() => this.FirePropertyChangedEvent(string.Empty);

    protected void LaunchLink(string appOrBrowserAddress) => this.ApplicationHandler.InvokeUri(appOrBrowserAddress);
  }
}
