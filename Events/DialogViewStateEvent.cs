// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.DialogViewStateEvent
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Events
{
  public class DialogViewStateEvent : PubSubEvent<IDialogViewState>
  {
    private readonly ConcurrentDictionary<DialogViewType, (TaskCompletionSource<bool> TaskSource, SubscriptionToken Token)> resultSubscriptions = new ConcurrentDictionary<DialogViewType, (TaskCompletionSource<bool>, SubscriptionToken)>(2, 2);

    public void RequestDialogDisplay<TPayload>(DialogViewType dialogType, TPayload payload = null) where TPayload : class => this.Publish((IDialogViewState) new DialogViewState<TPayload>(dialogType, payload: payload));

    public virtual Task<bool> RequestDialogDisplayWithResult<TPayload>(
      IEventAggregator aggregator,
      DialogViewType dialogType,
      TPayload payload = null)
      where TPayload : class
    {
      if (aggregator == null)
        return Task.FromResult<bool>(false);
      (TaskCompletionSource<bool> TaskSource, SubscriptionToken Token) tuple;
      if (this.resultSubscriptions.TryGetValue(dialogType, out tuple))
        return tuple.TaskSource.Task;
      TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
      SubscriptionToken subscriptionToken = aggregator.GetEvent<DialogViewStateResultEvent>().Subscribe(new Action<DialogViewStateResult>(this.OnDialogResultUpdated));
      this.resultSubscriptions.TryAdd(dialogType, (completionSource, subscriptionToken));
      this.RequestDialogDisplay<TPayload>(dialogType, payload);
      return completionSource.Task;
    }

    public void RequestDialogClosure(DialogViewType dialogType) => this.Publish((IDialogViewState) new DialogViewState<object>(dialogType, false));

    internal int PendingResults => this.resultSubscriptions.Count;

    private void OnDialogResultUpdated(DialogViewStateResult viewState)
    {
      (TaskCompletionSource<bool> TaskSource, SubscriptionToken Token) tuple;
      if (!this.resultSubscriptions.TryRemove(viewState.ViewType, out tuple))
        return;
      tuple.TaskSource.TrySetResult(viewState.DialogResult);
      this.Unsubscribe(tuple.Token);
    }
  }
}
