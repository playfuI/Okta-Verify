// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.UpdateWindowsHelloSettingsViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK.Extensions;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class UpdateWindowsHelloSettingsViewModel : BaseViewModel
  {
    private readonly IClientAccountManager accountManager;
    private readonly AccountStateContext authenticationContext;

    public UpdateWindowsHelloSettingsViewModel(AccountStateContext authContext)
    {
      this.authenticationContext = authContext;
      this.accountManager = AppInjector.Get<IClientAccountManager>();
      this.ConfirmUpdateSettingsCommand = (ICommand) new DelegateCommand(new Action(this.ConfirmUpdateSettings));
    }

    public ICommand ConfirmUpdateSettingsCommand { get; }

    private void ConfirmUpdateSettings()
    {
      ConfirmUpdateSettingsAsync().AsBackgroundTask("Confirm Win Hello update");

      async Task ConfirmUpdateSettingsAsync()
      {
        UpdateWindowsHelloSettingsViewModel settingsViewModel = this;
        IOktaAccount account;
        MainViewState payload;
        if (!settingsViewModel.accountManager.TryGetAccount(settingsViewModel.authenticationContext.AccountId, out account))
        {
          settingsViewModel.Logger.WriteWarningEx("No account found associated with " + settingsViewModel.authenticationContext.AccountId, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\UpdateWindowsHelloSettingsViewModel.cs", "ConfirmUpdateSettings");
          payload = new MainViewState(MainViewType.Accounts, (INotifyPropertyChanged) new AccountListViewModel());
        }
        else
          payload = await settingsViewModel.AnyOtherInvalidatedAccounts().ConfigureAwait(false) ? new MainViewState(MainViewType.Accounts, (INotifyPropertyChanged) new AccountListViewModel()) : new MainViewState(MainViewType.AccountDetails, (INotifyPropertyChanged) new AccountDetailsViewModel(account));
        settingsViewModel.Logger.WriteInfoEx(string.Format("Windows Hello settings update confirmed; UV key invalidated: {0}", (object) settingsViewModel.authenticationContext.IsUVKeyInvalidated), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\UpdateWindowsHelloSettingsViewModel.cs", "ConfirmUpdateSettings");
        settingsViewModel.EventAggregator.GetEvent<MainViewStateEvent>().Publish(payload);
        account = (IOktaAccount) null;
      }
    }

    private async Task<bool> AnyOtherInvalidatedAccounts()
    {
      UpdateWindowsHelloSettingsViewModel settingsViewModel = this;
      try
      {
        foreach (IOktaAccount account in settingsViewModel.accountManager.Accounts)
        {
          if (!(account.AccountId == settingsViewModel.authenticationContext.AccountId))
          {
            if (await settingsViewModel.accountManager.IsUserVerificationInvalidated(account).ConfigureAwait(false))
              return true;
          }
        }
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        settingsViewModel.Logger.WriteException("An error occurred while checking accounts state:", ex);
      }
      return false;
    }
  }
}
