// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.ExecutionDeferralHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public class ExecutionDeferralHandler
  {
    private readonly TaskCompletionSource<bool> deferralsCS;
    private int deferralsCount;

    public ExecutionDeferralHandler(TimeSpan executionTimeout)
    {
      this.deferralsCS = new TaskCompletionSource<bool>();
      Task.Delay(executionTimeout).ContinueWith(new Action<Task>(this.OnDeferralTimeout), TaskScheduler.Default);
    }

    public ExecutionDeferral RequestDeferral()
    {
      if (this.deferralsCS.Task.Status >= TaskStatus.RanToCompletion)
        return (ExecutionDeferral) null;
      Interlocked.Increment(ref this.deferralsCount);
      ExecutionDeferral executionDeferral = new ExecutionDeferral();
      executionDeferral.CompleteTask.ContinueWith(new Action<Task>(this.OnDeferralCompleted), TaskScheduler.Default);
      return executionDeferral;
    }

    public async Task<bool> WaitForDeferrals() => await this.deferralsCS.Task.ConfigureAwait(true);

    private void OnDeferralTimeout(Task timeoutTask) => this.deferralsCS.TrySetResult(this.deferralsCount < 1);

    private void OnDeferralCompleted(Task deferralTask)
    {
      if (Interlocked.Decrement(ref this.deferralsCount) >= 1 || this.deferralsCount >= 1)
        return;
      this.deferralsCS.TrySetResult(true);
    }
  }
}
