// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine.ApplicationStateManager`4
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine
{
  internal class ApplicationStateManager<TState, TContext, TEvent, TEventPayload>
    where TState : Enum
    where TContext : class, new()
    where TEvent : PubSubEvent<TEventPayload>, new()
  {
    private readonly ILogger logger;
    private readonly IEventAggregator eventAggregator;
    private readonly IDictionary<TState, List<(Func<TContext, Task>, string)>> registeredDeferrals;
    private bool isProcessingDeferrals;
    private ExecutionDeferralHandler deferralHandler;

    public ApplicationStateManager(
      TState initialState,
      ILogger logger,
      IEventAggregator eventAggregator,
      IEnumerable<TState> deferralStates)
    {
      this.State = initialState;
      this.logger = logger;
      this.eventAggregator = eventAggregator;
      this.registeredDeferrals = (IDictionary<TState, List<(Func<TContext, Task>, string)>>) deferralStates.ToDictionary<TState, TState, List<(Func<TContext, Task>, string)>>((Func<TState, TState>) (k => k), (Func<TState, List<(Func<TContext, Task>, string)>>) (k => new List<(Func<TContext, Task>, string)>()));
      this.Context = default (TContext);
    }

    public TState State { get; private set; }

    public TContext Context { get; private set; }

    public bool RegisterDeferral(
      TState state,
      Func<TContext, Task> deferralTask,
      string description)
    {
      List<(Func<TContext, Task>, string)> valueTupleList;
      if (deferralTask == null || !this.registeredDeferrals.TryGetValue(state, out valueTupleList))
      {
        this.logger.WriteWarningEx(string.Format("Failed to register deferral \"{0}\" for state transition {1}.", (object) description, (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (RegisterDeferral));
        return false;
      }
      valueTupleList.Add((deferralTask, description));
      return true;
    }

    public bool UnregisterDeferral(
      TState state,
      Func<TContext, Task> deferralTask,
      string description)
    {
      List<(Func<TContext, Task>, string)> source;
      if (deferralTask == null || !this.registeredDeferrals.TryGetValue(state, out source))
      {
        this.logger.WriteWarningEx(string.Format("Failed to unregister deferral \"{0}\" for state transition {1}.", (object) description, (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (UnregisterDeferral));
        return false;
      }
      (Func<TContext, Task>, string) tuple1 = source.FirstOrDefault<(Func<TContext, Task>, string)>((Func<(Func<TContext, Task>, string), bool>) (t => t.Item1 == deferralTask && t.Item2.Equals(description, StringComparison.Ordinal)));
      (Func<TContext, Task>, string) tuple2 = tuple1;
      if (!((Delegate) tuple2.Item1 == (Delegate) null) || !(tuple2.Item2 == (string) null))
        return source.Remove(tuple1);
      this.logger.WriteWarningEx(string.Format("Did not find deferral \"{0}\" for state transition {1} to unregister.", (object) description, (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (UnregisterDeferral));
      return false;
    }

    public bool TransitionStateWithEventAndDeferrals(
      TState newState,
      Func<TState, TContext, TEventPayload> payloadCreator,
      TContext context,
      TimeSpan executionTimeout,
      Func<TContext, Task> optionalDeferral,
      string optionalDeferralDescription,
      Action<bool> finalAction)
    {
      this.Context = context ?? new TContext();
      if (this.isProcessingDeferrals)
      {
        this.logger.WriteWarningEx(string.Format("Cannot transition to {0}, previous state {1} still has active deferrals.", (object) newState, (object) this.State), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (TransitionStateWithEventAndDeferrals));
        return false;
      }
      this.deferralHandler = new ExecutionDeferralHandler(executionTimeout);
      using (this.deferralHandler.RequestDeferral())
      {
        if (optionalDeferral != null)
          this.AddDeferralTask(optionalDeferral(this.Context), optionalDeferralDescription);
        List<(Func<TContext, Task>, string)> tupleList;
        if (this.registeredDeferrals.TryGetValue(newState, out tupleList))
        {
          foreach ((Func<TContext, Task>, string) tuple in tupleList)
            this.AddDeferralTask(tuple.Item1(this.Context), tuple.Item2);
        }
        this.isProcessingDeferrals = true;
      }
      this.DeferralContinuation(newState, finalAction).AsBackgroundTask(string.Format("waiting for {0} deferrals", (object) newState), this.logger);
      return this.TransitionStateWithEvent(newState, payloadCreator, context);
    }

    public bool TransitionStateWithEvent(
      TState newState,
      Func<TState, TContext, TEventPayload> payloadCreator,
      TContext context = null)
    {
      this.Context = context ?? new TContext();
      this.State = newState;
      this.eventAggregator.GetEvent<TEvent>().Publish(payloadCreator(newState, context));
      return true;
    }

    public async Task<bool> WaitForDeferrals() => this.deferralHandler != null && await this.deferralHandler.WaitForDeferrals().ConfigureAwait(false);

    private static async Task RunDeferralTask(Task deferralTask, ExecutionDeferral deferral)
    {
      try
      {
        await deferralTask.ConfigureAwait(false);
      }
      finally
      {
        deferral.Dispose();
      }
    }

    private async Task DeferralContinuation(TState state, Action<bool> finalAction)
    {
      bool flag = await this.WaitForDeferrals().ConfigureAwait(false);
      this.isProcessingDeferrals = false;
      if (flag)
        this.logger.WriteInfoEx(string.Format("Deferrals for {0} state finished successfully.", (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (DeferralContinuation));
      else
        this.logger.WriteWarningEx(string.Format("Deferrals for {0} state did not finish successfully.", (object) state), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (DeferralContinuation));
      Action<bool> action = finalAction;
      if (action == null)
        return;
      action(flag);
    }

    private bool AddDeferralTask(Task deferralTask, string description)
    {
      ExecutionDeferral deferral = this.deferralHandler.RequestDeferral();
      if (deferral == null)
      {
        this.logger.WriteWarningEx("Failed to register deferral " + description + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\StateMachine\\ApplicationStateManager.cs", nameof (AddDeferralTask));
        return false;
      }
      ApplicationStateManager<TState, TContext, TEvent, TEventPayload>.RunDeferralTask(deferralTask, deferral).AsBackgroundTask(string.Format("running deferral \"{0}\" for state transition {1}", (object) description, (object) this.State));
      return true;
    }
  }
}
