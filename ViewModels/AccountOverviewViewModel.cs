// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AccountOverviewViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInfo;
using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Enums;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Prism.Events;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AccountOverviewViewModel : BaseViewModel
  {
    private readonly IApplicationInfoManager applicationInfoManager;
    private readonly IAccountStateManager accountStateManager;

    public AccountOverviewViewModel(IOktaAccount account)
    {
      this.AccountModel = account;
      this.SignInManager = AppInjector.Get<IClientSignInManager>();
      this.applicationInfoManager = AppInjector.Get<IApplicationInfoManager>();
      this.AccountManager = AppInjector.Get<IClientAccountManager>();
      this.accountStateManager = AppInjector.Get<IAccountStateManager>();
      this.RegisterEvents();
    }

    public IOktaAccount AccountModel { get; }

    public AccountState AccountState { get; private set; }

    public bool ShouldShowWarning { get; protected set; }

    protected IClientSignInManager SignInManager { get; }

    protected IClientAccountManager AccountManager { get; }

    protected virtual bool UseCache => true;

    internal virtual async Task UpdateAccountState()
    {
      AccountOverviewViewModel overviewViewModel = this;
      try
      {
        (overviewViewModel.AccountState, overviewViewModel.ShouldShowWarning) = await overviewViewModel.DetermineStateFromUserVerificationStatus().ConfigureAwait(true);
        AccountLifecycleEventType detectedLifecycleState = overviewViewModel.accountStateManager.DetectedLifecycleChange(overviewViewModel.AccountModel.AccountId);
        if (detectedLifecycleState != AccountLifecycleEventType.None)
          (overviewViewModel.AccountState, overviewViewModel.ShouldShowWarning) = overviewViewModel.DetermineStateFromLifecycleChanges(detectedLifecycleState);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        overviewViewModel.Logger.WriteException("Failed to update the account state", ex);
      }
      overviewViewModel.FirePropertyChangedEvent("AccountState");
      overviewViewModel.FirePropertyChangedEvent("ShouldShowWarning");
    }

    protected virtual void UpdateAccountState(AccountStateContext updatedState)
    {
      if (updatedState == null)
        return;
      (this.AccountState, this.ShouldShowWarning) = this.DetermineStateFromLifecycleChanges(updatedState.LifecycleChange);
      AccountState accountState;
      bool flag;
      if (this.AccountState == AccountState.CanBeUpdated)
      {
        if (!updatedState.IsUVKeyInvalidated || !updatedState.IsPoPKeyInvalidated)
        {
          this.AccountState = accountState = AccountState.CanBeUpdated;
          this.ShouldShowWarning = flag = false;
        }
        else
        {
          this.AccountState = accountState = AccountState.AccountInvalidatedReenrollNeeded;
          this.ShouldShowWarning = flag = true;
        }
      }
      this.FirePropertyChangedEvent("AccountState");
      this.FirePropertyChangedEvent("ShouldShowWarning");
    }

    private async Task<(AccountState State, bool AnyWarnings)> DetermineStateFromUserVerificationStatus()
    {
      if (this.accountStateManager.AreAccountKeysInvalidated(this.AccountModel.AccountId))
        return (AccountState.AccountInvalidatedReenrollNeeded, true);
      if (this.applicationInfoManager.CheckIfInRemoteSession())
        return (AccountState.UVDisabledWhileRemote, false);
      if (!this.SignInManager.CanSignInWithWindowsHello)
        return (AccountState.UVNotSupported, false);
      bool uvInvalidated = await this.AccountManager.IsUserVerificationInvalidated(this.AccountModel).ConfigureAwait(true);
      bool flag = await this.AccountManager.IsAccountUserVerificationRequired(this.AccountModel, this.UseCache).ConfigureAwait(true);
      return !uvInvalidated ? (!flag ? (AccountState.CanBeUpdated, false) : (this.AccountModel.IsUserVerificationEnabled ? (AccountState.UVToRemainEnabled, false) : (AccountState.UVNeeded, true))) : (flag ? (AccountState.UVReenableRequired, true) : (AccountState.UVReenablePreferred, true));
    }

    private (AccountState State, bool AnyWarnings) DetermineStateFromLifecycleChanges(
      AccountLifecycleEventType detectedLifecycleState)
    {
      switch (detectedLifecycleState)
      {
        case AccountLifecycleEventType.DeviceDeactivated:
        case AccountLifecycleEventType.EnrollmentInvalidated:
        case AccountLifecycleEventType.UserDeactivated:
        case AccountLifecycleEventType.UserDeleted:
          return (AccountState.AccountInvalidated, true);
        case AccountLifecycleEventType.DeviceDeleted:
          return (AccountState.AccountInvalidatedReenrollNeeded, true);
        case AccountLifecycleEventType.EnrollmentReset:
          return (AccountState.MFAResetReenrollNeeded, true);
        default:
          return (AccountState.CanBeUpdated, false);
      }
    }

    private void RegisterEvents() => this.EventAggregator.GetEvent<AccountStateEvent>().Subscribe(new Action<AccountStateContext>(this.OnAccountStateUpdated), ThreadOption.PublisherThread, false, (Predicate<AccountStateContext>) (state => state.AccountId == this.AccountModel.AccountId));

    private void OnAccountStateUpdated(AccountStateContext updatedState)
    {
      if (updatedState?.AccountId != this.AccountModel.AccountId)
        return;
      this.ApplicationHandler.InvokeOnUIThread((Action) (() => this.UpdateAccountState(updatedState)));
    }
  }
}
