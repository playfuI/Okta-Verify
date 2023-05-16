// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.AuthenticationRequestContext
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using Okta.Devices.SDK.Exceptions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class AuthenticationRequestContext
  {
    private readonly TaskCompletionSource<bool> completionSource;
    private readonly TaskCompletionSource<bool> prioritizationSource;
    private int taskCounter;

    public AuthenticationRequestContext(string id)
    {
      this.Id = id;
      this.Status = AuthenticationRequestStatus.Started;
      this.KeyInteractions = (IDictionary<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>) new Dictionary<VerificationCredentialType, (AuthenticationRequestStatus, string, PlatformErrorCode)>();
      this.completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.prioritizationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.taskCounter = 0;
    }

    public IDictionary<VerificationCredentialType, (AuthenticationRequestStatus Status, string Id, PlatformErrorCode Error)> KeyInteractions { get; }

    public string Id { get; }

    public AuthenticationRequestStatus Status { get; private set; }

    public bool HasUserInteraction { get; internal set; }

    public bool RequiresFocus { get; internal set; }

    public bool IsBindingRequest { get; internal set; }

    public string OrgId { get; internal set; }

    public string OrgUrl { get; internal set; }

    public string UserId { get; internal set; }

    public string AppName { get; internal set; }

    public string EnrollmentId { get; internal set; }

    public OktaBindingFailureReason FailureReason { get; private set; }

    public Task CompletionTask => (Task) this.completionSource.Task;

    public Task PrioritizationTask => (Task) this.prioritizationSource.Task;

    public void SetResult(bool success, OktaBindingFailureReason failureReason)
    {
      this.Status = success ? AuthenticationRequestStatus.Succeeded : AuthenticationRequestStatus.Failed;
      this.FailureReason = failureReason;
      this.completionSource.SetResult(success);
    }

    public bool RequestInteractionQueuing(out TaskCompletionSource<bool> source)
    {
      if (Interlocked.Increment(ref this.taskCounter) > 1)
      {
        source = (TaskCompletionSource<bool>) null;
        return false;
      }
      source = this.prioritizationSource;
      return true;
    }
  }
}
