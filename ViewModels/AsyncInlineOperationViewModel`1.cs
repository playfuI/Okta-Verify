// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AsyncInlineOperationViewModel`1
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public abstract class AsyncInlineOperationViewModel<T> : BaseViewModel
  {
    private readonly TaskCompletionSource<T> taskCompletionSource;

    protected AsyncInlineOperationViewModel() => this.taskCompletionSource = new TaskCompletionSource<T>();

    public Task<T> ResultTask => this.taskCompletionSource.Task;

    protected bool IsFinished => this.ResultTask.IsCompleted || this.ResultTask.IsCanceled || this.ResultTask.IsFaulted;

    protected bool TrySetResult(T result) => this.taskCompletionSource.TrySetResult(result);
  }
}
