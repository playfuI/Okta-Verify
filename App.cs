// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.App
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.Views;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Extensions.Error;
using Okta.Devices.SDK.Telemetry;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Okta.Authenticator.NativeApp
{
  public class App : Application, IDisposable
  {
    private readonly Stopwatch applicationInitStopwatch;
    private readonly StartupHandler startupHandler;
    private readonly bool useSoftwareRender;
    private bool disposed;
    private MainView mainWindow;
    private ILogger logger;
    private IAnalyticsProvider analyticsProvider;
    private IAnalyticsRepository analyticsRepository;
    private ITelemetryDataManager telemetryData;
    private bool _contentLoaded;

    public App()
    {
      this.useSoftwareRender = OktaVerifyConfigurationManager.UseSoftwareRender();
      if (this.useSoftwareRender)
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
      this.applicationInitStopwatch = new Stopwatch();
      this.applicationInitStopwatch.Start();
      this.startupHandler = new StartupHandler();
      this.Initialize();
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
      await this.HandleStartup(e).ConfigureAwait(true);
      if (!this.useSoftwareRender)
        return;
      this.logger.WriteInfoEx("Using Software Render mode.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (OnStartup));
      this.analyticsProvider?.TrackEvent("SoftwareRender");
    }

    private async Task HandleStartup(StartupEventArgs e)
    {
      App app = this;
      app.startupHandler.Initialize(e?.Args);
      app.logger = AppInjector.Get<ILogger>();
      app.analyticsProvider = AppInjector.Get<IAnalyticsProvider>();
      app.analyticsRepository = AppInjector.Get<IAnalyticsRepository>();
      app.telemetryData = AppInjector.Get<ITelemetryDataManager>();
      IApplicationStateMachine stateMachine = AppInjector.Get<IApplicationStateMachine>();
      DevicesSdk.SetLogger();
      stateMachine.TransitionTo(ComputingStateType.Bootstrapping);
      ComputingStateContext stateContext = new ComputingStateContext(app.startupHandler.Command, app.startupHandler.Arguments);
      StartupOperation operation = app.startupHandler.GetStartupOperation();
      switch (operation)
      {
        case StartupOperation.Shutdown:
          app.logger.WriteDebugEx("Initiating shutdown for the current app instance", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (HandleStartup));
          if (app.startupHandler.Command == StartupArgumentType.Uri)
          {
            app.mainWindow = new MainView(true);
            app.mainWindow.Initialize();
          }
          if (!stateMachine.TransitionTo(ComputingStateType.ShuttingDown, stateContext))
            app.logger.WriteErrorEx("Failed to transistion to shutdown", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (HandleStartup));
          if (await stateMachine.WaitForComputingTransitionFinished().ConfigureAwait(true))
            app.logger.WriteInfoEx("Shutting down current application, all tasks completed.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (HandleStartup));
          else
            app.logger.WriteErrorEx("Shutting down current application, tasks did not complete.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (HandleStartup));
          app.Shutdown();
          stateMachine = (IApplicationStateMachine) null;
          stateContext = (ComputingStateContext) null;
          break;
        case StartupOperation.TestCrash:
          await Task.Delay(10000);
          throw new InvalidProgramException("This is a test crash.");
        default:
          await DevicesSdk.EnsureInitialized().ConfigureAwait(true);
          app.mainWindow = new MainView();
          app.HandleInitializationWithTelemetry(stateMachine, stateContext).AsBackgroundTask("startup telemetry");
          if (operation == StartupOperation.MoveToSystemTray)
          {
            stateMachine.TransitionTo(AppStateRequestType.SendToSystemTray);
            stateMachine = (IApplicationStateMachine) null;
            stateContext = (ComputingStateContext) null;
            break;
          }
          stateMachine.TransitionTo(AppStateRequestType.Activate);
          stateMachine = (IApplicationStateMachine) null;
          stateContext = (ComputingStateContext) null;
          break;
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing)
        this.ReleaseResources();
      this.disposed = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
      this.ReleaseResources();
      base.OnExit(e);
    }

    private void Initialize()
    {
      this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(this.ProcessDispatcherUnhandledExceptions);
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.ProcessAppDomainUnhandledExceptions);
      TaskScheduler.UnobservedTaskException += new EventHandler<UnobservedTaskExceptionEventArgs>(this.OnUnobservedTaskException);
      MultilingualHelper.CheckIfCustomLocaleRequested();
    }

    private void ReleaseResources()
    {
      this.mainWindow?.Dispose();
      this.startupHandler.SafeDispose();
    }

    private void ProcessDispatcherUnhandledExceptions(
      object sender,
      DispatcherUnhandledExceptionEventArgs e)
    {
      AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(this.ProcessAppDomainUnhandledExceptions);
      this.ProcessUnhandledException("Dispatcher", (object) e.Exception);
      e.Handled = false;
    }

    private void ProcessAppDomainUnhandledExceptions(object sender, UnhandledExceptionEventArgs e) => this.ProcessUnhandledException("App Domain", e.ExceptionObject);

    private void ProcessUnhandledException(string name, object exceptionObject)
    {
      ILogger logger = this.logger;
      if (logger != null)
        logger.WriteErrorEx(string.Format("Unhandled {0} exception: {1}", (object) name, exceptionObject), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (ProcessUnhandledException));
      this.telemetryData?.PersistDataInLog();
      this.analyticsRepository?.OnApplicationCrash();
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
      Exception ex = e.Exception.FlattenException();
      ILogger logger = this.logger;
      if (logger != null)
        logger.WriteErrorEx("Task faulted: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\App.xaml.cs", nameof (OnUnobservedTaskException));
      IAnalyticsProvider analyticsProvider = this.analyticsProvider;
      if (analyticsProvider == null)
        return;
      analyticsProvider.TrackErrorWithLogsAndAppData(ex);
    }

    private async Task HandleInitializationWithTelemetry(
      IApplicationStateMachine stateMachine,
      ComputingStateContext stateContext)
    {
      using (ITrackedOperation operation = this.analyticsProvider.StartScenario(this.startupHandler.InstanceIdentifier.IsPrimaryInstance ? AppTelemetryScenario.PrimaryApplicationLaunch : AppTelemetryScenario.SecondaryApplicationLaunch, this.applicationInitStopwatch))
      {
        if (!stateMachine.TransitionTo(ComputingStateType.Loading, stateContext))
        {
          operation?.SetStatus(TelemetryEventStatus.ClientError);
          return;
        }
        bool flag = await stateMachine.WaitForComputingTransitionFinished().ConfigureAwait(false);
        this.telemetryData.AddCustomDataPoint(AppTelemetryDataKey.StartupType, (object) stateContext?.Command);
        foreach (KeyValuePair<AppTelemetryDataKey, object> telemetryDataPoint in (IEnumerable<KeyValuePair<AppTelemetryDataKey, object>>) this.telemetryData.GetAllTelemetryDataPoints())
          this.analyticsProvider.AddScenarioData<AppTelemetryDataKey>(telemetryDataPoint.Key, telemetryDataPoint.Value);
        operation?.SetStatus(flag ? TelemetryEventStatus.Success : TelemetryEventStatus.ClientTimeout);
      }
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
      if (this._contentLoaded)
        return;
      this._contentLoaded = true;
      Application.LoadComponent((object) this, new Uri("/OktaVerify;V4.0.2.0;component/app.xaml", UriKind.Relative));
    }

    [STAThread]
    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public static void Main()
    {
      App app = new App();
      app.InitializeComponent();
      app.Run();
    }
  }
}
