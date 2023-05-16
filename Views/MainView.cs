// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Views.MainView
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UI.Handlers;
using Okta.Authenticator.NativeApp.UI.Models;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.Views
{
  public class MainView : Window, IDisposable, IComponentConnector
  {
    private readonly IEventAggregator aggregator;
    private readonly ILogger logger;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IWindowActivationHandler windowActivationHandler;
    private readonly IWindowsMessageReceiver windowsMessageReceiver;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IWindowsMessageSender windowsMessageSender;
    private readonly bool isDormant;
    private bool shutdownRequested;
    private bool disposed;
    private HwndSource hwndSource;
    private AppStateRequestEvent appStateRequestEvent;
    internal Border topNavBorder;
    internal ListBox topNav;
    internal NotificationBannerView NotificationBanner;
    private bool _contentLoaded;

    public MainView(bool isDormant = false)
    {
      this.isDormant = isDormant;
      this.InitializeComponent();
      this.aggregator = AppInjector.Get<IEventAggregator>();
      this.logger = AppInjector.Get<ILogger>();
      this.stateMachine = AppInjector.Get<IApplicationStateMachine>();
      this.windowActivationHandler = AppInjector.Get<IWindowActivationHandler>();
      this.windowsMessageReceiver = AppInjector.Get<IWindowsMessageReceiver>();
      this.analyticsProvider = AppInjector.Get<IAnalyticsProvider>();
      this.windowsMessageSender = AppInjector.Get<IWindowsMessageSender>();
      this.RegisterEvents();
    }

    public void Initialize() => this.Dispatcher.Invoke((Action) (() =>
    {
      this.WindowState = WindowState.Minimized;
      if (!this.windowActivationHandler.IsInitialized)
      {
        this.logger.WriteInfoEx("Making sure window handle is initialized...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (Initialize));
        this.Show();
      }
      this.Hide();
    }));

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing && this.hwndSource != null)
      {
        this.hwndSource.RemoveHook(new HwndSourceHook(this.windowsMessageReceiver.OnIncomingMessage));
        this.hwndSource.Dispose();
      }
      this.disposed = true;
    }

    protected override void OnStateChanged(EventArgs e)
    {
      if (this.WindowState == WindowState.Minimized && this.stateMachine.ApplicationState != ApplicationStateType.SystemTray && this.stateMachine.ApplicationState != ApplicationStateType.InShutdown)
        this.TransitionState(AppStateRequestType.Minimize);
      else if (this.WindowState == WindowState.Normal && this.stateMachine.ApplicationState == ApplicationStateType.Minimized)
        this.TransitionState(AppStateRequestType.Activate);
      base.OnStateChanged(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      if (this.shutdownRequested)
      {
        base.OnClosing(e);
      }
      else
      {
        this.TransitionState(AppStateRequestType.SendToSystemTray);
        if (e == null)
          return;
        e.Cancel = true;
      }
    }

    protected override void OnActivated(EventArgs e)
    {
      if (this.stateMachine.ApplicationState == ApplicationStateType.NormalOutOfFocus)
        this.TransitionState(AppStateRequestType.Activate);
      base.OnActivated(e);
    }

    protected override void OnDeactivated(EventArgs e)
    {
      if (this.WindowState == WindowState.Normal)
        this.TransitionState(AppStateRequestType.LostFocus);
      base.OnDeactivated(e);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
      base.OnSourceInitialized(e);
      WindowInteropHelper windowInteropHelper = new WindowInteropHelper((Window) this);
      this.windowActivationHandler.Initialize(new NativeWindowModel(windowInteropHelper.Handle));
      this.hwndSource = HwndSource.FromHwnd(windowInteropHelper.Handle);
      this.hwndSource.AddHook(new HwndSourceHook(this.windowsMessageReceiver.OnIncomingMessage));
      this.logger.WriteInfoEx("Main window initialized.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (OnSourceInitialized));
    }

    private void EnsureActivated(bool forceActivation, bool shouldAttach) => this.Dispatcher.Invoke((Action) (() =>
    {
      bool flag1 = false;
      bool flag2 = false;
      this.logger.WriteDebugEx("Activating main window...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (EnsureActivated));
      if (shouldAttach)
        flag1 = this.windowActivationHandler.AttachToCurrentForegroundWindow();
      this.Show();
      this.WindowState = WindowState.Normal;
      if (this.IsActive && this.IsFocused)
      {
        this.logger.WriteDebugEx("Window is already active, no action taken.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (EnsureActivated));
      }
      else
      {
        if (this.Activate())
        {
          flag2 = true;
          this.logger.WriteDebugEx("Window successfully activated through Activate.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (EnsureActivated));
        }
        else if (forceActivation)
        {
          (string, uint) error = ();
          (string ErrorMessage, uint ErrorCode) errorHandle;
          flag2 = this.windowActivationHandler.ForceMainWindowActivation(out errorHandle) || this.ActivateWithSecondaryCallback(out error);
          if (!flag2)
            this.analyticsProvider.TrackErrorWithLogs("Forceful activation failed with " + errorHandle.ErrorMessage + " and " + error.Item1, (int) errorHandle.ErrorCode, nameof (EnsureActivated));
        }
        else
          this.logger.WriteInfoEx("Failed to activate, not forcing it.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (EnsureActivated));
        if (!flag2)
        {
          this.Topmost = true;
          this.Topmost = false;
        }
        if (!(shouldAttach & flag1))
          return;
        this.windowActivationHandler.AlignWindows();
      }
    }));

    private void EnsureMinimized() => this.Dispatcher.Invoke((Action) (() =>
    {
      if (this.WindowState == WindowState.Minimized)
        return;
      this.windowActivationHandler.RestoreToPreviousWindow();
      this.WindowState = WindowState.Minimized;
    }));

    private void SendToSystemTray()
    {
      this.logger.WriteInfoEx("Sending the app to System tray...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (SendToSystemTray));
      this.Initialize();
      this.aggregator.GetEvent<AppStateEvent>().Publish(new AppState(ComputingStateType.Resetting));
    }

    private void RegisterEvents()
    {
      this.appStateRequestEvent = this.aggregator.GetEvent<AppStateRequestEvent>();
      this.appStateRequestEvent.Subscribe(new Action<AppStateRequest>(this.OnAppStateRequested));
    }

    private void OnAppStateRequested(AppStateRequest requestedState)
    {
      this.logger.WriteInfoEx(string.Format("Changing application state: {0} | {1}", (object) requestedState.State, (object) requestedState.TemporaryStateType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Views\\MainView.xaml.cs", nameof (OnAppStateRequested));
      switch (requestedState.State)
      {
        case ApplicationStateType.NormalInFocus:
          this.EnsureActivated(requestedState.ForceActivation, (requestedState.TemporaryStateType == TemporaryApplicationStateType.Start || requestedState.TemporaryStateType == TemporaryApplicationStateType.InProgress) && !this.windowActivationHandler.IsAttached);
          break;
        case ApplicationStateType.NormalOutOfFocus:
          if (requestedState.TemporaryStateType != TemporaryApplicationStateType.InProgress)
          {
            this.Dispatcher.Invoke(new Action(this.windowActivationHandler.RestoreToPreviousWindow));
            break;
          }
          break;
        case ApplicationStateType.Minimized:
          this.EnsureMinimized();
          break;
        case ApplicationStateType.SystemTray:
          this.SendToSystemTray();
          break;
        case ApplicationStateType.InShutdown:
          this.shutdownRequested = true;
          this.Dispatcher.Invoke(new Action(((Window) this).Close));
          break;
      }
      if (requestedState.TemporaryStateType != TemporaryApplicationStateType.Finished)
        return;
      this.windowActivationHandler.ClearAttachedWindow();
    }

    private bool TransitionState(AppStateRequestType state) => !this.isDormant && this.stateMachine.TransitionTo(state);

    private bool ActivateWithSecondaryCallback(out (string ErrorMessage, uint Code) error)
    {
      error.Item2 = (uint) this.windowsMessageSender.SignalInstance(SingletonSignals.ActivationRequestCallback);
      if (error.Item2 == 0U)
      {
        error.Item1 = (string) null;
        return true;
      }
      error.Item1 = "Failed to signal secondary instance for activation";
      return false;
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
      if (this._contentLoaded)
        return;
      this._contentLoaded = true;
      Application.LoadComponent((object) this, new Uri("/OktaVerify;V4.0.2.0;component/ui/views/mainview.xaml", UriKind.Relative));
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    internal Delegate _CreateDelegate(Type delegateType, string handler) => Delegate.CreateDelegate(delegateType, (object) this, handler);

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void IComponentConnector.Connect(int connectionId, object target)
    {
      switch (connectionId)
      {
        case 1:
          this.topNavBorder = (Border) target;
          break;
        case 2:
          this.topNav = (ListBox) target;
          break;
        case 3:
          this.NotificationBanner = (NotificationBannerView) target;
          break;
        default:
          this._contentLoaded = true;
          break;
      }
    }
  }
}
