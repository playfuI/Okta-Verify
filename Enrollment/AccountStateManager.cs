// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.AccountStateManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.WebClient.Errors;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public class AccountStateManager : IAccountStateManager
  {
    private static readonly AccountErrorStateTypes CriticalAccountStates = AccountErrorStateTypes.UserVerificationRequiredByServer | AccountErrorStateTypes.AccountReenrollmentRequired | AccountErrorStateTypes.AccountInvalidated;
    private readonly IEventAggregator eventAggregator;
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<string, AccountStateContext> detectedAccountStateChanges;

    public AccountStateManager(IEventAggregator eventAggregator, ILogger logger)
    {
      this.eventAggregator = eventAggregator;
      this.logger = logger;
      this.detectedAccountStateChanges = new ConcurrentDictionary<string, AccountStateContext>();
    }

    public AccountLifecycleEventType DetectedLifecycleChange(string accountId)
    {
      AccountStateContext accountStateContext;
      return string.IsNullOrEmpty(accountId) || !this.detectedAccountStateChanges.TryGetValue(accountId, out accountStateContext) ? AccountLifecycleEventType.None : accountStateContext.LifecycleChange;
    }

    public bool AreAccountKeysInvalidated(string accountId)
    {
      AccountStateContext accountStateContext;
      return !string.IsNullOrEmpty(accountId) && this.detectedAccountStateChanges.TryGetValue(accountId, out accountStateContext) && accountStateContext.IsPoPKeyInvalidated && accountStateContext.IsUVKeyInvalidated;
    }

    public bool IsErrorStateCritical(AccountErrorStateTypes errorState) => AccountStateManager.CriticalAccountStates.HasFlag((Enum) errorState);

    public void TrackAccountApiError(string accountId, OktaApiErrorCode apiErrorCode)
    {
      AccountLifecycleEventType lifecycleChangeType = AccountLifecycleEventType.None;
      switch (apiErrorCode)
      {
        case OktaApiErrorCode.DeviceDeactivated:
          lifecycleChangeType = AccountLifecycleEventType.DeviceDeactivated;
          break;
        case OktaApiErrorCode.DeviceDeleted:
          lifecycleChangeType = AccountLifecycleEventType.DeviceDeleted;
          break;
        case OktaApiErrorCode.EnrollmentReset:
          lifecycleChangeType = AccountLifecycleEventType.EnrollmentReset;
          break;
        case OktaApiErrorCode.UserNotActive:
          lifecycleChangeType = AccountLifecycleEventType.UserDeactivated;
          break;
        case OktaApiErrorCode.UserDeleted:
          lifecycleChangeType = AccountLifecycleEventType.UserDeleted;
          break;
      }
      this.TrackAccountLifecycleChange(accountId, lifecycleChangeType);
    }

    public void TrackAccountLifecycleChange(
      string accountId,
      AccountLifecycleEventType lifecycleChangeType)
    {
      if (string.IsNullOrEmpty(accountId) || lifecycleChangeType == AccountLifecycleEventType.None)
        return;
      AccountStateContext payload = this.detectedAccountStateChanges.AddOrUpdate(accountId, new AccountStateContext(accountId, false, lifecycleChange: lifecycleChangeType), (Func<string, AccountStateContext, AccountStateContext>) ((k, current) => new AccountStateContext(accountId, current.IsUVKeyInvalidated, current.IsPoPKeyInvalidated, lifecycleChangeType)));
      this.eventAggregator.GetEvent<AccountStateEvent>().Publish(payload);
    }

    public void TrackAccountKeyChanges(
      string accountId,
      bool isPoPKeyInvalidated,
      bool isUVKeyInvalidated)
    {
      if (string.IsNullOrEmpty(accountId) || !(isPoPKeyInvalidated | isUVKeyInvalidated))
        return;
      this.detectedAccountStateChanges.AddOrUpdate(accountId, new AccountStateContext(accountId, isUVKeyInvalidated, isPoPKeyInvalidated), (Func<string, AccountStateContext, AccountStateContext>) ((k, v) => new AccountStateContext(accountId, isUVKeyInvalidated, isPoPKeyInvalidated, v.LifecycleChange)));
      this.eventAggregator.GetEvent<AccountStateEvent>().Publish(new AccountStateContext(accountId, isUVKeyInvalidated, isPoPKeyInvalidated));
    }

    public bool EnsureAccountStateReset(string accountId, bool resetKeys = true, bool resetLifecycle = true)
    {
      AccountStateContext comparisonValue;
      if (string.IsNullOrEmpty(accountId) || !this.detectedAccountStateChanges.TryGetValue(accountId, out comparisonValue))
        return false;
      AccountStateContext accountStateContext = resetKeys ? new AccountStateContext(accountId, false, lifecycleChange: resetLifecycle ? AccountLifecycleEventType.None : comparisonValue.LifecycleChange) : new AccountStateContext(accountId, comparisonValue.IsUVKeyInvalidated, comparisonValue.IsPoPKeyInvalidated, resetLifecycle ? AccountLifecycleEventType.None : comparisonValue.LifecycleChange);
      int num = accountStateContext.IsPoPKeyInvalidated || accountStateContext.IsUVKeyInvalidated || accountStateContext.LifecycleChange != AccountLifecycleEventType.None ? (this.detectedAccountStateChanges.TryUpdate(accountId, accountStateContext, comparisonValue) ? 1 : 0) : (this.detectedAccountStateChanges.TryRemove(accountId, out AccountStateContext _) ? 1 : 0);
      if (num == 0)
        return num != 0;
      this.eventAggregator.GetEvent<AccountStateEvent>().Publish(accountStateContext);
      return num != 0;
    }

    public async Task<TResult> InvokeWithAccountStateTrack<TResult>(
      string accountId,
      Func<Task<TResult>> actionWithResult)
    {
      return await this.InvokeTaskWithTracking<TResult>(accountId, actionWithResult: actionWithResult).ConfigureAwait(false);
    }

    private async Task<TResult> InvokeTaskWithTracking<TResult>(
      string accountId,
      Func<Task> action = null,
      Func<Task<TResult>> actionWithResult = null)
    {
      TResult result = default (TResult);
      if (string.IsNullOrEmpty(accountId))
        return result;
      try
      {
        bool flag = false;
        if (action != null)
        {
          await action().ConfigureAwait(false);
          flag = true;
        }
        if (actionWithResult != null)
        {
          result = await actionWithResult().ConfigureAwait(false);
          flag = true;
        }
        if (flag)
        {
          if (this.EnsureAccountStateReset(accountId, false))
            this.logger.WriteInfoEx("Previously detected lifecycle changes cleared on account " + accountId, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\AccountStateManager.cs", nameof (InvokeTaskWithTracking));
        }
      }
      catch (OktaWebException ex)
      {
        this.logger.WriteErrorEx(string.Format("API error code {0} detected on account {1}.", (object) ex.ApiErrorCode, (object) accountId), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\AccountStateManager.cs", nameof (InvokeTaskWithTracking));
        this.TrackAccountApiError(accountId, ex.ApiErrorCode);
      }
      return result;
    }
  }
}
