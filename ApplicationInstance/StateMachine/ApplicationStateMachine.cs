// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine.ApplicationStateMachine
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.UI.Enums;
using Okta.Authenticator.NativeApp.UI.Handlers;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine
{
  public class ApplicationStateMachine : IApplicationStateMachine
  {
    private static readonly TimeSpan ComputationDeferralTimeout = TimeSpan.FromSeconds(30.0);
    private static readonly TimeSpan AppStateDeferralTimeout = TimeSpan.FromMinutes(2.0);
    private readonly ILogger logger;
    private readonly IWindowActivationHandler windowHandler;
    private readonly ApplicationStateManager<ComputingStateType, ComputingStateContext, AppStateEvent, AppState> computingManager;
    private readonly ApplicationStateManager<ApplicationStateType, ApplicationStateContext, AppStateRequestEvent, AppStateRequest> appStateManager;
    private TemporaryApplicationStateType temporaryApplicationState;
    private ApplicationStateType finalApplicationState;

    public ApplicationStateMachine(
      ILogger logger,
      IEventAggregator eventAggregator,
      IWindowActivationHandler windowHandler)
    {
      this.logger = logger;
      this.windowHandler = windowHandler;
      this.computingManager = new ApplicationStateManager<ComputingStateType, ComputingStateContext, AppStateEvent, AppState>(ComputingStateType.Launching, logger, eventAggregator, (IEnumerable<ComputingStateType>) new ComputingStateType[2]
      {
        ComputingStateType.Loading,
        ComputingStateType.ShuttingDown
      });
      this.appStateManager = new ApplicationStateManager<ApplicationStateType, ApplicationStateContext, AppStateRequestEvent, AppStateRequest>(ApplicationStateType.Unknown, logger, eventAggregator, Enumerable.Empty<ApplicationStateType>());
      this.temporaryApplicationState = TemporaryApplicationStateType.None;
      this.finalApplicationState = ApplicationStateType.Unknown;
    }

    public ApplicationStateType ApplicationState => this.appStateManager.State;

    public ComputingStateType ComputingState => this.computingManager.State;

    public ApplicationStateContext ApplicationContext => this.appStateManager.Context;

    public ComputingStateContext ComputingContext => this.computingManager.Context;

    public bool TransitionTo(
      AppStateRequestType requestedTransition,
      ApplicationStateContext context = null)
    {
      return this.TransitionToWithDeferral(requestedTransition, (ApplicationStateDeferral) null, (string) null, context);
    }

    public bool TransitionToWithDeferral(
      AppStateRequestType requestedTransition,
      ApplicationStateDeferral deferralTask,
      string deferralTaskDescription,
      ApplicationStateContext context = null)
    {
      if (!this.IsAllowedTransition(requestedTransition))
        return false;
      (ApplicationStateType applicationStateType, bool IsTemporary) = this.GetApplicationStateType(requestedTransition);
      if (applicationStateType == ApplicationStateType.Unknown)
        throw new InvalidOperationException(string.Format("Unable to determine application state for {0} and current state {1}.", (object) requestedTransition, (object) this.ApplicationState));
      NativeWindowState state;
      if (applicationStateType == this.ApplicationState && (context != null ? (!context.ForceActivation ? 1 : 0) : 1) != 0 && !this.IsWindowStateOutOfSync(out state))
      {
        this.logger.WriteDebugEx(string.Format("Ignoring state transition for {0}. Current state: {1}|{2}", (object) applicationStateType, (object) this.ApplicationState, (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateMachine.cs", nameof (TransitionToWithDeferral));
        return deferralTask == null;
      }
      if (IsTemporary && this.temporaryApplicationState == TemporaryApplicationStateType.None)
      {
        this.temporaryApplicationState = TemporaryApplicationStateType.Start;
        this.finalApplicationState = this.ApplicationState;
        Func<ApplicationStateContext, Task> optionalDeferral = (Func<ApplicationStateContext, Task>) null;
        if (deferralTask != null)
          optionalDeferral = (Func<ApplicationStateContext, Task>) (c => deferralTask(c));
        return this.appStateManager.TransitionStateWithEventAndDeferrals(applicationStateType, (Func<ApplicationStateType, ApplicationStateContext, AppStateRequest>) ((s, c) => ApplicationStateMachine.GetAppStateRequestPayload(s, c, TemporaryApplicationStateType.Start)), context, ApplicationStateMachine.AppStateDeferralTimeout, optionalDeferral, deferralTaskDescription, new Action<bool>(this.FinishTemporaryActivation));
      }
      if (this.temporaryApplicationState == TemporaryApplicationStateType.Start)
        this.temporaryApplicationState = TemporaryApplicationStateType.InProgress;
      return this.appStateManager.TransitionStateWithEvent(applicationStateType, (Func<ApplicationStateType, ApplicationStateContext, AppStateRequest>) ((s, c) => ApplicationStateMachine.GetAppStateRequestPayload(s, c, this.temporaryApplicationState)), context);
    }

    public bool TransitionTo(ComputingStateType newState, ComputingStateContext context = null) => this.TransitionToWithDeferral(newState, (ComputingStateDeferral) null, (string) null, context);

    public bool TransitionToWithDeferral(
      ComputingStateType newState,
      ComputingStateDeferral deferralTask,
      string description,
      ComputingStateContext context = null)
    {
      if (newState == this.ComputingState)
      {
        this.logger.WriteDebugEx(string.Format("Ignoring state transition for {0}. Current state: {1}", (object) newState, (object) this.ComputingState), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateMachine.cs", nameof (TransitionToWithDeferral));
        return deferralTask == null;
      }
      if (!this.IsAllowedTransition(newState))
      {
        this.logger.WriteWarningEx(string.Format("State transition to {0} from current state {1} is disallowed.", (object) newState, (object) this.ComputingState), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateMachine.cs", nameof (TransitionToWithDeferral));
        return false;
      }
      Func<ComputingStateContext, Task> optionalDeferral = (Func<ComputingStateContext, Task>) null;
      if (deferralTask != null)
        optionalDeferral = (Func<ComputingStateContext, Task>) (c => deferralTask(c));
      switch (newState)
      {
        case ComputingStateType.Bootstrapping:
          return this.computingManager.TransitionStateWithEvent(newState, new Func<ComputingStateType, ComputingStateContext, AppState>(ApplicationStateMachine.GetAppStatePayload));
        case ComputingStateType.Loading:
          return this.computingManager.TransitionStateWithEventAndDeferrals(newState, new Func<ComputingStateType, ComputingStateContext, AppState>(ApplicationStateMachine.GetAppStatePayload), context, ApplicationStateMachine.ComputationDeferralTimeout, optionalDeferral, description, (Action<bool>) (b => this.computingManager.TransitionStateWithEvent(ComputingStateType.Idle, new Func<ComputingStateType, ComputingStateContext, AppState>(ApplicationStateMachine.GetAppStatePayload), context)));
        case ComputingStateType.ShuttingDown:
          return this.computingManager.TransitionStateWithEventAndDeferrals(newState, new Func<ComputingStateType, ComputingStateContext, AppState>(ApplicationStateMachine.GetAppStatePayload), context, ApplicationStateMachine.ComputationDeferralTimeout, optionalDeferral, description, (Action<bool>) (b => this.appStateManager.TransitionStateWithEvent(ApplicationStateType.InShutdown, new Func<ApplicationStateType, ApplicationStateContext, AppStateRequest>(ApplicationStateMachine.GetSteadyAppStateRequestPayload))));
        default:
          throw new NotImplementedException();
      }
    }

    public bool RegisterDeferral(
      AppStateRequestType state,
      ApplicationStateDeferral deferralTask,
      string description)
    {
      return this.appStateManager.RegisterDeferral(this.GetApplicationStateType(state).Type, (Func<ApplicationStateContext, Task>) (c => deferralTask(c)), description);
    }

    public bool UnregisterDeferral(
      AppStateRequestType state,
      ApplicationStateDeferral deferralTask,
      string description)
    {
      return this.appStateManager.UnregisterDeferral(this.GetApplicationStateType(state).Type, (Func<ApplicationStateContext, Task>) (c => deferralTask(c)), description);
    }

    public bool RegisterDeferral(
      ComputingStateType state,
      ComputingStateDeferral deferralTask,
      string description)
    {
      return this.computingManager.RegisterDeferral(state, (Func<ComputingStateContext, Task>) (c => deferralTask(c)), description);
    }

    public bool UnregisterDeferral(
      ComputingStateType state,
      ComputingStateDeferral deferralTask,
      string description)
    {
      return this.computingManager.UnregisterDeferral(state, (Func<ComputingStateContext, Task>) (c => deferralTask(c)), description);
    }

    public Task<bool> WaitForComputingTransitionFinished() => this.computingManager.WaitForDeferrals();

    private static AppState GetAppStatePayload(
      ComputingStateType state,
      ComputingStateContext context)
    {
      return new AppState(state, context != null ? context.Command : StartupArgumentType.Unknown);
    }

    private static AppStateRequest GetSteadyAppStateRequestPayload(
      ApplicationStateType state,
      ApplicationStateContext context)
    {
      return ApplicationStateMachine.GetAppStateRequestPayload(state, context, TemporaryApplicationStateType.None);
    }

    private static AppStateRequest GetAppStateRequestPayload(
      ApplicationStateType state,
      ApplicationStateContext context,
      TemporaryApplicationStateType temporaryState)
    {
      return new AppStateRequest(state, context != null && context.ForceActivation, temporaryState);
    }

    private static bool StateMatches(
      ApplicationStateType applicationState,
      NativeWindowState windowState)
    {
      switch (applicationState)
      {
        case ApplicationStateType.NormalInFocus:
          return windowState == NativeWindowState.NormalInFocus;
        case ApplicationStateType.NormalOutOfFocus:
          return windowState == NativeWindowState.NormalOutOfFocus;
        case ApplicationStateType.Minimized:
          return windowState == NativeWindowState.Minimized;
        case ApplicationStateType.SystemTray:
          return windowState == NativeWindowState.Hidden;
        default:
          return false;
      }
    }

    private (ApplicationStateType Type, bool IsTemporary) GetApplicationStateType(
      AppStateRequestType requestedState)
    {
      bool flag = requestedState == AppStateRequestType.TemporaryActivate || this.temporaryApplicationState == TemporaryApplicationStateType.Start || this.temporaryApplicationState == TemporaryApplicationStateType.InProgress;
      switch (requestedState)
      {
        case AppStateRequestType.Activate:
        case AppStateRequestType.TemporaryActivate:
          return (ApplicationStateType.NormalInFocus, flag);
        case AppStateRequestType.BringToFocus:
          return (ApplicationStateType.NormalInFocus, flag);
        case AppStateRequestType.LostFocus:
          return (this.ApplicationState == ApplicationStateType.NormalInFocus ? ApplicationStateType.NormalOutOfFocus : this.ApplicationState, flag);
        case AppStateRequestType.Minimize:
          return (ApplicationStateType.Minimized, flag);
        case AppStateRequestType.SendToSystemTray:
          return (ApplicationStateType.SystemTray, flag);
        default:
          return (ApplicationStateType.Unknown, flag);
      }
    }

    private bool IsAllowedTransition(ComputingStateType newState)
    {
      switch (newState)
      {
        case ComputingStateType.Bootstrapping:
          return this.ComputingState == ComputingStateType.Launching;
        case ComputingStateType.Loading:
          return this.ComputingState == ComputingStateType.Bootstrapping;
        case ComputingStateType.ShuttingDown:
          return this.ComputingState == ComputingStateType.Launching || this.ComputingState == ComputingStateType.Bootstrapping || this.ComputingState == ComputingStateType.Loading || this.ComputingState == ComputingStateType.Idle;
        default:
          return false;
      }
    }

    private bool IsAllowedTransition(AppStateRequestType newState)
    {
      if (this.ApplicationState != ApplicationStateType.InShutdown && this.ComputingState != ComputingStateType.ShuttingDown)
        return true;
      this.logger.WriteWarningEx(string.Format("Cannot transition to {0} during shutdown.", (object) newState), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateMachine.cs", nameof (IsAllowedTransition));
      return false;
    }

    private void FinishTemporaryActivation(bool finishedTransition)
    {
      int applicationState = (int) this.finalApplicationState;
      if (!finishedTransition)
        this.logger.WriteWarningEx("Not transitioning after temporary activation because transition did not finish.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateMachine.cs", nameof (FinishTemporaryActivation));
      else
        this.logger.WriteInfoEx(string.Format("Transitioning the app back to {0}...", (object) this.finalApplicationState), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateMachine.cs", nameof (FinishTemporaryActivation));
      this.appStateManager.TransitionStateWithEvent(this.finalApplicationState, (Func<ApplicationStateType, ApplicationStateContext, AppStateRequest>) ((s, c) => ApplicationStateMachine.GetAppStateRequestPayload(s, c, TemporaryApplicationStateType.Finished)));
      this.temporaryApplicationState = TemporaryApplicationStateType.None;
    }

    private bool IsWindowStateOutOfSync(out NativeWindowState state)
    {
      if (this.windowHandler.TryGetMainWindowState(out state))
        return !ApplicationStateMachine.StateMatches(this.ApplicationState, state);
      state = NativeWindowState.Unknown;
      return true;
    }
  }
}
