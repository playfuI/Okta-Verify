// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.StartupHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.RegistryWatcher;
using Okta.DeviceAccess.Windows.Injector;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public class StartupHandler : IStartupHandler, IDisposable
  {
    private bool isDisposed;
    private OktaDeviceAccessRegistryWatcher oktaDeviceAccessRegistryEventListener;

    public string[] Arguments { get; private set; }

    public StartupArgumentType Command { get; private set; }

    public ISingleInstanceIdentifier InstanceIdentifier { get; private set; }

    public ILogger Logger { get; private set; }

    public IConfigurationManager ConfigurationManager { get; private set; }

    public void Initialize(string[] arguments)
    {
      this.InstanceIdentifier = (ISingleInstanceIdentifier) new SingleInstanceIdentifier();
      Okta.Authenticator.NativeApp.Injector.AppInjector.Initialize((Okta.Authenticator.NativeApp.Injector.BaseContainer) new OktaVerifyContainer(this.InstanceIdentifier));
      IConfigurationManager configurationManager = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IConfigurationManager>();
      ILogger logger = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<ILogger>();
      if (Okta.Authenticator.NativeApp.Extensions.IsDebugMode && configurationManager.ShouldLaunchDebugger(logger))
        Debugger.Launch();
      this.Initialize(arguments, logger, configurationManager);
      if (configurationManager.IsOktaDeviceAccessEnrolled())
        Okta.DeviceAccess.Windows.Injector.AppInjector.Initialize((Okta.DeviceAccess.Windows.Injector.BaseContainer) new OktaJoinContainer());
      else
        this.oktaDeviceAccessRegistryEventListener = new OktaDeviceAccessRegistryWatcher();
    }

    internal void Initialize(
      string[] arguments,
      ILogger logger,
      IConfigurationManager configurationManager)
    {
      logger.EnsureNotNull(nameof (logger));
      configurationManager.EnsureNotNull(nameof (configurationManager));
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      this.Logger = logger;
      this.Logger.WriteInfoEx(string.Format("Application is launching. Version: {0} - UI Thread: {1} ...", (object) executingAssembly.GetName().Version, (object) Thread.CurrentThread.ManagedThreadId), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StartupHandler.cs", nameof (Initialize));
      this.ConfigurationManager = configurationManager;
      this.Arguments = arguments ?? Array.Empty<string>();
      this.SetCommand();
      this.Logger.WriteDebugEx("Initializing " + (this.InstanceIdentifier.IsPrimaryInstance ? "primary instance" : "secondary instance") + " with Arguments \"" + this.GetArgumentsAsString() + "\"", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StartupHandler.cs", nameof (Initialize));
      this.ConfigurationManager.EnsureRegistryRootExists(this.Logger);
    }

    public StartupOperation GetStartupOperation()
    {
      switch (this.Command)
      {
        case StartupArgumentType.None:
          return !this.InstanceIdentifier.IsPrimaryInstance ? StartupOperation.Shutdown : StartupOperation.Activate;
        case StartupArgumentType.Configure:
          return StartupOperation.Shutdown;
        case StartupArgumentType.ReportError:
        case StartupArgumentType.Shutdown:
        case StartupArgumentType.RemoveAll:
          return StartupOperation.Shutdown;
        case StartupArgumentType.Force:
          return StartupOperation.Activate;
        case StartupArgumentType.TestCrashReport:
          return StartupOperation.TestCrash;
        case StartupArgumentType.ShowReportIssue:
          return !this.InstanceIdentifier.IsPrimaryInstance ? StartupOperation.Shutdown : StartupOperation.ShowReportIssue;
        case StartupArgumentType.ShowAbout:
          return !this.InstanceIdentifier.IsPrimaryInstance ? StartupOperation.Shutdown : StartupOperation.ShowAbout;
        case StartupArgumentType.ShowSettings:
          return !this.InstanceIdentifier.IsPrimaryInstance ? StartupOperation.Shutdown : StartupOperation.ShowSettings;
        default:
          return !this.InstanceIdentifier.IsPrimaryInstance ? StartupOperation.Shutdown : StartupOperation.MoveToSystemTray;
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
      if (this.isDisposed)
        return;
      if (isDisposing)
      {
        this.oktaDeviceAccessRegistryEventListener?.Dispose();
        Okta.Authenticator.NativeApp.Injector.AppInjector.SafeDispose();
        this.InstanceIdentifier.SafeDispose();
      }
      this.isDisposed = true;
    }

    private string GetArgumentsAsString() => string.Join(" ", this.Arguments);

    private bool TryGetArgument(int index, out string value)
    {
      value = (string) null;
      if (this.Arguments == null || this.Arguments.Length <= index)
        return false;
      value = this.Arguments[index];
      return true;
    }

    private void SetCommand()
    {
      string str;
      if (!this.TryGetArgument(0, out str) || string.IsNullOrWhiteSpace(str))
      {
        this.Command = StartupArgumentType.None;
      }
      else
      {
        StartupArgumentType result;
        if (str.Length > 2 && Enum.TryParse<StartupArgumentType>(str.Substring(2), true, out result))
        {
          this.Command = result;
        }
        else
        {
          this.Logger.WriteWarningEx("Okta Verify was launched with unknown arguments \"" + this.GetArgumentsAsString() + "\"", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StartupHandler.cs", nameof (SetCommand));
          this.Command = StartupArgumentType.Unknown;
        }
      }
    }
  }
}
