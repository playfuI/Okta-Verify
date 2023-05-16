// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Events.EventPayloads.ExecutionDeferral
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Events.EventPayloads
{
  public class ExecutionDeferral : IDisposable
  {
    private readonly TaskCompletionSource<bool> completeSource;
    private bool disposedValue;

    public ExecutionDeferral() => this.completeSource = new TaskCompletionSource<bool>((object) TaskContinuationOptions.RunContinuationsAsynchronously);

    internal Task CompleteTask => (Task) this.completeSource.Task;

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposedValue)
        return;
      if (disposing)
        this.completeSource.TrySetResult(true);
      this.disposedValue = true;
    }
  }
}
