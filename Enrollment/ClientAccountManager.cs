// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.ClientAccountManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.DeviceAccess.Windows.FactorEnrollment;
using Okta.DeviceAccess.Windows.FactorEnrollment.Models;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authenticator.Entities;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Serialization;
using Okta.Devices.SDK.WebClient;
using Okta.Devices.SDK.Windows.Extensions;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public class ClientAccountManager : IClientAccountManager, IDisposable
  {
    private readonly ILogger logger;
    private readonly IClientStorageManager storageManager;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IEventAggregator eventAggregator;
    private readonly IConfigurationManager configManager;
    private readonly IFeatureSettings featureSettings;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IAccountStateManager accountStateManager;
    private readonly IPublicKeyList publicKeyList;
    private readonly ConcurrentDictionary<string, IOktaAccount> accounts;
    private readonly TaskCompletionSource<bool> accountsInitializationSource;
    private readonly ConcurrentDictionary<string, UvRequirementType> uvRequirements;
    private readonly ConcurrentDictionary<string, AccountSettingsModel> accountsSettings;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<EnrollmentEndEventArg>> pendingEnrollTasks;
    private readonly bool uvRestrictionsOverridden;
    private bool disposedValue;
    private IAuthenticatorAccountManager accountsManager;

    public ClientAccountManager(
      ILogger logger,
      IClientStorageManager storageManager,
      IAnalyticsProvider analyticsProvider,
      IConfigurationManager configManager,
      IEventAggregator eventAggregator,
      IFeatureSettings featureSettings,
      IApplicationStateMachine stateMachine,
      IAccountStateManager accountStateManager,
      IPublicKeyList publicKeyList)
    {
      this.logger = logger;
      this.storageManager = storageManager;
      this.analyticsProvider = analyticsProvider;
      this.eventAggregator = eventAggregator;
      this.configManager = configManager;
      this.featureSettings = featureSettings;
      this.stateMachine = stateMachine;
      this.accountStateManager = accountStateManager;
      this.uvRequirements = new ConcurrentDictionary<string, UvRequirementType>();
      this.accountsSettings = new ConcurrentDictionary<string, AccountSettingsModel>();
      this.accountsInitializationSource = new TaskCompletionSource<bool>();
      this.accounts = new ConcurrentDictionary<string, IOktaAccount>();
      this.pendingEnrollTasks = new ConcurrentDictionary<string, TaskCompletionSource<EnrollmentEndEventArg>>();
      this.RegisterToEvents();
      this.uvRestrictionsOverridden = this.RetrieveUVRestrictionsSettings();
      this.publicKeyList = publicKeyList;
    }

    public IEnumerable<IOktaAccount> Accounts => (IEnumerable<IOktaAccount>) this.accounts.Values;

    public bool AnyAccounts() => this.accounts.Count > 0;

    public bool TryGetAccount(string accounId, out IOktaAccount account)
    {
      account = (IOktaAccount) null;
      return !string.IsNullOrEmpty(accounId) && this.accounts.TryGetValue(accounId, out account);
    }

    public async Task<bool> AnyAccountsAsync(Func<IOktaAccount, bool> filterPredicate)
    {
      try
      {
        int num = await this.accountsInitializationSource.Task.ConfigureAwait(false) ? 1 : 0;
        return filterPredicate == null ? this.accounts.Values.Any<IOktaAccount>() : this.accounts.Values.Any<IOktaAccount>(filterPredicate);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when checking enrolled accounts", ex, this.analyticsProvider);
        return false;
      }
    }

    public async Task<IOktaAccount> TryGetAccount(Func<IOktaAccount, bool> filterPredicate)
    {
      int num1;
      if (num1 != 0 && filterPredicate == null)
        return (IOktaAccount) null;
      try
      {
        int num2 = await this.accountsInitializationSource.Task.ConfigureAwait(false) ? 1 : 0;
        return this.accounts.Values.FirstOrDefault<IOktaAccount>(filterPredicate);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when attempting to retrieve any enrolled account with a filter", ex, this.analyticsProvider);
      }
      return (IOktaAccount) null;
    }

    public async Task<IList<IOktaAccount>> GetAccounts(Func<IOktaAccount, bool> filterPredicate = null)
    {
      try
      {
        int num = await this.accountsInitializationSource.Task.ConfigureAwait(false) ? 1 : 0;
        return filterPredicate == null ? (IList<IOktaAccount>) this.accounts.Values.ToList<IOktaAccount>() : (IList<IOktaAccount>) this.accounts.Values.Where<IOktaAccount>(filterPredicate).ToList<IOktaAccount>();
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Error when attempting to retrieve enrolled accounts with a filter", ex, this.analyticsProvider);
      }
      return (IList<IOktaAccount>) Array.Empty<IOktaAccount>();
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    public async Task<(OrganizationEnrollmentStatus Status, IOktaOrganization Org)> CheckOrganizationStatus(
      Uri orgUrl)
    {
      OktaOrganization oktaOrg = (OktaOrganization) null;
      bool isAlternate = orgUrl != (Uri) null && !this.publicKeyList.IsPinnedUrl(orgUrl);
      try
      {
        using (new SecureConnectionOperation(DevicesSdk.ConnectionValidator, orgUrl, this.logger, isAlternate))
          oktaOrg = await DevicesSdk.WebClient.GetOktaOrganization(orgUrl).ConfigureAwait(false);
        bool flag = isAlternate;
        if (flag)
          flag = !await this.ValidateUrl(orgUrl, (IOktaOrganization) oktaOrg);
        if (flag)
          return (OrganizationEnrollmentStatus.UrlNotTrusted, (IOktaOrganization) null);
      }
      catch (OktaWebException ex)
      {
        this.logger.WriteException("Failed get organization status", ex.InnerException ?? (Exception) ex);
        return (OrganizationEnrollmentStatus.InvalidUrl, (IOktaOrganization) null);
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        this.logger.WriteException("An error occured when getting the organization status", ex);
        bool flag1 = false;
        bool flag2 = false;
        if (ex.InnerException is WebException innerException)
        {
          if (innerException.Status == WebExceptionStatus.ConnectFailure || innerException.Status == WebExceptionStatus.NameResolutionFailure)
            flag1 = true;
          else if (innerException.Status == WebExceptionStatus.TrustFailure)
            flag2 = true;
        }
        if (!flag1)
          this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        return (flag1 ? OrganizationEnrollmentStatus.InvalidUrl : (flag2 ? OrganizationEnrollmentStatus.UrlNotTrusted : OrganizationEnrollmentStatus.UnknownError), (IOktaOrganization) null);
      }
      if (!oktaOrg.IsOnIdx)
      {
        this.logger.WriteInfoEx("Checking Organization status: " + oktaOrg.Domain + " as IDX is not enabled on org.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (CheckOrganizationStatus));
        return (OrganizationEnrollmentStatus.EnrollmentNotSupported, (IOktaOrganization) oktaOrg);
      }
      int num1 = await this.accountsInitializationSource.Task.ConfigureAwait(false) ? 1 : 0;
      int num2 = this.accounts.Count<KeyValuePair<string, IOktaAccount>>((Func<KeyValuePair<string, IOktaAccount>, bool>) (a => a.Value.OrgId.Equals(oktaOrg.Id, StringComparison.Ordinal)));
      this.logger.WriteInfoEx(string.Format("Checking Organization status: {0} has {1} accounts already enrolled.", (object) oktaOrg.Domain, (object) num2), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (CheckOrganizationStatus));
      return (num2 == 0 ? OrganizationEnrollmentStatus.CanEnrollNewOrganization : OrganizationEnrollmentStatus.CanEnrollExistingOrganization, (IOktaOrganization) oktaOrg);
    }

    public async Task<IOktaAccount> GetDefaultAccount(
      string orgId,
      bool useAsDefaultIfOnlyOneEnrolled = true,
      bool pickOneIfNoDefaultSet = false)
    {
      if (string.IsNullOrEmpty(orgId))
        return (IOktaAccount) null;
      try
      {
        IList<IOktaAccount> source = await this.GetAccounts((Func<IOktaAccount, bool>) (a => a.OrgId == orgId)).ConfigureAwait(false);
        if (source.Count == 0)
        {
          this.logger.WriteInfoEx("No account found for organization.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetDefaultAccount));
          return (IOktaAccount) null;
        }
        if (useAsDefaultIfOnlyOneEnrolled && source.Count == 1)
        {
          this.logger.WriteInfoEx("Only one account present; returning it as the default.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetDefaultAccount));
          return source.First<IOktaAccount>();
        }
        foreach (IOktaAccount defaultAccount in (IEnumerable<IOktaAccount>) source)
        {
          if (this.IsAccountSetAsDefault(defaultAccount.AccountId))
          {
            this.logger.WriteInfoEx("Returning already set default account " + defaultAccount.AccountId + " for " + orgId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetDefaultAccount));
            return defaultAccount;
          }
        }
        if (pickOneIfNoDefaultSet)
        {
          IOktaAccount picked = source.First<IOktaAccount>();
          this.logger.WriteInfoEx("No default account found associated with org " + orgId + "; picking one - " + picked.AccountId + " - and setting it as default...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetDefaultAccount));
          int num = await this.UpdateAccountSettings(picked.AccountId, (Action<AccountSettingsModel>) (s => s.IsDefaultAccount = true)).ConfigureAwait(false) ? 1 : 0;
          return picked;
        }
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("An error occurred while checking the default account for the org " + orgId, ex, this.analyticsProvider);
      }
      return (IOktaAccount) null;
    }

    public async Task<bool> IsAccountUserVerificationRequired(
      string orgId,
      string signInUrl,
      string accessToken,
      bool useCache = true)
    {
      if (string.IsNullOrEmpty(accessToken) || string.IsNullOrWhiteSpace(signInUrl) || string.IsNullOrEmpty(orgId))
        return false;
      Uri uri = new Uri(signInUrl);
      bool bypassSecureConnection = !this.publicKeyList.IsPinnedUrl(uri);
      using (new SecureConnectionOperation(DevicesSdk.ConnectionValidator, uri, this.logger, bypassSecureConnection))
      {
        OktaAccessToken authToken = new OktaAccessToken(accessToken);
        return await this.GetOrUpdateUVRequirement(orgId, (Func<Task<UvRequirementType>>) (() => this.accountsManager.GetUvRequirement((IDeviceToken) authToken, signInUrl)), useCache).ConfigureAwait(false) == UvRequirementType.UvRequired;
      }
    }

    public async Task<bool> IsAccountUserVerificationRequired(IOktaAccount account, bool useCache = true) => account != null && account.Enrollment != null && await this.GetOrUpdateUVRequirement(account.OrgId, (Func<Task<UvRequirementType>>) (() => this.accountsManager.GetUvRequirement(account.Enrollment.AuthenticatorEnrollmentId)), useCache, account.AccountId).ConfigureAwait(false) == UvRequirementType.UvRequired;

    public async Task<(Uri, string)> GetPrimaryOrganization()
    {
      Uri result = this.configManager.GetWebAddressFromRegistry(this.logger, "SignInUrl");
      if (result != (Uri) null)
      {
        try
        {
          (OrganizationEnrollmentStatus enrollmentStatus, IOktaOrganization oktaOrganization) = await this.CheckOrganizationStatus(result).ConfigureAwait(false);
          if (!string.IsNullOrEmpty(oktaOrganization?.Domain))
          {
            if (Uri.TryCreate(oktaOrganization.Domain, UriKind.Absolute, out result))
            {
              this.logger.WriteInfoEx(string.Format("Using pre configured org url {0} for auto update. Org status: {1}", (object) result, (object) enrollmentStatus), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetPrimaryOrganization));
              return (result, oktaOrganization.Id);
            }
          }
        }
        catch (Exception ex) when (
        {
          // ISSUE: unable to correctly present filter
          ILogger logger = this.logger;
          if (!ex.IsCritical(logger))
          {
            SuccessfulFiltering;
          }
          else
            throw;
        }
        )
        {
          this.logger.WriteWarningEx(string.Format("Org url {0} is not a valid okta endpoint.", (object) result), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetPrimaryOrganization));
        }
      }
      int num = await this.accountsInitializationSource.Task.ConfigureAwait(false) ? 1 : 0;
      foreach (IOktaAccount oktaAccount in (IEnumerable<IOktaAccount>) this.accounts.Values)
      {
        if (Uri.TryCreate(oktaAccount.Organization.InternalUrl, UriKind.Absolute, out result))
        {
          this.logger.WriteInfoEx(string.Format("Using org url {0} from enrolled account {1} for auto update.", (object) result, (object) oktaAccount.AccountId), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetPrimaryOrganization));
          return (result, oktaAccount.OrgId);
        }
      }
      return ((Uri) null, (string) null);
    }

    [AnalyticsScenario(ScenarioType.AddAccount)]
    public async Task<(IOktaAccount Account, EnrollmentError Error)> EnrollAccount(
      EnrollmentParameters enrollmentParameters,
      bool withUserVerification,
      bool setAsDefault = false)
    {
      string signInUrl = enrollmentParameters.SignInUrl;
      Uri uri = new Uri(signInUrl);
      bool bypassSecureConnection = !this.publicKeyList.IsPinnedUrl(uri);
      string accessToken = enrollmentParameters.AccessToken;
      if (signInUrl == null || string.IsNullOrEmpty(accessToken))
        return ((IOktaAccount) null, new EnrollmentError());
      try
      {
        using (new SecureConnectionOperation(DevicesSdk.ConnectionValidator, uri, this.logger, bypassSecureConnection))
        {
          string userId = enrollmentParameters.UserId;
          IDeviceEnrollment deviceEnrollment = await this.accountsManager.EnrollAuthenticator((IDeviceToken) new OktaAccessToken(accessToken), signInUrl, (IDeviceSignals) null, withUserVerification).ConfigureAwait(false);
          (IUserInformation, IOrganizationInformation) valueTuple = await this.GetAccountData(deviceEnrollment, userId).ConfigureAwait(false);
          OktaAccount account = new OktaAccount(valueTuple.Item1, valueTuple.Item2, deviceEnrollment, withUserVerification);
          int num = await this.UpdateAccountDefaultSettings((IOktaAccount) account, setAsDefault).ConfigureAwait(false) ? 1 : 0;
          this.UpdateCollection(ListChangeType.Added, (IOktaAccount) account);
          return ((IOktaAccount) account, new EnrollmentError());
        }
      }
      catch (OktaWebException ex)
      {
        PlatformErrorCode platformErrorCode = ex.GetPlatformErrorCode();
        this.logger.WriteErrorEx(string.Format("API error code {0} detected while enrolling a new account.", (object) platformErrorCode), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (EnrollAccount));
        return ((IOktaAccount) null, new EnrollmentError(platformErrorCode));
      }
      catch (OktaCryptographicException ex)
      {
        this.logger.WriteException("Encountered a cryptographic error while enrolling:", (Exception) ex);
        return ((IOktaAccount) null, new EnrollmentError(ex.ErrorCode));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Got unexpected exception on enrollment: ", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return ((IOktaAccount) null, new EnrollmentError());
    }

    public async Task<(IOktaAccount Account, EnrollmentError Error)> ReEnrollAccount(
      string accountId,
      EnrollmentParameters enrollmentParameters,
      bool withUserVerification,
      bool setAsDefault = false)
    {
      IOktaAccount account;
      if (!this.TryGetAccount(accountId, out account))
        return ((IOktaAccount) null, new EnrollmentError());
      return await this.DeleteAccount(account, true).ConfigureAwait(false) == AuthenticatorOperationResult.Success ? await this.EnrollAccount(enrollmentParameters, withUserVerification, setAsDefault).ConfigureAwait(false) : ((IOktaAccount) null, new EnrollmentError());
    }

    public async Task<AuthenticatorOperationResult> DeleteAccount(
      IOktaAccount account,
      bool localOnly = false)
    {
      account.EnsureNotNull(nameof (account));
      AuthenticatorOperationResult authenticatorOperationResult;
      if (localOnly)
        authenticatorOperationResult = await this.accountsManager.RemoveAuthenticatorLocally(account.AccountId).ConfigureAwait(false);
      else
        authenticatorOperationResult = await ClientAccountManager.DeleteAccount(account.AccountId, this.accountsManager, this.logger, this.analyticsProvider).ConfigureAwait(false);
      AuthenticatorOperationResult deleteResult = authenticatorOperationResult;
      if (deleteResult == AuthenticatorOperationResult.Success)
      {
        int num = await this.RemoveAccountSettings(account).ConfigureAwait(false) ? 1 : 0;
        this.accountStateManager.EnsureAccountStateReset(account.AccountId);
      }
      return deleteResult;
    }

    public static async Task<AuthenticatorOperationResult> DeleteAccount(
      string authenticatorEnrollmentId,
      IAuthenticatorAccountManager authenticatorAccountManager,
      ILogger logger,
      IAnalyticsProvider analyticsProvider)
    {
      authenticatorAccountManager.EnsureNotNull(nameof (authenticatorAccountManager));
      logger.EnsureNotNull(nameof (logger));
      analyticsProvider.EnsureNotNull(nameof (analyticsProvider));
      AuthenticatorOperationResult result = AuthenticatorOperationResult.Unknown;
      bool removeLocally = false;
      try
      {
        result = await authenticatorAccountManager.RemoveAuthenticator(authenticatorEnrollmentId).ConfigureAwait(false);
        removeLocally = result != AuthenticatorOperationResult.Success && result != AuthenticatorOperationResult.Cancelled;
      }
      catch (OktaWebException ex)
      {
        removeLocally = true;
        logger.WriteErrorEx("Failed to remove the account, due to web exception: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (DeleteAccount));
        analyticsProvider.TrackErrorWithLogs(string.Format("Failed to remove account from Okta: {0} {1}", (object) ex.StatusCode, (object) ex.Error?.Code), sourceMethodName: nameof (DeleteAccount));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        removeLocally = true;
        logger.WriteErrorEx(string.Format("Failed to remove the account with {0}", (object) ex.GetType()), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (DeleteAccount));
        analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      finally
      {
        if (removeLocally)
        {
          logger.WriteInfoEx("Removing the account " + authenticatorEnrollmentId + " locally only.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (DeleteAccount));
          result = await authenticatorAccountManager.RemoveAuthenticatorLocally(authenticatorEnrollmentId).ConfigureAwait(false);
        }
      }
      return result;
    }

    public async Task<AuthenticatorOperationResult> AddAccountUserVerification(
      string accountId,
      string accessToken)
    {
      IOktaAccount account;
      if (!this.TryGetAccount(accountId, out account) || string.IsNullOrEmpty(accessToken))
        return AuthenticatorOperationResult.Failed;
      try
      {
        if (await this.accountStateManager.InvokeWithAccountStateTrack<AuthenticatorOperationResult>(accountId, (Func<Task<AuthenticatorOperationResult>>) (() => this.accountsManager.EnrollAuthenticatorUserVerification(accountId, (IDeviceToken) new OktaAccessToken(accessToken)))).ConfigureAwait(false) != AuthenticatorOperationResult.Success)
          return AuthenticatorOperationResult.Failed;
        this.UpdateAccount(account, true);
        return AuthenticatorOperationResult.Success;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        if (ex is OktaCryptographicException cryptographicException && cryptographicException.ErrorCode == PlatformErrorCode.UserCancelled)
        {
          this.logger.WriteInfoEx("Adding user verification was cancelled by the user.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (AddAccountUserVerification));
          return AuthenticatorOperationResult.Cancelled;
        }
        this.logger.WriteException("Failed to add user verification.", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        return AuthenticatorOperationResult.Failed;
      }
    }

    public async Task<bool> AddOrUpdateUserInformation(UserInformation userInformation)
    {
      if (userInformation == null || string.IsNullOrEmpty(userInformation.Id))
        return false;
      if (await this.storageManager.Store.PutDataAsync<UserInformation>(userInformation.Id, userInformation).ConfigureAwait(false))
        return true;
      this.logger.WriteErrorEx("Failed to persist user information for user " + userInformation.Id + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (AddOrUpdateUserInformation));
      this.analyticsProvider.TrackErrorWithLogs("Failed to persist user information", sourceMethodName: nameof (AddOrUpdateUserInformation));
      return false;
    }

    public async Task<bool> RemoveUserInformation(string userId)
    {
      if (string.IsNullOrEmpty(userId))
        return false;
      if (await this.storageManager.Store.RemoveDataAsync<UserInformation>(userId).ConfigureAwait(false))
        return true;
      this.logger.WriteWarningEx("Failed to remove user information for user " + userId + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (RemoveUserInformation));
      return false;
    }

    public async Task<AuthenticatorOperationResult> RemoveAccountUserVerification(
      IOktaAccount account)
    {
      if (account == null || !account.IsUserVerificationEnabled)
        return AuthenticatorOperationResult.Failed;
      try
      {
        int num = (int) await this.accountStateManager.InvokeWithAccountStateTrack<AuthenticatorOperationResult>(account.AccountId, (Func<Task<AuthenticatorOperationResult>>) (() => this.accountsManager.RemoveAuthenticatorUserVerification(account.AccountId))).ConfigureAwait(false);
        if (num == 1)
          this.UpdateAccount(account, false);
        return (AuthenticatorOperationResult) num;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Failed to remove user verification.", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
        return AuthenticatorOperationResult.Failed;
      }
    }

    public async Task<bool> IsUserVerificationInvalidated(IOktaAccount account)
    {
      int num;
      if (num != 0 && (account == null || !account.IsUserVerificationEnabled || string.IsNullOrEmpty(account.AccountId)))
        return false;
      try
      {
        return !await this.accountsManager.HasValidUserVerificationKey(account.AccountId).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        KeyCheckResult accessFailureReason = ex.TryGetKeyAccessFailureReason();
        this.logger.WriteWarningEx(string.Format("Failed to validate the user verification key for account {0} with status {1}.", (object) account.AccountId, (object) accessFailureReason), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (IsUserVerificationInvalidated));
        if (accessFailureReason == KeyCheckResult.Unknown)
          this.analyticsProvider.TrackErrorWithLogsAndAppData("Failed to validate the user verification key: " + ex.Message, ex.HResult, nameof (IsUserVerificationInvalidated));
        return false;
      }
    }

    public bool IsAccountSetAsDefault(string accountId)
    {
      AccountSettingsModel accountSettingsModel;
      return !string.IsNullOrEmpty(accountId) && this.accountsSettings.TryGetValue(accountId, out accountSettingsModel) && accountSettingsModel.IsDefaultAccount;
    }

    public (bool Dismissed, bool Critical) GetAccountErrorState(
      string accountId,
      AccountErrorStateTypes types)
    {
      if (types == AccountErrorStateTypes.None)
        return (false, false);
      return this.accountStateManager.IsErrorStateCritical(types) ? (false, true) : (this.IsMessageDismissed(accountId, types), false);
    }

    public async Task<bool> UpdateAccountWarningSettings(
      string accountId,
      AccountErrorStateTypes dismissedWarnings)
    {
      AccountSettingsModel accountSettingsModel;
      return !string.IsNullOrEmpty(accountId) && !this.accountStateManager.IsErrorStateCritical(dismissedWarnings) && (!this.accountsSettings.TryGetValue(accountId, out accountSettingsModel) || accountSettingsModel.DismissedAccountWarnings != dismissedWarnings) && await this.UpdateAccountSettings(accountId, (Action<AccountSettingsModel>) (account => account.DismissedAccountWarnings = dismissedWarnings)).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAccountDefaultSettings(string accountId, bool isDefault)
    {
      int num;
      IOktaAccount account;
      if (num != 0 && (string.IsNullOrEmpty(accountId) || !this.accounts.TryGetValue(accountId, out account)))
        return false;
      try
      {
        this.logger.WriteInfoEx(string.Format("Request to update the account default settings. Account: {0} - Set as default: {1}", (object) accountId, (object) isDefault), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (UpdateAccountDefaultSettings));
        return await this.UpdateAccountDefaultSettings(account, isDefault).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("An error occurred while updating the account default settings for " + accountId, ex, this.analyticsProvider);
      }
      return false;
    }

    public async Task<bool> RemoveAccountSettings(IOktaAccount account)
    {
      if (string.IsNullOrEmpty(account?.AccountId))
        return false;
      AccountSettingsModel accountSettingsModel;
      if (!this.accountsSettings.TryGetValue(account.AccountId, out accountSettingsModel))
        return false;
      try
      {
        int num = await this.storageManager.Store.RemoveDataAsync<AccountSettingsModel>(accountSettingsModel.AccountId).ConfigureAwait(false) ? 1 : 0;
        if (num != 0)
          this.accountsSettings.TryRemove(account.AccountId, out accountSettingsModel);
        return num != 0;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Failed to remove the account settings", ex, this.analyticsProvider);
      }
      return false;
    }

    public async Task AccountsInitialization()
    {
      int num = await this.accountsInitializationSource.Task.ConfigureAwait(false) ? 1 : 0;
    }

    public async Task<EnrollmentEndEventArg> InvokeEnrollmentFlow(
      EnrollmentStartEventArg enrollmentArgs,
      bool waitForPending = false)
    {
      try
      {
        ConfiguredTaskAwaitable configuredTaskAwaitable = this.AccountsInitialization().ConfigureAwait(false);
        await configuredTaskAwaitable;
        if (enrollmentArgs.EnrollType != EnrollStartEventType.AddAccount)
          return await this.InvokeEnrollmentFlow(enrollmentArgs.AccountOrgId, enrollmentArgs, waitForPending).ConfigureAwait(false);
        configuredTaskAwaitable = this.InvokeManualEnrollmentFlow(enrollmentArgs, waitForPending).ConfigureAwait(false);
        await configuredTaskAwaitable;
      }
      catch (TaskCanceledException ex)
      {
        this.logger.WriteWarningEx(string.Format("Cancellation received while processing the enrollment for {0}: {1}", (object) enrollmentArgs.EnrollType, (object) ex.Message), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InvokeEnrollmentFlow));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteErrorEx(string.Format("An error occurred while processing the enrollment for {0} - {1}: {2}", (object) enrollmentArgs.EnrollType, (object) enrollmentArgs.AccountOrgId, (object) ex.Message), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InvokeEnrollmentFlow));
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return new EnrollmentEndEventArg();
    }

    internal async Task<bool> LoadAccounts(bool subscribeToUpdates = true)
    {
      ClientAccountManager clientAccountManager = this;
      try
      {
        clientAccountManager.UpdateCollection(ListChangeType.Cleared);
        if (subscribeToUpdates)
          clientAccountManager.storageManager.SubscribeToStoreUpdates(new Action<StoreChangeType, IList>(clientAccountManager.OnStoreDataUpdated));
        IStorageManager store = clientAccountManager.storageManager.Store;
        List<DeviceEnrollment> enrollments = await store.GetAllAsync<DeviceEnrollment>().ConfigureAwait(false);
        if (enrollments.Count == 0)
          return true;
        Dictionary<string, UserInformation> users = (await store.GetAllAsync<UserInformation>().ConfigureAwait(false)).ToDictionary<UserInformation, string>((Func<UserInformation, string>) (u => u.Id));
        Dictionary<string, OrganizationInformation> organizations = (await store.GetAllAsync<OrganizationInformation>().ConfigureAwait(false)).ToDictionary<OrganizationInformation, string>((Func<OrganizationInformation, string>) (o => o.Id));
        Dictionary<string, AuthenticatorVerificationMethod> dictionary = (await store.GetAllAsync<AuthenticatorVerificationMethod>().ConfigureAwait(false)).ToDictionary<AuthenticatorVerificationMethod, string>((Func<AuthenticatorVerificationMethod, string>) (m => m.Id));
        foreach (DeviceEnrollment deviceEnrollment in enrollments)
        {
          DeviceEnrollment enrollment = deviceEnrollment;
          if (!users.ContainsKey(enrollment.UserId) || !organizations.ContainsKey(enrollment.OrganizationId))
          {
            clientAccountManager.logger.WriteErrorEx("Enrollment UserId or OrganizationId can't be found.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (LoadAccounts));
            return false;
          }
          bool isUserVerificationEnabled = dictionary.Any<KeyValuePair<string, AuthenticatorVerificationMethod>>((Func<KeyValuePair<string, AuthenticatorVerificationMethod>, bool>) (f => enrollment.MethodEnrollmentIds.Contains(f.Key) && f.Value.Type == AuthenticationMethodType.SignedNonce && f.Value.Credentials.ContainsKey(VerificationCredentialType.UserPresence)));
          OktaAccount account = new OktaAccount((IUserInformation) users[enrollment.UserId], (IOrganizationInformation) organizations[enrollment.OrganizationId], (IDeviceEnrollment) enrollment, isUserVerificationEnabled);
          clientAccountManager.UpdateCollection(ListChangeType.Added, (IOktaAccount) account);
        }
        store = (IStorageManager) null;
        enrollments = (List<DeviceEnrollment>) null;
        users = (Dictionary<string, UserInformation>) null;
        organizations = (Dictionary<string, OrganizationInformation>) null;
      }
      catch (Exception ex) when (!ex.IsCritical(clientAccountManager.logger))
      {
        clientAccountManager.logger.WriteErrorEx("Failed to load acccounts: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (LoadAccounts));
        clientAccountManager.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return clientAccountManager.accounts.Count > 0;
    }

    internal async Task<bool> LoadAccountsSettings()
    {
      bool loaded = false;
      this.accountsSettings.Clear();
      try
      {
        List<AccountSettingsModel> accountSettingsModelList = await this.storageManager.Store.GetAllAsync<AccountSettingsModel>().ConfigureAwait(false);
        foreach (AccountSettingsModel accountSettingsModel in accountSettingsModelList)
          this.accountsSettings.TryAdd(accountSettingsModel.AccountId, accountSettingsModel);
        loaded = accountSettingsModelList.Count == this.accountsSettings.Count;
        this.logger.WriteInfoEx(string.Format("Accounts settings successfully loaded: {0}", (object) loaded), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (LoadAccountsSettings));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("An error occurred while loading the accounts settings", ex);
      }
      return loaded;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposedValue)
        return;
      if (disposing)
      {
        this.accountsManager.SafeDispose();
        this.storageManager.UnsubscribeFromStoreUpdates(new Action<StoreChangeType, IList>(this.OnStoreDataUpdated));
        this.EnsurePendingTasksCleared();
      }
      this.disposedValue = true;
    }

    private async Task<bool> ValidateUrl(
      Uri alternateUrl,
      IOktaOrganization orgInfoFromAlternateUrl)
    {
      alternateUrl.EnsureNotNull(nameof (alternateUrl));
      try
      {
        Uri result;
        if (!Uri.TryCreate(orgInfoFromAlternateUrl.Domain, UriKind.Absolute, out result))
          this.logger.WriteErrorEx("The org url found in the org info has an invalid format", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (ValidateUrl));
        if (!this.publicKeyList.IsPinnedUrl(result))
        {
          this.logger.WriteErrorEx("The org url found in the org info must be a valid Okta domain.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (ValidateUrl));
          return false;
        }
        OktaOrganization orgInfo = await DevicesSdk.WebClient.GetOktaOrganization(result).ConfigureAwait(false);
        if (!this.IsValidAlternate(alternateUrl, orgInfo))
        {
          this.logger.WriteInfoEx(string.Format("Enrollment URL {0} is not a valid custom domain URL", (object) alternateUrl), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (ValidateUrl));
          return false;
        }
        this.logger.WriteInfoEx(string.Format("Enrollment URL {0} is a valid custom domain URL", (object) alternateUrl), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (ValidateUrl));
        return true;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteErrorEx(string.Format("Error trying to validate enrollment URL {0} : {1}", (object) alternateUrl, (object) ex), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (ValidateUrl));
        return false;
      }
    }

    private async Task InitializeAccounts()
    {
      this.accountsManager = (IAuthenticatorAccountManager) new AuthenticatorAccountManager();
      bool accountsLoaded = await this.LoadAccounts().ConfigureAwait(false);
      bool flag = await this.LoadAccountsSettings().ConfigureAwait(false);
      this.accountsInitializationSource.TrySetResult(accountsLoaded & flag);
      this.logger.WriteInfoEx(string.Format("Accounts initialized, Accounts: {0}, Settings: {1}", (object) accountsLoaded, (object) flag), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InitializeAccounts));
      if (!this.AnyAccounts())
        return;
      this.analyticsProvider.SetUserId(this.accounts.Values.FirstOrDefault<IOktaAccount>()?.UserId);
    }

    private async Task ShowOfflineFactorsIfNotEnrolled()
    {
      if (!Okta.DeviceAccess.Windows.Injector.AppInjector.Initialized)
        return;
      IOfflineFactorManager offlineFactorManager = Okta.DeviceAccess.Windows.Injector.AppInjector.Get<IOfflineFactorManager>();
      IEnumerable<OfflineFactorModel> source = await Task.Run<IEnumerable<OfflineFactorModel>>((Func<IEnumerable<OfflineFactorModel>>) (() => offlineFactorManager.GetEnrolledOfflineFactors()));
      if (source == null || source.Any<OfflineFactorModel>())
        return;
      this.stateMachine.TransitionTo(AppStateRequestType.BringToFocus);
      this.eventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.OfflineFactors));
    }

    private void UpdateAccount(IOktaAccount account, bool verificationEnabled)
    {
      if (account == null)
        return;
      account.UpdateAccountKeyInformation(verificationEnabled);
      this.UpdateCollection(ListChangeType.Updated, account);
    }

    private async Task<(IUserInformation UserInfo, IOrganizationInformation OrgInfo)> GetAccountData(
      IDeviceEnrollment deviceEnrollment,
      string userId)
    {
      ClientAccountManager clientAccountManager = this;
      OrganizationInformation org = await clientAccountManager.storageManager.Store.GetDataAsync<OrganizationInformation>(deviceEnrollment.OrganizationId).ConfigureAwait(false);
      if (org == null)
      {
        await clientAccountManager.RollbackEnrollmentWithError("Missing org " + deviceEnrollment.OrganizationId + " from enrollment " + deviceEnrollment.AuthenticatorEnrollmentId + " in storage.", deviceEnrollment.AuthenticatorEnrollmentId, userId).ConfigureAwait(false);
        return ();
      }
      if (deviceEnrollment.UserId != userId)
      {
        await clientAccountManager.RollbackEnrollmentWithError("UserId " + deviceEnrollment.UserId + " from enrollment " + deviceEnrollment.AuthenticatorEnrollmentId + " does not match signed in user " + userId, deviceEnrollment.AuthenticatorEnrollmentId, userId).ConfigureAwait(false);
        return ();
      }
      UserInformation userInformation = await clientAccountManager.storageManager.Store.TryGetDataAsync<UserInformation>(userId).ConfigureAwait(false);
      if (userInformation != null)
        return ((IUserInformation) userInformation, (IOrganizationInformation) org);
      await clientAccountManager.RollbackEnrollmentWithError("User information was not persisted.", deviceEnrollment.AuthenticatorEnrollmentId, (string) null).ConfigureAwait(false);
      return ();
    }

    private async Task RollbackEnrollmentWithError(
      string message,
      string enrollmentId,
      string userId)
    {
      this.logger.WriteError("ClientAccountManager.EnrollAccount", message);
      this.analyticsProvider.TrackErrorWithLogs(message, sourceMethodName: nameof (RollbackEnrollmentWithError));
      int num1 = (int) await ClientAccountManager.DeleteAccount(enrollmentId, this.accountsManager, this.logger, this.analyticsProvider).ConfigureAwait(false);
      int num2 = await this.RemoveUserInformation(userId).ConfigureAwait(false) ? 1 : 0;
    }

    private void RegisterToEvents()
    {
      this.eventAggregator.GetEvent<AppStateEvent>().Subscribe(new Action<AppState>(this.OnAppStateUpdated));
      this.eventAggregator.GetEvent<AccountEnrollStartEvent>().Subscribe(new Action<EnrollmentStartEventArg>(this.OnEnrollStarted));
      this.eventAggregator.GetEvent<AccountEnrollEndEvent>().Subscribe(new Action<EnrollmentEndEventArg>(this.OnEnrollmentEnded));
      this.eventAggregator.GetEvent<UserSessionChangedEvent>().Subscribe(new Action<UserSessionChangedEventType>(this.OnUserSessionChanged));
      this.stateMachine.RegisterDeferral(ComputingStateType.Loading, (ComputingStateDeferral) (s => this.InitializeAccounts()), "initializing account manager");
    }

    private bool RetrieveUVRestrictionsSettings()
    {
      bool flag = this.featureSettings.IsFeatureEnabled(FeatureType.OverrideUVRestrictions);
      this.logger.WriteInfoEx(string.Format("User verification restrictions will be overridden: {0}", (object) flag), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (RetrieveUVRestrictionsSettings));
      return flag;
    }

    private void RemoveAccounts(IEnumerable<string> enrollmentIds)
    {
      foreach (string enrollmentId in enrollmentIds)
      {
        IOktaAccount account;
        if (this.accounts.TryGetValue(enrollmentId, out account))
          this.UpdateCollection(ListChangeType.Removed, account);
      }
    }

    private void UpdateCollection(ListChangeType changeType, IOktaAccount account = null)
    {
      bool flag = false;
      switch (changeType)
      {
        case ListChangeType.Added:
          flag = this.accounts.TryAdd(account.AccountId, account);
          break;
        case ListChangeType.Updated:
          if (this.accounts.ContainsKey(account.AccountId))
          {
            this.accounts[account.AccountId] = account;
            flag = true;
            break;
          }
          break;
        case ListChangeType.Removed:
          flag = this.accounts.TryRemove(account.AccountId, out IOktaAccount _);
          break;
        case ListChangeType.Cleared:
          this.accounts.Clear();
          flag = true;
          break;
      }
      if (!flag)
        return;
      this.eventAggregator.GetEvent<AccountListEvent>()?.Publish(new AccountListChange(changeType, account));
    }

    private async Task EnsurePendingTasksCancelledOrCompleted(bool waitForPending)
    {
      if (this.pendingEnrollTasks.IsEmpty)
        return;
      if (waitForPending)
        await this.EnsurePendingEnrollTasksCompleted().ConfigureAwait(false);
      else
        this.EnsurePendingTasksCleared();
    }

    private async Task EnsurePendingEnrollTasksCompleted()
    {
      if (this.pendingEnrollTasks.IsEmpty)
        return;
      this.logger.WriteInfoEx("Making sure all pending tasks are completed...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (EnsurePendingEnrollTasksCompleted));
      foreach (KeyValuePair<string, TaskCompletionSource<EnrollmentEndEventArg>> pendingEnrollTask in this.pendingEnrollTasks)
      {
        EnrollmentEndEventArg enrollmentEndEventArg = await pendingEnrollTask.Value.Task.ConfigureAwait(false);
      }
    }

    private async Task InvokeManualEnrollmentFlow(
      EnrollmentStartEventArg enrollArg,
      bool waitForPending)
    {
      await this.EnsurePendingTasksCancelledOrCompleted(waitForPending).ConfigureAwait(false);
      this.eventAggregator.GetEvent<ViewStateRequestEvent>().Publish(new ViewStateRequest(MainViewType.EnrollAccount, (object) new EnrollmentStateContext(EnrollmentStateType.Manual, enrollArg)));
    }

    private async Task<EnrollmentEndEventArg> InvokeEnrollmentFlow(
      string orgId,
      EnrollmentStartEventArg enrollArg,
      bool waitForPending)
    {
      TaskCompletionSource<EnrollmentEndEventArg> completionSource1;
      if (this.pendingEnrollTasks.TryGetValue(orgId, out completionSource1))
      {
        this.logger.WriteWarningEx("Enrollment associated with " + orgId + " already in progress; waiting for it to complete...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InvokeEnrollmentFlow));
        return await completionSource1.Task.ConfigureAwait(false);
      }
      await this.EnsurePendingTasksCancelledOrCompleted(waitForPending).ConfigureAwait(false);
      TaskCompletionSource<EnrollmentEndEventArg> completionSource2 = new TaskCompletionSource<EnrollmentEndEventArg>(TaskCreationOptions.RunContinuationsAsynchronously);
      if (!this.pendingEnrollTasks.TryAdd(orgId, completionSource2))
      {
        this.logger.WriteWarningEx("Failed to queue the enrollment task associated with " + orgId, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InvokeEnrollmentFlow));
        completionSource2.SetResult(new EnrollmentEndEventArg());
      }
      else
      {
        this.logger.WriteInfoEx("Initiating the enrollment task for " + orgId + "...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InvokeEnrollmentFlow));
        EnrollmentStateContext context = (EnrollmentStateContext) null;
        switch (enrollArg.EnrollType)
        {
          case EnrollStartEventType.EnableUserVerification:
            context = new EnrollmentStateContext(EnrollmentStateType.EnableUserVerification, enrollArg);
            break;
          case EnrollStartEventType.DisableUserVerification:
            context = new EnrollmentStateContext(EnrollmentStateType.DisableUserVerification, enrollArg);
            break;
          case EnrollStartEventType.JustInTimeEnrollment:
            context = new EnrollmentStateContext(EnrollmentStateType.JustInTime, enrollArg);
            break;
          case EnrollStartEventType.ReEnrollment:
            context = new EnrollmentStateContext(EnrollmentStateType.ReEnroll, enrollArg);
            break;
          default:
            this.logger.WriteWarningEx(string.Format("{0} won't be processed", (object) enrollArg.EnrollType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (InvokeEnrollmentFlow));
            break;
        }
        this.eventAggregator.GetEvent<ViewStateRequestEvent>().Publish(new ViewStateRequest(MainViewType.EnrollAccount, (object) context));
      }
      return await completionSource2.Task.ConfigureAwait(false);
    }

    private void EnsurePendingTasksCleared()
    {
      foreach (KeyValuePair<string, TaskCompletionSource<EnrollmentEndEventArg>> pendingEnrollTask in this.pendingEnrollTasks)
      {
        this.logger.WriteInfoEx("Cancelling the enrollment task associated with " + pendingEnrollTask.Key, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (EnsurePendingTasksCleared));
        pendingEnrollTask.Value.TrySetCanceled();
      }
      this.pendingEnrollTasks.Clear();
    }

    private void OnStoreDataUpdated(StoreChangeType changeType, IList items)
    {
      if (items == null)
        return;
      IEnumerable<IDeviceEnrollment> source = items.OfType<IDeviceEnrollment>();
      if (!source.Any<IDeviceEnrollment>())
        return;
      switch (changeType)
      {
        case StoreChangeType.Deleted:
          this.RemoveAccounts(source.Select<IDeviceEnrollment, string>((Func<IDeviceEnrollment, string>) (e => e.AuthenticatorEnrollmentId)));
          break;
      }
    }

    private void OnUserSessionChanged(UserSessionChangedEventType userSessionEventType)
    {
      if (userSessionEventType != UserSessionChangedEventType.ActivatedUserProfile)
        return;
      this.ShowOfflineFactorsIfNotEnrolled().AsBackgroundTask("User session active, show offline factor enrollment if not enrolled");
    }

    private void OnAppStateUpdated(AppState appState)
    {
      if (appState.StateType == ComputingStateType.Resetting)
        this.EnsurePendingTasksCleared();
      if (appState.StateType != ComputingStateType.Idle || appState.StateArgument == StartupArgumentType.Uri)
        return;
      this.ShowOfflineFactorsIfNotEnrolled().AsBackgroundTask("App state updated, show offline factor enrollment if not enrolled");
    }

    private void OnEnrollStarted(EnrollmentStartEventArg arg)
    {
      this.logger.WriteInfoEx(string.Format("Received account enrollment start event: {0}", (object) arg.EnrollType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (OnEnrollStarted));
      this.InvokeEnrollmentFlow(arg, false).AsBackgroundTask("Enrollment start flow");
    }

    private void OnEnrollmentEnded(EnrollmentEndEventArg updatedEnroll)
    {
      if (this.pendingEnrollTasks.IsEmpty || updatedEnroll.EnrollType == EnrollEndEventType.Unknown)
        return;
      string accountOrgId = updatedEnroll.AccountOrgId;
      TaskCompletionSource<EnrollmentEndEventArg> completionSource;
      if (string.IsNullOrEmpty(accountOrgId) || !this.pendingEnrollTasks.TryRemove(accountOrgId, out completionSource))
      {
        this.logger.WriteWarningEx(string.Format("Enrollment update {0} won't be processed: {1}", (object) updatedEnroll.EnrollType, (object) accountOrgId), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (OnEnrollmentEnded));
      }
      else
      {
        this.logger.WriteInfoEx(string.Format("Update received for enrollment task: {0} - Update: {1}", (object) accountOrgId, (object) updatedEnroll.EnrollType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (OnEnrollmentEnded));
        completionSource.TrySetResult(updatedEnroll);
      }
    }

    private async Task<UvRequirementType> GetOrUpdateUVRequirement(
      string orgId,
      Func<Task<UvRequirementType>> fetchAction,
      bool useCache,
      string accountId = null)
    {
      if (this.uvRestrictionsOverridden)
        return UvRequirementType.UvPreferred;
      try
      {
        UvRequirementType requirementType;
        if (useCache && this.uvRequirements.TryGetValue(orgId, out requirementType))
          return requirementType;
        UvRequirementType uvRequirementType;
        if (string.IsNullOrEmpty(accountId))
          uvRequirementType = await fetchAction().ConfigureAwait(false);
        else
          uvRequirementType = await this.accountStateManager.InvokeWithAccountStateTrack<UvRequirementType>(accountId, fetchAction).ConfigureAwait(false);
        requirementType = uvRequirementType;
        if (requirementType != UvRequirementType.None)
        {
          int num = (int) this.uvRequirements.AddOrUpdate(orgId, requirementType, (Func<string, UvRequirementType, UvRequirementType>) ((k, v) => requirementType));
          this.logger.WriteInfoEx(string.Format("UV requirement for org {0} updated to {1}", (object) orgId, (object) requirementType), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (GetOrUpdateUVRequirement));
        }
        return requirementType;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Failed to get the UV requirement state for org " + orgId + "...", ex);
      }
      return UvRequirementType.None;
    }

    private bool IsMessageDismissed(string accountId, AccountErrorStateTypes type)
    {
      AccountSettingsModel accountSettingsModel;
      return !string.IsNullOrEmpty(accountId) && this.accountsSettings.TryGetValue(accountId, out accountSettingsModel) && accountSettingsModel.DismissedAccountWarnings.HasFlag((System.Enum) type);
    }

    private async Task<bool> UpdateAccountDefaultSettings(IOktaAccount account, bool setAsDefault)
    {
      AccountSettingsModel accountSettingsModel;
      if ((this.accountsSettings.TryGetValue(account.AccountId, out accountSettingsModel) ? (accountSettingsModel.IsDefaultAccount == setAsDefault ? 1 : 0) : (!setAsDefault ? 1 : 0)) != 0)
        return false;
      if (setAsDefault)
      {
        IOktaAccount oktaAccount = await this.GetDefaultAccount(account.OrgId, false).ConfigureAwait(false);
        if (oktaAccount != null && oktaAccount.AccountId != account.AccountId)
        {
          bool flag = await this.UpdateAccountSettings(oktaAccount.AccountId, (Action<AccountSettingsModel>) (c => c.IsDefaultAccount = false)).ConfigureAwait(false);
          this.logger.WriteInfoEx(string.Format("Current default account for org {0} reset: {1}", (object) account.OrgId, (object) flag), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Enrollment\\ClientAccountManager.cs", nameof (UpdateAccountDefaultSettings));
        }
      }
      return await this.UpdateAccountSettings(account.AccountId, (Action<AccountSettingsModel>) (a => a.IsDefaultAccount = setAsDefault)).ConfigureAwait(false);
    }

    private async Task<bool> UpdateAccountSettings(
      string accountId,
      Action<AccountSettingsModel> updateAction)
    {
      int num1;
      if (num1 != 0 && (string.IsNullOrEmpty(accountId) || updateAction == null))
        return false;
      try
      {
        AccountSettingsModel settings;
        if (!this.accountsSettings.TryGetValue(accountId, out settings))
          settings = new AccountSettingsModel()
          {
            AccountId = accountId
          };
        updateAction(settings);
        int num2 = await this.storageManager.Store.PutDataAsync<AccountSettingsModel>(settings.AccountId, settings).ConfigureAwait(false) ? 1 : 0;
        if (num2 != 0)
          this.accountsSettings.AddOrUpdate(accountId, settings, (Func<string, AccountSettingsModel, AccountSettingsModel>) ((id, current) => settings));
        return num2 != 0;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.LogAndReportToAnalytics("Failed to update the account settings for " + accountId, ex, this.analyticsProvider);
      }
      return false;
    }

    private bool IsValidAlternate(Uri uri, OktaOrganization orgInfo)
    {
      List<string> source = new List<string>();
      if (orgInfo.Alternate != null)
        source.Add(orgInfo.Alternate);
      if (orgInfo.Alternates != null && orgInfo.Alternates.Length != 0)
        source.AddRange(((IEnumerable<OktaObjectLinkContent>) orgInfo.Alternates).Select<OktaObjectLinkContent, string>((Func<OktaObjectLinkContent, string>) (a => a.Link)));
      Uri result;
      return source.Any<string>((Func<string, bool>) (domain => Uri.TryCreate(domain, UriKind.Absolute, out result) && Uri.Compare(uri, result, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0));
    }
  }
}
