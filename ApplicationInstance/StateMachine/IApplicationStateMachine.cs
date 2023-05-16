// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine.IApplicationStateMachine
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine
{
  public interface IApplicationStateMachine
  {
    ApplicationStateType ApplicationState { get; }

    ComputingStateType ComputingState { get; }

    ApplicationStateContext ApplicationContext { get; }

    ComputingStateContext ComputingContext { get; }

    bool TransitionTo(AppStateRequestType state, ApplicationStateContext context = null);

    bool TransitionToWithDeferral(
      AppStateRequestType state,
      ApplicationStateDeferral deferralTask,
      string description,
      ApplicationStateContext context = null);

    bool TransitionTo(ComputingStateType state, ComputingStateContext context = null);

    bool TransitionToWithDeferral(
      ComputingStateType state,
      ComputingStateDeferral deferralTask,
      string description,
      ComputingStateContext context = null);

    bool RegisterDeferral(
      AppStateRequestType state,
      ApplicationStateDeferral deferralTask,
      string description);

    bool UnregisterDeferral(
      AppStateRequestType state,
      ApplicationStateDeferral deferralTask,
      string description);

    bool RegisterDeferral(
      ComputingStateType state,
      ComputingStateDeferral deferralTask,
      string description);

    bool UnregisterDeferral(
      ComputingStateType state,
      ComputingStateDeferral deferralTask,
      string description);

    Task<bool> WaitForComputingTransitionFinished();
  }
}
