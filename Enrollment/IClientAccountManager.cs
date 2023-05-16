// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Enrollment.IClientAccountManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Devices.SDK;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Enrollment
{
  public interface IClientAccountManager
  {
    IEnumerable<IOktaAccount> Accounts { get; }

    bool TryGetAccount(string accounId, out IOktaAccount account);

    Task<IOktaAccount> TryGetAccount(Func<IOktaAccount, bool> filterPredicate);

    Task<IList<IOktaAccount>> GetAccounts(Func<IOktaAccount, bool> filterPredicate = null);

    bool AnyAccounts();

    Task<bool> AnyAccountsAsync(Func<IOktaAccount, bool> filterPredicate = null);

    Task<(IOktaAccount Account, EnrollmentError Error)> EnrollAccount(
      EnrollmentParameters enrollmentParameters,
      bool withUserVerification,
      bool setAsDefault = false);

    Task<(IOktaAccount Account, EnrollmentError Error)> ReEnrollAccount(
      string accountId,
      EnrollmentParameters enrollmentParameters,
      bool withUserVerification,
      bool setAsDefault = false);

    Task<EnrollmentEndEventArg> InvokeEnrollmentFlow(
      EnrollmentStartEventArg enrollmentArgs,
      bool waitForPending = false);

    Task<(OrganizationEnrollmentStatus Status, IOktaOrganization Org)> CheckOrganizationStatus(
      Uri orgUrl);

    Task<(Uri Url, string Id)> GetPrimaryOrganization();

    Task<IOktaAccount> GetDefaultAccount(
      string orgId,
      bool useAsDefaultIfOnlyOneEnrolled = true,
      bool pickOneIfNoDefaultSet = false);

    Task<AuthenticatorOperationResult> DeleteAccount(IOktaAccount account, bool localOnly = false);

    Task<AuthenticatorOperationResult> AddAccountUserVerification(
      string accountId,
      string accessToken);

    Task<bool> AddOrUpdateUserInformation(UserInformation userInformation);

    Task<bool> RemoveUserInformation(string userId);

    Task<AuthenticatorOperationResult> RemoveAccountUserVerification(IOktaAccount account);

    Task AccountsInitialization();

    Task<bool> IsAccountUserVerificationRequired(
      string orgId,
      string signInUrl,
      string accessToken,
      bool useCache = true);

    Task<bool> IsAccountUserVerificationRequired(IOktaAccount account, bool useCache = true);

    Task<bool> IsUserVerificationInvalidated(IOktaAccount account);

    bool IsAccountSetAsDefault(string accountId);

    (bool Dismissed, bool Critical) GetAccountErrorState(
      string accountId,
      AccountErrorStateTypes types);

    Task<bool> UpdateAccountWarningSettings(
      string accountId,
      AccountErrorStateTypes dismissedWarnings);

    Task<bool> UpdateAccountDefaultSettings(string accountId, bool isDefault);
  }
}
