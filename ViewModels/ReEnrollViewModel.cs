// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.ReEnrollViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Devices.SDK.Extensions;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class ReEnrollViewModel : EnrollAccountViewModel
  {
    private readonly string accountId;
    private readonly string userEmail;
    private readonly string userId;

    public ReEnrollViewModel(
      string orgDomain,
      string orgId,
      string accountId,
      string userEmail,
      string userId)
    {
      string orgId1 = orgId;
      // ISSUE: explicit constructor call
      base.\u002Ector(orgDomain, orgId1);
      this.accountId = accountId;
      this.userEmail = userEmail;
      this.userId = userId;
      this.InitializeEnrollment();
    }

    protected override bool IsNewEnrollment => false;

    protected override async Task<SignInResult> StartSignIn()
    {
      ReEnrollViewModel reEnrollViewModel = this;
      SignInResult signInResult;
      using (SignInViewModel viewModel = new SignInViewModel(reEnrollViewModel.SignInUrl, reEnrollViewModel.userEmail, reEnrollViewModel.userId, reEnrollViewModel.OrgId))
        signInResult = await reEnrollViewModel.SignIn(EnrollViewState.RedirectedToBrowser, viewModel).ConfigureAwait(true);
      return signInResult;
    }

    protected override async Task<bool> AnyOtherAccountsForOrg(string orgId) => await this.ClientAccountManager.AnyAccountsAsync((Func<IOktaAccount, bool>) (a => a.OrgId == orgId && a.AccountId != this.accountId)).ConfigureAwait(true);

    protected override async Task StartEnrollment(
      EnrollmentParameters enrollmentParameters,
      bool uvRequired)
    {
      ReEnrollViewModel reEnrollViewModel = this;
      try
      {
        await reEnrollViewModel.InitiateEnrollmentWithBiometrics(enrollmentParameters, uvRequired).ConfigureAwait(true);
      }
      catch (Exception ex) when (!ex.IsCritical(reEnrollViewModel.Logger))
      {
        reEnrollViewModel.Logger.WriteException("An error occurred while re-enrolling an account for " + reEnrollViewModel.OrgId, ex);
        reEnrollViewModel.AnalyticsProvider.TrackErrorWithLogs(ex);
      }
    }

    protected override async Task<(IOktaAccount, EnrollmentError)> RequestAccountEnrollment(
      EnrollmentParameters parameters,
      bool withBiometrics,
      bool setAsDefault)
    {
      ReEnrollViewModel reEnrollViewModel = this;
      return await reEnrollViewModel.ClientAccountManager.ReEnrollAccount(reEnrollViewModel.accountId, parameters, withBiometrics, setAsDefault).ConfigureAwait(true);
    }

    protected override async Task ResetFailedEnrollment(string signInUrl, string errorMessage)
    {
      ReEnrollViewModel reEnrollViewModel = this;
      reEnrollViewModel.Logger.WriteInfoEx("The re-enrollment for " + signInUrl + " failed.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\ReEnrollViewModel.cs", nameof (ResetFailedEnrollment));
      reEnrollViewModel.FailEnrollment(errorMessage);
    }
  }
}
