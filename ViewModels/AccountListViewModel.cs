// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AccountListViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK.Extensions;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AccountListViewModel : BaseViewModel
  {
    private readonly IClientAccountManager clientAccountManager;
    private readonly IDeviceConfigurationManager deviceConfigurationManager;
    private IAnalyticsRepository analyticsRepository;
    private readonly IEventAggregator eventAggregator;
    private ICommand addAccountCommand;
    private AccountOverviewViewModel selectedAccount;
    private DeviceHealthViewModel deviceHealthViewModel;
    private bool deviceHealth;
    private bool shieldIconVisibility;

    public AccountListViewModel()
    {
      this.clientAccountManager = AppInjector.Get<IClientAccountManager>();
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.deviceConfigurationManager = AppInjector.Get<IDeviceConfigurationManager>();
      this.analyticsRepository = AppInjector.Get<IAnalyticsRepository>();
      this.OpenSettingsCommand = (ICommand) new DelegateCommand(new Action(this.OpenSettings));
      this.Accounts = new ObservableCollection<AccountOverviewViewModel>();
      this.shieldIconVisibility = this.deviceConfigurationManager.IsDeviceHealthCheckEnabled;
      if (this.shieldIconVisibility)
      {
        this.ShieldIconVisibility = false;
        this.DeviceHealthCommand = (ICommand) new DelegateCommand(new Action(this.ViewDeviceHealth));
        this.deviceHealthViewModel = new DeviceHealthViewModel(true);
        this.InitializeDeviceViewModelTask = this.deviceHealthViewModel.InitializationTask.ContinueWith((Action<Task>) (t => this.SetDeviceHealthCheck()));
        this.eventAggregator.GetEvent<AppStateRequestEvent>().Subscribe(new Action<AppStateRequest>(this.OnAppStateUpdated));
      }
      this.Initialize();
    }

    public ObservableCollection<AccountOverviewViewModel> Accounts { get; }

    public ICommand AddAccountCommand
    {
      get
      {
        if (this.addAccountCommand == null)
          this.addAccountCommand = (ICommand) new DelegateCommand(new Action(this.AddAccount));
        return this.addAccountCommand;
      }
    }

    public ICommand OpenSettingsCommand { get; }

    public ICommand DeviceHealthCommand { get; }

    public bool DeviceHealthy => this.deviceHealth;

    public AccountOverviewViewModel SelectedAccount
    {
      get => this.selectedAccount;
      set
      {
        this.ShowAccountDetails(value);
        this.FirePropertyChangedEvent(nameof (SelectedAccount));
      }
    }

    public bool ShieldIconVisibility { get; private set; }

    internal Task InitializeDeviceViewModelTask { get; private set; }

    private void Initialize()
    {
      this.eventAggregator.GetEvent<AccountListEvent>().Subscribe(new Action<AccountListChange>(this.OnAccountCollectionChanged));
      this.LoadAccounts().AsBackgroundTask("loading accounts to view model");
    }

    private void AddAccount() => this.eventAggregator.GetEvent<AccountEnrollStartEvent>()?.Publish(EnrollmentStartEventArg.AsManualEnrollmentRequest());

    private void OpenSettings() => this.eventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.Settings, (INotifyPropertyChanged) new SettingsViewModel()));

    private void ViewDeviceHealth()
    {
      this.eventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.DeviceHealth, (INotifyPropertyChanged) this.deviceHealthViewModel));
      this.analyticsRepository?.TrackEvent("ShowDeviceHealthScreenFromAcctListView");
    }

    private void ShowAccountDetails(AccountOverviewViewModel accountOverview)
    {
      if (this.selectedAccount?.AccountModel.AccountId == accountOverview?.AccountModel.AccountId)
        return;
      this.selectedAccount = accountOverview;
      if (this.selectedAccount == null)
        return;
      this.eventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.AccountDetails, (INotifyPropertyChanged) new AccountDetailsViewModel(this.selectedAccount.AccountModel)));
    }

    private async Task LoadAccounts()
    {
      AccountListViewModel accountListViewModel = this;
      // ISSUE: reference to a compiler-generated method
      await accountListViewModel.ApplicationHandler.InvokeOnUIThreadAsync(new Func<Task>(accountListViewModel.\u003CLoadAccounts\u003Eb__39_0)).ConfigureAwait(false);
    }

    private async Task UpdatePartialListState(int maxAccounts = 5)
    {
      int count = Math.Min(this.Accounts.Count, maxAccounts);
      for (int i = 0; i < count; ++i)
        await this.Accounts[i].UpdateAccountState().ConfigureAwait(false);
    }

    private void UpdateAccount(IOktaAccount account)
    {
      if (account == null)
        return;
      for (int index = 0; index < this.Accounts.Count; ++index)
      {
        if (this.Accounts[index].AccountModel.AccountId == account.AccountId)
        {
          this.Accounts[index] = new AccountOverviewViewModel(account);
          break;
        }
      }
    }

    private void RemoveAccount(IOktaAccount account)
    {
      if (string.IsNullOrEmpty(account?.AccountId))
        return;
      for (int index = 0; index < this.Accounts.Count; ++index)
      {
        if (this.Accounts[index].AccountModel.AccountId == account.AccountId)
        {
          this.Accounts.RemoveAt(index);
          break;
        }
      }
    }

    private void EnsureViewStateUpdated()
    {
      if (this.Accounts.Count != 0)
        return;
      MainViewState payload = new MainViewState(MainViewType.Accounts, activate: false);
      this.EventAggregator.GetEvent<MainViewStateEvent>().Publish(payload);
    }

    private void OnAccountCollectionChanged(AccountListChange collectionChange) => this.ApplicationHandler.InvokeOnUIThread((Action) (() =>
    {
      try
      {
        switch (collectionChange.ChangeType)
        {
          case ListChangeType.Added:
            this.Accounts.Add(new AccountOverviewViewModel(collectionChange.Account));
            break;
          case ListChangeType.Updated:
            this.UpdateAccount(collectionChange.Account);
            break;
          case ListChangeType.Removed:
            this.RemoveAccount(collectionChange.Account);
            this.EnsureViewStateUpdated();
            break;
          case ListChangeType.Cleared:
            this.Accounts.Clear();
            this.EnsureViewStateUpdated();
            break;
          default:
            this.Logger.WriteInfoEx(string.Format("Collection change event {0} not supported; ignoring it...", (object) collectionChange.ChangeType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\AccountListViewModel.cs", nameof (OnAccountCollectionChanged));
            break;
        }
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.Logger.LogAndReportToAnalytics("An error occurred while updating the accounts collection", ex, this.AnalyticsProvider);
      }
    }));

    private void SetDeviceHealthCheck()
    {
      this.ShieldIconVisibility = this.shieldIconVisibility;
      this.deviceHealth = this.deviceHealthViewModel.IsOverallStatusHealthy;
      this.FireViewModelChangedEvent();
    }

    private void OnAppStateUpdated(AppStateRequest newState) => this.ApplicationHandler.InvokeOnUIThread((Action) (() =>
    {
      try
      {
        if (newState.State != ApplicationStateType.NormalInFocus)
          return;
        this.deviceHealthViewModel.InitializeHealthState().ContinueWith((Action<Task>) (t => this.SetDeviceHealthCheck()));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.Logger.LogAndReportToAnalytics("An error occurred while updating the device health", ex, this.AnalyticsProvider);
      }
    }));
  }
}
