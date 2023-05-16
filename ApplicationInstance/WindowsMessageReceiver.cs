// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.WindowsMessageReceiver
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Bindings;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public class WindowsMessageReceiver : IWindowsMessageReceiver
  {
    private readonly ILogger logger;
    private readonly ISingleInstanceIdentifier singleInstanceIdentifier;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IBindingsManager bindingsManager;
    private readonly IApplicationHandler appHandler;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IEventAggregator eventAggregator;

    public WindowsMessageReceiver(
      ILogger logger,
      ISingleInstanceIdentifier singleInstanceIdentifier,
      IApplicationStateMachine stateMachine,
      IBindingsManager bindingsManager,
      IApplicationHandler appHandler,
      IAnalyticsProvider analyticsProvider,
      IEventAggregator eventAggregator)
    {
      this.logger = logger;
      this.singleInstanceIdentifier = singleInstanceIdentifier;
      this.stateMachine = stateMachine;
      this.bindingsManager = bindingsManager;
      this.appHandler = appHandler;
      this.analyticsProvider = analyticsProvider;
      this.eventAggregator = eventAggregator;
    }

    public IntPtr OnIncomingMessage(
      IntPtr hWndReceiver,
      int msgId,
      IntPtr hWndSender,
      IntPtr dataPointer,
      ref bool handled)
    {
      if (msgId == 74 && this.ProcessCopyDataMessage(hWndReceiver, hWndSender, dataPointer, ref handled) || msgId == 689 && this.ProcessSessionChangeMessage(hWndReceiver, hWndSender, dataPointer, ref handled))
        return hWndReceiver;
      if (this.EnsureMessageTarget(msgId))
        this.ProcessSignalMessage(hWndReceiver, hWndSender, dataPointer, ref handled);
      return IntPtr.Zero;
    }

    private bool EnsureMessageTarget(int targetId)
    {
      if (this.singleInstanceIdentifier.InstanceMessageId != 0U)
        return (long) targetId == (long) this.singleInstanceIdentifier.InstanceMessageId;
      string str = string.Format("Failed to receive message because signalling message Id is not registered. Error 0x{0:X8}", (object) NativeMethods.GetLastError());
      this.logger.WriteErrorEx(str, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (EnsureMessageTarget));
      this.analyticsProvider.TrackErrorWithLogsAndAppData(str, sourceMethodName: nameof (EnsureMessageTarget));
      return false;
    }

    private bool ProcessCopyDataMessage(
      IntPtr hWndReceiver,
      IntPtr hWndSender,
      IntPtr dataPtr,
      ref bool handled)
    {
      CopyDataStruct copyDataStruct = CopyDataStruct.FromPtr(dataPtr);
      this.logger.WriteDebugEx(string.Format("Received copy data message at 0x{0:x8} from 0x{1:X8}.", (object) hWndReceiver, (object) hWndSender), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (ProcessCopyDataMessage));
      if (this.singleInstanceIdentifier.IsPrimaryInstance && copyDataStruct.IsOfType(SingletonSignals.URIActivate))
      {
        this.bindingsManager.ProcessUriSignalActivationAsync(copyDataStruct.AsUnicodeString()).AsBackgroundTask("binding activation from secondary");
        handled = true;
        return true;
      }
      this.logger.WriteErrorEx(string.Format("Not handling window message with data type {0} on {1}.", (object) copyDataStruct.DataType, this.singleInstanceIdentifier.IsPrimaryInstance ? (object) "Primary" : (object) "Secondary"), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (ProcessCopyDataMessage));
      return false;
    }

    private bool ProcessSessionChangeMessage(
      IntPtr hWndReceiver,
      IntPtr changeType,
      IntPtr sessionId,
      ref bool handled)
    {
      int num = (int) changeType;
      WtsSessionChangeType sessionChangeType = Enum.IsDefined(typeof (WtsSessionChangeType), (object) num) ? (WtsSessionChangeType) num : WtsSessionChangeType.Unknown;
      this.logger.WriteDebugEx(string.Format("Received session change message at 0x{0:X8} for {1} and session 0x{2:X8}.", (object) hWndReceiver, (object) sessionChangeType, (object) sessionId), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (ProcessSessionChangeMessage));
      if (!this.singleInstanceIdentifier.IsPrimaryInstance)
        return false;
      switch (sessionChangeType)
      {
        case WtsSessionChangeType.ConsoleConnect:
        case WtsSessionChangeType.RemoteConnect:
        case WtsSessionChangeType.SessionLogon:
        case WtsSessionChangeType.SessionUnlock:
          this.SendSessionChangeEvent(true);
          handled = true;
          return true;
        case WtsSessionChangeType.ConsoleDisconnect:
        case WtsSessionChangeType.RemoteDisconnect:
        case WtsSessionChangeType.SessionLogoff:
        case WtsSessionChangeType.SessionLock:
          this.SendSessionChangeEvent(false);
          handled = true;
          return true;
        default:
          return false;
      }
    }

    private void SendSessionChangeEvent(bool sessionStart) => this.eventAggregator.GetEvent<UserSessionChangedEvent>()?.Publish(sessionStart ? UserSessionChangedEventType.ActivatedUserProfile : UserSessionChangedEventType.InactiveUserProfile);

    private void ProcessSignalMessage(
      IntPtr hWndReceiver,
      IntPtr hWndSender,
      IntPtr signalParameter,
      ref bool handled)
    {
      SingletonSignals singletonSignals = (SingletonSignals) (int) signalParameter;
      this.logger.WriteDebugEx(string.Format("Received signal {0} at 0x{1:x8} from 0x{2:X8}.", (object) singletonSignals, (object) hWndReceiver, (object) hWndSender), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (ProcessSignalMessage));
      if (!this.singleInstanceIdentifier.IsPrimaryInstance)
      {
        if (singletonSignals == SingletonSignals.ActivationRequestCallback && hWndSender != IntPtr.Zero)
        {
          handled = NativeMethods.SetForegroundWindow(hWndSender);
          if (handled)
            return;
          this.logger.WriteWarningEx(string.Format("Failed to set foreground window to 0x{0:X8}", (object) hWndSender), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (ProcessSignalMessage));
        }
        else
          this.logger.WriteDebugEx(string.Format("Ignoring {0} signal for secondary instance.", (object) signalParameter), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageReceiver.cs", nameof (ProcessSignalMessage));
      }
      else if (singletonSignals.HasFlag((Enum) SingletonSignals.Shutdown))
      {
        this.stateMachine.TransitionTo(ComputingStateType.ShuttingDown);
        handled = true;
      }
      else
      {
        if (singletonSignals.HasFlag((Enum) SingletonSignals.Activate))
          this.stateMachine.TransitionTo(AppStateRequestType.Activate);
        else if (singletonSignals.HasFlag((Enum) SingletonSignals.MoveToSystemTray))
          this.stateMachine.TransitionTo(AppStateRequestType.SendToSystemTray);
        if (singletonSignals.HasFlag((Enum) SingletonSignals.CheckBindings))
          this.bindingsManager?.ProcessBindingRefresh();
        if (singletonSignals.HasFlag((Enum) SingletonSignals.OpenReportIssueWindow))
          this.appHandler.InvokeOnUIThread(new Action(this.appHandler.ShowReportIssueWindow));
        else if (singletonSignals.HasFlag((Enum) SingletonSignals.OpenAboutWindow))
          this.appHandler.InvokeOnUIThread(new Action(this.appHandler.ShowAboutWindow));
        else if (singletonSignals.HasFlag((Enum) SingletonSignals.OpenSettingsWindow))
          this.appHandler.InvokeOnUIThread(new Action(this.appHandler.ShowSettings));
        handled = true;
      }
    }
  }
}
