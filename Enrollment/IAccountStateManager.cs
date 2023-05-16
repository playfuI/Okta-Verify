// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.IAccountStateManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Devices.SDK.WebClient.Errors;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public interface IAccountStateManager
  {
    bool AreAccountKeysInvalidated(string accountId);

    AccountLifecycleEventType DetectedLifecycleChange(string accountId);

    bool IsErrorStateCritical(AccountErrorStateTypes errorState);

    void TrackAccountLifecycleChange(
      string accountId,
      AccountLifecycleEventType lifecycleChangeType);

    void TrackAccountApiError(string accountId, OktaApiErrorCode apiErrorCode);

    void TrackAccountKeyChanges(
      string accountId,
      bool isPoPKeyInvalidated,
      bool isUVKeyInvalidated);

    bool EnsureAccountStateReset(string accountId, bool resetKeys = true, bool resetLifecycle = true);

    Task<TResult> InvokeWithAccountStateTrack<TResult>(
      string accountId,
      Func<Task<TResult>> actionWithResult);
  }
}
