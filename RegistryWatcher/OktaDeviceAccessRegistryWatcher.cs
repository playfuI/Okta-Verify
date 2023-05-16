// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.RegistryWatcher.OktaDeviceAccessRegistryWatcher
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Win32;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.DeviceAccess.Windows.Injector;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.Management;

namespace Okta.Authenticator.NativeApp.RegistryWatcher
{
  public class OktaDeviceAccessRegistryWatcher : IDisposable
  {
    private const string OktaRootKeyPath = "Software\\\\Okta";
    private const string OktaDeviceAccessKeyPath = "Software\\\\Okta\\\\Okta Device Access";
    private readonly IEventAggregator eventAggregator;
    private readonly IConfigurationManager configurationManager;
    private readonly ILogger logger;
    private ManagementEventWatcher registryWatcher;
    private OktaDeviceAccessRegistryWatcherState watcherState;
    private bool disposedValue;

    public OktaDeviceAccessRegistryWatcher()
    {
      this.configurationManager = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IConfigurationManager>();
      this.logger = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<ILogger>();
      this.eventAggregator = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IEventAggregator>();
      this.registryWatcher = new ManagementEventWatcher();
      this.registryWatcher.EventArrived += new EventArrivedEventHandler(this.RegistryEventHandler);
      this.Initialize();
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposedValue)
        return;
      if (disposing && this.registryWatcher != null)
      {
        this.registryWatcher.EventArrived -= new EventArrivedEventHandler(this.RegistryEventHandler);
        this.registryWatcher.Dispose();
      }
      this.disposedValue = true;
    }

    private WqlEventQuery GetQuery(string keyPath) => new WqlEventQuery("SELECT * FROM RegistryKeyChangeEvent WHERE Hive = 'HKEY_LOCAL_MACHINE' AND KeyPath = '" + keyPath + "'");

    private void Initialize()
    {
      this.logger.WriteInfoEx("Initializing registry watcher", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (Initialize));
      try
      {
        if (this.configurationManager.RegistryKeyExists(this.logger, Registry.LocalMachine, "Software\\Okta\\Okta Device Access"))
        {
          this.logger.WriteInfoEx("Registry watcher: 'Okta Device Access' key found, watching key", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (Initialize));
          this.watcherState = OktaDeviceAccessRegistryWatcherState.OktaDeviceAccessKey;
          this.registryWatcher.Query = (EventQuery) this.GetQuery("Software\\\\Okta\\\\Okta Device Access");
        }
        else
        {
          this.logger.WriteInfoEx("Registry watcher: 'Okta Device Access' key not found, watching 'Okta' key", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (Initialize));
          this.watcherState = OktaDeviceAccessRegistryWatcherState.OktaRootKey;
          this.registryWatcher.Query = (EventQuery) this.GetQuery("Software\\\\Okta");
        }
        this.registryWatcher.Start();
        this.logger.WriteInfoEx("Registry watcher: Started", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (Initialize));
      }
      catch (ManagementException ex)
      {
        this.logger.WriteErrorEx("Error starting registry watcher", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (Initialize));
        this.Dispose();
      }
    }

    private void RegistryEventHandler(object sender, EventArrivedEventArgs e)
    {
      if (this.watcherState == OktaDeviceAccessRegistryWatcherState.OktaRootKey && this.configurationManager.RegistryKeyExists(this.logger, Registry.LocalMachine, "Software\\Okta\\Okta Device Access"))
      {
        this.logger.WriteInfoEx("Registry watcher: 'Okta Device Access' key appeared, stopping and re-initializing watcher", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (RegistryEventHandler));
        this.registryWatcher.Stop();
        this.logger.WriteInfoEx("Registry watcher: Stopped", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (RegistryEventHandler));
        if (this.configurationManager.IsOktaDeviceAccessEnrolled())
        {
          this.InitializeOktaDeviceAccess();
          this.Dispose();
        }
        else
          this.Initialize();
      }
      else if (this.watcherState == OktaDeviceAccessRegistryWatcherState.OktaDeviceAccessKey && this.configurationManager.IsOktaDeviceAccessEnrolled())
      {
        this.InitializeOktaDeviceAccess();
        this.Dispose();
      }
      else
        this.logger.WriteInfoEx("Registry watcher: Change detected but not what we're looking for", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (RegistryEventHandler));
    }

    private void InitializeOktaDeviceAccess()
    {
      this.logger.WriteInfoEx("Registry watcher: Criteria met, initializing Okta Device Access", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\RegistryWatcher\\OktaDeviceAccessRegistryWatcher.cs", nameof (InitializeOktaDeviceAccess));
      Okta.DeviceAccess.Windows.Injector.AppInjector.Initialize((Okta.DeviceAccess.Windows.Injector.BaseContainer) new OktaJoinContainer());
      this.eventAggregator.GetEvent<OktaDeviceAccessRegistryChangedEvent>().Publish();
    }
  }
}
