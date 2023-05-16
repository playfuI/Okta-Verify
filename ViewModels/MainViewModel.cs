// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.MainViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Factories;
using Okta.Authenticator.NativeApp.RegistryWatcher;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UI.Enums;
using Okta.Authenticator.NativeApp.UI.Handlers;
using Okta.Authenticator.NativeApp.UI.ViewModels;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class MainViewModel : BaseViewModel
  {
    private const int ExtendedWindowHeight = 608;
    private const int DefaultWindowHeight = 568;
    private readonly IClientAccountManager accountManager;
    private readonly IApplicationStateMachine stateMachine;
    private readonly Task initializeTask;
    private readonly IApplicationHandler applicationHandler;
    private readonly IJumpListHandler jumpListHandler;
    private readonly IAnalyticsRepository analyticsRepository;
    private KeyValuePair<NavigationSection, NavigationItemViewModel> selectedNavigationItem;
    private Dictionary<NavigationSection, NavigationItemViewModel> navigationList;
    private Tuple<MainViewType, INotifyPropertyChanged> currentState;
    private int windowHeight = 568;

    public MainViewModel()
    {
      this.accountManager = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IClientAccountManager>();
      this.stateMachine = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IApplicationStateMachine>();
      this.applicationHandler = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IApplicationHandler>();
      this.jumpListHandler = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IJumpListHandler>();
      this.analyticsRepository = Okta.Authenticator.NativeApp.Injector.AppInjector.Get<IAnalyticsRepository>();
      this.BannerContext = new NotificationBannerViewModel();
      this.ActivateAppCommand = (ICommand) new DelegateCommand(new Action(this.OnActivateApp));
      this.AboutCommand = (ICommand) new DelegateCommand(new Action(this.applicationHandler.ShowAboutWindow));
      this.SettingsCommand = (ICommand) new DelegateCommand(new Action(this.OnTrayMenuSettingsClicked));
      this.SendFeedbackCommand = (ICommand) new DelegateCommand(new Action(this.applicationHandler.ShowReportIssueWindow));
      this.ExitCommand = (ICommand) new DelegateCommand(new Action(this.OnTrayMenuExitClick));
      this.NavigationSelectionChangedCommand = (ICommand) new DelegateCommand(new Action(this.OnNavigationSelectionChanged));
      this.InitializeNavigationPanel();
      this.initializeTask = this.Initialize();
    }

    public int WindowHeight
    {
      get => this.windowHeight;
      set
      {
        this.windowHeight = value;
        this.FirePropertyChangedEvent(nameof (WindowHeight));
      }
    }

    public bool ShowTopNavigation
    {
      get
      {
        if (this.NavigationList.Count > 1)
        {
          Tuple<MainViewType, INotifyPropertyChanged> currentState = this.CurrentState;
          if ((currentState != null ? (currentState.Item1 == MainViewType.BrowserRedirect ? 1 : 0) : 0) == 0)
            return true;
        }
        return false;
      }
    }

    public Dictionary<NavigationSection, NavigationItemViewModel> NavigationList
    {
      get => this.navigationList;
      set
      {
        this.navigationList = value;
        this.FirePropertyChangedEvent(nameof (NavigationList));
      }
    }

    public KeyValuePair<NavigationSection, NavigationItemViewModel> SelectedNavigationItem
    {
      get => this.selectedNavigationItem;
      set
      {
        this.selectedNavigationItem = value;
        this.FirePropertyChangedEvent(nameof (SelectedNavigationItem));
      }
    }

    public Tuple<MainViewType, INotifyPropertyChanged> CurrentState
    {
      get => this.currentState;
      private set
      {
        this.currentState = value;
        this.FirePropertyChangedEvent("ShowTopNavigation");
      }
    }

    public NotificationBannerViewModel BannerContext { get; }

    public bool CanSendFeedback
    {
      get
      {
        IAnalyticsRepository analyticsRepository = this.analyticsRepository;
        return analyticsRepository != null && analyticsRepository.CanReportIssue;
      }
    }

    public ICommand ActivateAppCommand { get; }

    public ICommand AboutCommand { get; }

    public ICommand SendFeedbackCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand ExitCommand { get; }

    public ICommand NavigationSelectionChangedCommand { get; }

    public void InitializeNavigationPanel()
    {
      Dictionary<NavigationSection, NavigationItemViewModel> dictionary = new Dictionary<NavigationSection, NavigationItemViewModel>()
      {
        {
          NavigationSection.Accounts,
          new NavigationItemViewModel()
          {
            Title = Resources.AccountsTextBlock
          }
        }
      };
      if (Okta.DeviceAccess.Windows.Injector.AppInjector.Initialized)
      {
        dictionary.Add(NavigationSection.DeviceAccess, new NavigationItemViewModel()
        {
          Title = Resources.DeviceAccessTextBlock
        });
        this.WindowHeight = 608;
      }
      this.NavigationList = dictionary;
    }

    internal void ResetViewState(bool preserveStateWhenIdentic = false)
    {
      MainViewType viewType = MainViewType.Onboarding;
      if (this.accountManager.AnyAccounts())
        viewType = MainViewType.Accounts;
      if (preserveStateWhenIdentic)
      {
        Tuple<MainViewType, INotifyPropertyChanged> currentState = this.CurrentState;
        if ((currentState != null ? (currentState.Item1 == viewType ? 1 : 0) : 0) != 0)
          return;
      }
      if (viewType == MainViewType.Accounts)
        this.UpdateCurrentState(viewType, (INotifyPropertyChanged) new AccountListViewModel());
      else
        this.UpdateCurrentState(viewType, (INotifyPropertyChanged) new OnboardingViewModel());
    }

    internal async Task ResetViewStateWithInitializationCheck()
    {
      await this.initializeTask.ConfigureAwait(false);
      this.ResetViewState(true);
    }

    internal void ShowBannerNotification(string message, BannerType messageType)
    {
      if (string.IsNullOrEmpty(message) || messageType == BannerType.Unknown)
        return;
      this.Logger.WriteInfoEx(string.Format("{0} - Notifying the user: {1}", (object) messageType, (object) message), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\MainViewModel.cs", nameof (ShowBannerNotification));
      this.BannerContext.ShowBanner(message, messageType);
    }

    private async Task Initialize()
    {
      MainViewModel mainViewModel = this;
      mainViewModel.Logger.WriteInfoEx("Initializing the main view...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\MainViewModel.cs", nameof (Initialize));
      mainViewModel.EventAggregator.GetEvent<OktaDeviceAccessRegistryChangedEvent>().Subscribe(new Action(mainViewModel.InitializeNavigationPanel));
      mainViewModel.EventAggregator.GetEvent<MainViewStateEvent>().Subscribe(new Action<MainViewState>(mainViewModel.OnViewStateUpdated));
      mainViewModel.EventAggregator.GetEvent<ViewStateRequestEvent>().Subscribe(new Action<ViewStateRequest>(mainViewModel.OnViewStateRequested));
      mainViewModel.EventAggregator.GetEvent<AppStateEvent>().Subscribe(new Action<AppState>(mainViewModel.OnAppStateUpdated));
      mainViewModel.EventAggregator.GetEvent<AccountEnrollEndEvent>().Subscribe(new Action<EnrollmentEndEventArg>(mainViewModel.OnEnrollUpdated));
      mainViewModel.EventAggregator.GetEvent<BannerNotificationEvent>().SubscribeToNotifications(new Action<BannerNotification>(mainViewModel.OnBannerNotificationRequested));
      mainViewModel.stateMachine.RegisterDeferral(ComputingStateType.Loading, new ComputingStateDeferral(mainViewModel.HandleStartupStateArgument), "Main view startup");
      await mainViewModel.accountManager.AccountsInitialization().ConfigureAwait(false);
      mainViewModel.ResetViewState();
      mainViewModel.CreateJumpList();
    }

    private void UpdateCurrentState(MainViewType viewType, INotifyPropertyChanged context) => this.applicationHandler.InvokeOnUIThread((Action) (() =>
    {
      this.SelectedNavigationItem = viewType != MainViewType.OfflineFactors ? new KeyValuePair<NavigationSection, NavigationItemViewModel>(NavigationSection.Accounts, this.NavigationList[NavigationSection.Accounts]) : new KeyValuePair<NavigationSection, NavigationItemViewModel>(NavigationSection.DeviceAccess, this.NavigationList[NavigationSection.DeviceAccess]);
      this.CurrentState = Tuple.Create<MainViewType, INotifyPropertyChanged>(viewType, context);
      this.FirePropertyChangedEvent("CurrentState");
    }));

    private void EndEnrollmentFlow(EnrollmentEndEventArg accountEnroll, BannerType bannerType)
    {
      this.ResetViewState();
      this.ShowBannerNotification(accountEnroll.EnrollMessage, bannerType);
    }

    private void OnViewStateUpdated(MainViewState viewState)
    {
      if (viewState.ViewType == MainViewType.Unknown)
        return;
      if (viewState.Activate)
      {
        this.UpdateCurrentState(viewState.ViewType, viewState.Context);
      }
      else
      {
        MainViewType? nullable = this.CurrentState?.Item1;
        MainViewType viewType = viewState.ViewType;
        if (nullable.GetValueOrDefault() == viewType & nullable.HasValue)
        {
          this.Logger.WriteInfoEx(string.Format("{0} deactivation", (object) viewState.ViewType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\MainViewModel.cs", nameof (OnViewStateUpdated));
          this.ResetViewState();
        }
        else
          this.Logger.WriteWarningEx(string.Format("The view associated with {0} is no longer active", (object) viewState.ViewType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\MainViewModel.cs", nameof (OnViewStateUpdated));
      }
    }

    private void OnViewStateRequested(ViewStateRequest viewState)
    {
      if (viewState.ViewType == MainViewType.Unknown)
        return;
      this.stateMachine.TransitionTo(AppStateRequestType.BringToFocus);
      INotifyPropertyChanged context = ViewModelFactory.BuildViewModel(viewState);
      if (context == null)
        return;
      this.UpdateCurrentState(viewState.ViewType, context);
    }

    private void OnAppStateUpdated(AppState updatedState)
    {
      if (updatedState.StateType != ComputingStateType.Resetting)
        return;
      this.ResetViewStateWithInitializationCheck().AsBackgroundTask(string.Format("App state {0} update", (object) updatedState.StateType));
    }

    private void OnEnrollUpdated(EnrollmentEndEventArg updatedEnroll)
    {
      this.Logger.WriteInfoEx(string.Format("Received account enrollment end event: {0}", (object) updatedEnroll.EnrollType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\MainViewModel.cs", nameof (OnEnrollUpdated));
      switch (updatedEnroll.EnrollType)
      {
        case EnrollEndEventType.AccountAdded:
          this.EndEnrollmentFlow(updatedEnroll, BannerType.Success);
          break;
        case EnrollEndEventType.EnrollmentFailed:
        case EnrollEndEventType.EnrollmentCancelled:
          this.EndEnrollmentFlow(updatedEnroll, BannerType.Error);
          break;
        default:
          this.Logger.WriteWarningEx(string.Format("{0} won't be processed", (object) updatedEnroll.EnrollType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\MainViewModel.cs", nameof (OnEnrollUpdated));
          break;
      }
    }

    private async Task HandleStartupStateArgument(ComputingStateContext context)
    {
      MainViewModel mainViewModel = this;
      switch (context.Command)
      {
        case StartupArgumentType.ShowReportIssue:
          if (!mainViewModel.CanSendFeedback)
            break;
          await mainViewModel.initializeTask.ConfigureAwait(false);
          // ISSUE: reference to a compiler-generated method
          mainViewModel.applicationHandler.InvokeOnUIThread(new Action(mainViewModel.\u003CHandleStartupStateArgument\u003Eb__61_1));
          break;
        case StartupArgumentType.ShowAbout:
          await mainViewModel.initializeTask.ConfigureAwait(false);
          // ISSUE: reference to a compiler-generated method
          mainViewModel.applicationHandler.InvokeOnUIThread(new Action(mainViewModel.\u003CHandleStartupStateArgument\u003Eb__61_0));
          break;
        case StartupArgumentType.ShowSettings:
          await mainViewModel.initializeTask.ConfigureAwait(false);
          mainViewModel.OnTrayMenuSettingsClicked();
          break;
      }
    }

    private void OnActivateApp() => this.stateMachine.TransitionTo(AppStateRequestType.Activate);

    private void OnTrayMenuSettingsClicked() => this.applicationHandler.InvokeOnUIThread((Action) (() => this.applicationHandler.ShowSettings()));

    private void OnTrayMenuExitClick() => this.stateMachine.TransitionTo(ComputingStateType.ShuttingDown);

    private void OnNavigationSelectionChanged()
    {
      if (this.SelectedNavigationItem.Key != NavigationSection.DeviceAccess)
        Okta.DeviceAccess.Windows.UI.ViewModels.MainViewModel.Instance?.Dispose();
      switch (this.SelectedNavigationItem.Key)
      {
        case NavigationSection.Accounts:
          this.ResetViewState();
          break;
        case NavigationSection.DeviceAccess:
          this.OnViewStateUpdated(new MainViewState(MainViewType.OfflineFactors));
          break;
      }
    }

    private void OnBannerNotificationRequested(BannerNotification notification) => this.ShowBannerNotification(notification.Message, notification.Banner);

    private void CreateJumpList() => this.jumpListHandler.Initialize(this.CanSendFeedback);
  }
}
