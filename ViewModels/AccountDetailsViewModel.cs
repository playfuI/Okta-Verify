// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AccountDetailsViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Enums;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AccountDetailsViewModel : AccountOverviewViewModel
  {
    private (string Message, bool IsCritical) accountWarning;
    private (bool Disabled, string Reason) winHelloSettingsChange;
    private (bool IsDefault, string Label, string Description) accountSetAsDefault;
    private string enableUVText;
    private string disableUVText;

    public AccountDetailsViewModel(IOktaAccount account)
      : base(account)
    {
      this.Initialize().AsBackgroundTask("Account details initialization");
    }

    public ICommand CloseAccountDetailsCommand { get; private set; }

    public ICommand UpdateUserVerificationCommand { get; private set; }

    public ICommand ShowRemoveAccountDialogCommand { get; private set; }

    public ICommand DismissWarningCommand { get; private set; }

    public ICommand ReenrollAccountCommand { get; private set; }

    public ICommand SetAsDefaultAccountCommand { get; private set; }

    public bool WinHelloSettingsChangeDisabled => this.winHelloSettingsChange.Disabled;

    public string WinHelloSettingsChangeDisabledReason => this.winHelloSettingsChange.Reason;

    public string AccountWarningMessage => this.accountWarning.Message;

    public bool IsAccountWarningCritical => this.accountWarning.IsCritical;

    public string InlineVerificationMethodText { get; private set; }

    public bool WindowsHelloSupported { get; private set; }

    public bool IsAccountDefault => this.accountSetAsDefault.IsDefault;

    public string DefaultAccountSettingText => this.accountSetAsDefault.Label;

    public string DefaultAccountSettingDescription => this.accountSetAsDefault.Description;

    public bool AnyOtherAccountsOnCurrentOrg { get; private set; }

    protected override bool UseCache => false;

    internal override async Task UpdateAccountState()
    {
      AccountDetailsViewModel detailsViewModel = this;
      // ISSUE: reference to a compiler-generated method
      await detailsViewModel.\u003C\u003En__0().ConfigureAwait(true);
      detailsViewModel.UpdateUserVerificationDetails();
      detailsViewModel.accountWarning = detailsViewModel.GetWarningFromAccountState();
      detailsViewModel.ShouldShowWarning = !string.IsNullOrEmpty(detailsViewModel.AccountWarningMessage);
      detailsViewModel.FireViewModelChangedEvent();
    }

    protected override void UpdateAccountState(AccountStateContext updatedState)
    {
      base.UpdateAccountState(updatedState);
      this.accountWarning = this.GetWarningFromAccountState();
      this.FirePropertyChangedEvent("AccountWarningMessage");
      this.FirePropertyChangedEvent("IsAccountWarningCritical");
    }

    private async Task Initialize()
    {
      AccountDetailsViewModel detailsViewModel = this;
      detailsViewModel.InitializeLocalState();
      await detailsViewModel.UpdateAccountState().ConfigureAwait(true);
      detailsViewModel.FireViewModelChangedEvent();
    }

    private void UpdateUserVerificationDetails()
    {
      switch (this.AccountState)
      {
        case AccountState.UVNeeded:
        case AccountState.UVReenableRequired:
          this.winHelloSettingsChange = (false, string.Empty);
          this.InlineVerificationMethodText = this.enableUVText;
          break;
        case AccountState.UVToRemainEnabled:
          this.winHelloSettingsChange = (true, this.WindowsHelloSupported ? Resources.ExtraVerificationOrgRequiresWindowsHelloRemainsEnabled : Resources.ExtraVerificationOrgRequiresPasscodeRemainsEnabled);
          this.InlineVerificationMethodText = this.disableUVText;
          break;
        case AccountState.UVDisabledWhileRemote:
          this.winHelloSettingsChange = (true, this.AccountModel.IsUserVerificationEnabled ? Resources.WindowsHelloCantDisableWhileRemoteText : Resources.WindowsHelloCantEnableWhileRemoteText);
          this.InlineVerificationMethodText = this.AccountModel.IsUserVerificationEnabled ? this.disableUVText : this.enableUVText;
          break;
        case AccountState.UVNotSupported:
          this.winHelloSettingsChange = (true, string.Empty);
          this.InlineVerificationMethodText = this.AccountModel.IsUserVerificationEnabled ? this.disableUVText : this.enableUVText;
          break;
        case AccountState.UVReenablePreferred:
          this.winHelloSettingsChange = (false, string.Empty);
          this.InlineVerificationMethodText = this.enableUVText;
          break;
        default:
          this.winHelloSettingsChange = (false, string.Empty);
          this.InlineVerificationMethodText = this.AccountModel.IsUserVerificationEnabled ? this.disableUVText : this.enableUVText;
          break;
      }
      this.FirePropertyChangedEvent("InlineVerificationMethodText");
      this.FirePropertyChangedEvent("WinHelloSettingsChangeDisabled");
      this.FirePropertyChangedEvent("WinHelloSettingsChangeDisabledReason");
    }

    private void InitializeLocalState()
    {
      this.CloseAccountDetailsCommand = (ICommand) new DelegateCommand(new Action(this.CloseAccountDetails));
      this.UpdateUserVerificationCommand = (ICommand) new DelegateCommand(new Action(this.InitiateAccountVerificationUpdateFlow));
      this.ShowRemoveAccountDialogCommand = (ICommand) new DelegateCommand(new Action(this.ShowRemoveAccountDialog));
      this.ReenrollAccountCommand = (ICommand) new DelegateCommand(new Action(this.ReenrollAccount));
      this.DismissWarningCommand = (ICommand) new DelegateCommand(new Action(this.DismissWarning));
      this.SetAsDefaultAccountCommand = (ICommand) new DelegateCommand(new Action(this.SetAsDefaultAccount));
      this.WindowsHelloSupported = this.SignInManager.CanSignInWithWindowsHello;
      if (this.WindowsHelloSupported)
      {
        this.disableUVText = Resources.UserVerificationDisableWindowsHello;
        this.enableUVText = Resources.UserVerificationEnableWindowsHello;
      }
      else
      {
        this.disableUVText = Resources.UserVerificationDisablePasscode;
        this.enableUVText = Resources.UserVerificationEnablePasscode;
      }
      this.InlineVerificationMethodText = this.AccountModel.IsUserVerificationEnabled ? this.disableUVText : this.enableUVText;
      (bool MultiAccountsEnrolled, bool IsCurrentSetAsDefault) defaultSettings = this.GetDefaultSettings();
      this.AnyOtherAccountsOnCurrentOrg = defaultSettings.MultiAccountsEnrolled;
      if (defaultSettings.IsCurrentSetAsDefault)
      {
        this.accountSetAsDefault = (true, ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsIsSetAsDefaultLabel), ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsIsSetAsDefaultDescription, this.AccountModel.Domain));
      }
      else
      {
        (bool, string, string) valueTuple;
        if (defaultSettings.MultiAccountsEnrolled)
          valueTuple = (false, ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsSetAsDefaultLabel), ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsSetAsDefaultDescription, this.AccountModel.Domain));
        else
          valueTuple = (false, string.Empty, string.Empty);
        this.accountSetAsDefault = valueTuple;
      }
    }

    private (bool MultiAccountsEnrolled, bool IsCurrentSetAsDefault) GetDefaultSettings() => this.AccountManager.Accounts.Count<IOktaAccount>((Func<IOktaAccount, bool>) (a => a.OrgId == this.AccountModel.OrgId)) < 2 ? (false, false) : (true, this.AccountManager.IsAccountSetAsDefault(this.AccountModel.AccountId));

    private void SetAsDefaultAccount() => this.UpdateDefaultAccountChange().AsBackgroundTask("Default account update");

    private async Task UpdateDefaultAccountChange()
    {
      AccountDetailsViewModel detailsViewModel = this;
      if (detailsViewModel.IsAccountDefault)
        return;
      int num = await detailsViewModel.AccountManager.UpdateAccountDefaultSettings(detailsViewModel.AccountModel.AccountId, true).ConfigureAwait(false) ? 1 : 0;
      BannerNotificationEvent notificationEvent = detailsViewModel.EventAggregator.GetEvent<BannerNotificationEvent>();
      if (num == 0)
      {
        notificationEvent.Publish(new BannerNotification(BannerType.Error, Resources.ErrorMessageGeneric));
      }
      else
      {
        notificationEvent.Publish(new BannerNotification(BannerType.Success, ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsSetAsDefaultSucceededText, detailsViewModel.AccountModel.Domain)));
        detailsViewModel.accountSetAsDefault = (true, ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsIsSetAsDefaultLabel), ResourceExtensions.ExtractCompositeResource(Resources.AccountDetailsIsSetAsDefaultDescription, detailsViewModel.AccountModel.Domain));
        detailsViewModel.FirePropertyChangedEvent("IsAccountDefault");
        detailsViewModel.FirePropertyChangedEvent("DefaultAccountSettingText");
        detailsViewModel.FirePropertyChangedEvent("DefaultAccountSettingDescription");
      }
    }

    private void ReenrollAccount() => this.InitiateReEnrollFlow().AsBackgroundTask("Re-enrolling...");

    private void CloseAccountDetails() => this.EventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.AccountDetails, (INotifyPropertyChanged) this, false));

    private void InitiateAccountVerificationUpdateFlow()
    {
      bool? nullable = new bool?();
      switch (this.AccountState)
      {
        case AccountState.UVNeeded:
        case AccountState.UVReenablePreferred:
        case AccountState.UVReenableRequired:
          nullable = new bool?(true);
          break;
        case AccountState.UVToRemainEnabled:
          this.Logger.WriteInfoEx("Org. requires UV to remain enabled; ignoring request...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\AccountDetailsViewModel.cs", nameof (InitiateAccountVerificationUpdateFlow));
          break;
        default:
          nullable = new bool?(!this.AccountModel.IsUserVerificationEnabled);
          break;
      }
      if (!nullable.HasValue)
        return;
      this.UpdateAccountUserVerification(nullable.Value).AsBackgroundTask("User verification update");
    }

    private async Task UpdateAccountUserVerification(bool enableUverVerification)
    {
      AccountDetailsViewModel detailsViewModel = this;
      bool flag;
      if (enableUverVerification)
        flag = true;
      else
        flag = await detailsViewModel.EventAggregator.GetEvent<DialogViewStateEvent>().RequestDialogDisplayWithResult<IOktaAccount>(detailsViewModel.EventAggregator, DialogViewType.DisableVerificationConfirmation, detailsViewModel.AccountModel).ConfigureAwait(false);
      EnrollmentEndEventArg enrollResult;
      if (!flag)
      {
        enrollResult = new EnrollmentEndEventArg();
      }
      else
      {
        enrollResult = new EnrollmentEndEventArg();
        try
        {
          EnrollmentStartEventArg enrollmentArgs = EnrollmentStartEventArg.AsUserVerificationEnrollmentRequest(enableUverVerification ? EnrollStartEventType.EnableUserVerification : EnrollStartEventType.DisableUserVerification, detailsViewModel.AccountModel);
          enrollResult = await detailsViewModel.AccountManager.InvokeEnrollmentFlow(enrollmentArgs).ConfigureAwait(false);
          enrollResult = new EnrollmentEndEventArg();
        }
        catch (Exception ex) when (!ex.IsCritical())
        {
          detailsViewModel.Logger.LogAndReportToAnalytics("Failed to update the account user verification", ex, detailsViewModel.AnalyticsProvider);
          enrollResult = new EnrollmentEndEventArg();
        }
        finally
        {
          detailsViewModel.RefreshAndNotifyUpdateState(enrollResult);
        }
      }
    }

    private async Task InitiateAccountRemovalFlow()
    {
      AccountDetailsViewModel detailsViewModel = this;
      bool removalConfirmed = false;
      string innerNotificationMessage = (string) null;
      AuthenticatorOperationResult removalResult = AuthenticatorOperationResult.Unknown;
      detailsViewModel.EventAggregator.GetEvent<BannerNotificationEvent>().SubscribeToNotifications((Action<BannerNotification>) (notificaiton => innerNotificationMessage = notificaiton.Message));
      object obj = (object) null;
      int num = 0;
      try
      {
        try
        {
          removalConfirmed = await detailsViewModel.EventAggregator.GetEvent<DialogViewStateEvent>().RequestDialogDisplayWithResult<IOktaAccount>(detailsViewModel.EventAggregator, DialogViewType.RemoveAccountConfirmation, detailsViewModel.AccountModel).ConfigureAwait(false);
          if (removalConfirmed)
          {
            removalResult = await detailsViewModel.AccountManager.DeleteAccount(detailsViewModel.AccountModel).ConfigureAwait(false);
            goto label_10;
          }
        }
        catch (Exception ex) when (!ex.IsCritical())
        {
          detailsViewModel.Logger.LogAndReportToAnalytics("Failed to remove the account", ex, detailsViewModel.AnalyticsProvider);
          goto label_10;
        }
        num = 1;
      }
      catch (object ex)
      {
        obj = ex;
      }
label_10:
      switch (removalResult)
      {
        case AuthenticatorOperationResult.Success:
          detailsViewModel.CloseAccountDetails();
          break;
        case AuthenticatorOperationResult.Cancelled:
          detailsViewModel.RefreshAccountUpdateState(false, string.Empty, notifyUpdate: false);
          break;
        default:
          if (removalConfirmed)
          {
            await Task.Delay(50).ConfigureAwait(false);
            detailsViewModel.RefreshAccountUpdateState(false, innerNotificationMessage ?? Resources.ErrorMessageGeneric);
            detailsViewModel.CloseAccountDetails();
            break;
          }
          break;
      }
      object obj1 = obj;
      if (obj1 != null)
      {
        if (!(obj1 is Exception source))
          throw obj1;
        ExceptionDispatchInfo.Capture(source).Throw();
      }
      if (num == 1)
        ;
      else
        obj = (object) null;
    }

    private void RefreshAndNotifyUpdateState(EnrollmentEndEventArg enrollResult)
    {
      switch (enrollResult.EnrollType)
      {
        case EnrollEndEventType.AccountUpdated:
          IOktaAccount account;
          if (this.AccountManager.TryGetAccount(enrollResult.AccountId, out account))
          {
            this.RefreshAccountUpdateState(true, enrollResult.EnrollMessage, account);
            break;
          }
          this.Logger.WriteWarningEx("Received an out-of-sync update; account associated with " + enrollResult.AccountId + " not found", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\AccountDetailsViewModel.cs", nameof (RefreshAndNotifyUpdateState));
          this.RefreshAccountUpdateState(false, enrollResult.EnrollMessage);
          break;
        case EnrollEndEventType.AccountUpdateCancelled:
          this.RefreshAccountUpdateState(false, enrollResult.EnrollMessage, notifyUpdate: false);
          break;
        default:
          this.RefreshAccountUpdateState(false, enrollResult.EnrollMessage);
          break;
      }
    }

    private void RefreshAccountUpdateState(
      bool suceeded,
      string enrollMessage,
      IOktaAccount updatedAccount = null,
      bool notifyUpdate = true)
    {
      AccountDetailsViewModel context = this;
      BannerType type = BannerType.Error;
      string message = enrollMessage;
      if (suceeded)
      {
        context = new AccountDetailsViewModel(updatedAccount ?? this.AccountModel);
        type = BannerType.Success;
      }
      else if (string.IsNullOrEmpty(message))
        message = Resources.ErrorMessageGeneric;
      this.EventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.AccountDetails, (INotifyPropertyChanged) context));
      if (!notifyUpdate)
        return;
      this.EventAggregator.GetEvent<BannerNotificationEvent>().Publish(new BannerNotification(type, message));
    }

    private void ShowRemoveAccountDialog() => this.InitiateAccountRemovalFlow().AsBackgroundTask("Removing account...");

    private void DismissWarning()
    {
      if (this.IsAccountWarningCritical)
      {
        this.Logger.WriteInfoEx("Critical warnings will not be dismissed", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\AccountDetailsViewModel.cs", nameof (DismissWarning));
      }
      else
      {
        this.ShouldShowWarning = false;
        this.FirePropertyChangedEvent("ShouldShowWarning");
        this.AccountManager.UpdateAccountWarningSettings(this.AccountModel?.AccountId, AccountErrorStateTypes.UserVerificationKeyDoesNotExist).AsBackgroundTask("Updating account warning settings");
      }
    }

    private (string Message, bool IsCritical) GetAccountErrorStateMessage(
      AccountErrorStateTypes messageType)
    {
      (bool Dismissed, bool Critical) = this.AccountManager.GetAccountErrorState(this.AccountModel?.AccountId, messageType);
      if (Dismissed)
        return (string.Empty, Critical);
      switch (messageType)
      {
        case AccountErrorStateTypes.UserVerificationKeyDoesNotExist:
          return (Resources.AccountDetailsWinHelloChangedClickEnable, Critical);
        case AccountErrorStateTypes.UserVerificationRequiredByServer:
          return (this.WindowsHelloSupported ? Resources.ExtraVerificationWindowsHelloRequiredClickEnable : Resources.ExtraVerificationPasscodeRequiredClickEnable, Critical);
        case AccountErrorStateTypes.AccountReenrollmentRequired:
          return (this.AccountState == AccountState.MFAResetReenrollNeeded ? ResourceExtensions.ExtractCompositeResource(Resources.AccountMFAResetReenrollMessage) : ResourceExtensions.ExtractCompositeResource(Resources.AccountInvalidatedReenrollMessage), Critical);
        case AccountErrorStateTypes.AccountInvalidated:
          return (ResourceExtensions.ExtractCompositeResource(Resources.AccountInvalidatedContactAdminMessage), Critical);
        default:
          return (string.Empty, Critical);
      }
    }

    private (string Message, bool IsCritical) GetWarningFromAccountState()
    {
      switch (this.AccountState)
      {
        case AccountState.UVNeeded:
        case AccountState.UVReenableRequired:
          return this.GetAccountErrorStateMessage(AccountErrorStateTypes.UserVerificationRequiredByServer);
        case AccountState.UVReenablePreferred:
          return this.GetAccountErrorStateMessage(AccountErrorStateTypes.UserVerificationKeyDoesNotExist);
        case AccountState.AccountInvalidated:
          return this.GetAccountErrorStateMessage(AccountErrorStateTypes.AccountInvalidated);
        case AccountState.AccountInvalidatedReenrollNeeded:
        case AccountState.MFAResetReenrollNeeded:
          return this.GetAccountErrorStateMessage(AccountErrorStateTypes.AccountReenrollmentRequired);
        default:
          return (string.Empty, false);
      }
    }

    private async Task InitiateReEnrollFlow()
    {
      AccountDetailsViewModel detailsViewModel = this;
      EnrollmentEndEventArg enrollResult = new EnrollmentEndEventArg();
      try
      {
        detailsViewModel.Logger.WriteInfoEx("Initiating re-enrollment for " + detailsViewModel.AccountModel.AccountId + "...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\AccountDetailsViewModel.cs", nameof (InitiateReEnrollFlow));
        EnrollmentStartEventArg enrollmentArgs = EnrollmentStartEventArg.AsReEnrollmentRequest(detailsViewModel.AccountModel);
        enrollResult = await detailsViewModel.AccountManager.InvokeEnrollmentFlow(enrollmentArgs).ConfigureAwait(false);
        enrollResult = new EnrollmentEndEventArg();
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        detailsViewModel.Logger.LogAndReportToAnalytics("Failed to re-enroll the account", ex, detailsViewModel.AnalyticsProvider);
        enrollResult = new EnrollmentEndEventArg();
      }
      finally
      {
        if (enrollResult.EnrollType != EnrollEndEventType.AccountAdded)
          detailsViewModel.RefreshAccountUpdateState(false, enrollResult.EnrollMessage);
      }
    }
  }
}
