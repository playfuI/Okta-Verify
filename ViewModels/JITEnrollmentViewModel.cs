// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.JITEnrollmentViewModel
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
  public class JITEnrollmentViewModel : EnrollAccountViewModel
  {
    private readonly string appName;

    public JITEnrollmentViewModel(string orgDomain, string orgId, string appName)
      : base(orgDomain, orgId)
    {
      this.appName = appName;
      this.InitializeEnrollment();
    }

    protected override async Task StartEnrollment(
      EnrollmentParameters enrollmentParameters,
      bool uvRequired)
    {
      JITEnrollmentViewModel enrollmentViewModel = this;
      try
      {
        await enrollmentViewModel.InitiateEnrollmentWithBiometrics(enrollmentParameters, uvRequired).ConfigureAwait(true);
      }
      catch (Exception ex) when (!ex.IsCritical(enrollmentViewModel.Logger))
      {
        enrollmentViewModel.Logger.WriteException("An error occurred while enrolling a new account for " + enrollmentViewModel.appName, ex);
        enrollmentViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    protected override async Task ResetFailedEnrollment(string signInUrl, string errorMessage)
    {
      JITEnrollmentViewModel enrollmentViewModel = this;
      enrollmentViewModel.Logger.WriteInfoEx("The JIT enrollment for " + signInUrl + " failed.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\JITEnrollmentViewModel.cs", nameof (ResetFailedEnrollment));
      enrollmentViewModel.FailEnrollment(errorMessage);
    }

    protected override async Task<SignInResult> StartSignIn()
    {
      JITEnrollmentViewModel enrollmentViewModel = this;
      Uri result;
      string signInUrl = Uri.TryCreate(enrollmentViewModel.SignInUrl, UriKind.Absolute, out result) ? result.DnsSafeHost : enrollmentViewModel.SignInUrl;
      if (enrollmentViewModel.ClientAccountManager.AnyAccounts())
      {
        using (AccountNotFoundViewModel viewModel = new AccountNotFoundViewModel(signInUrl, enrollmentViewModel.appName))
          return await enrollmentViewModel.SignIn(EnrollViewState.AccountNotFound, (SignInViewModel) viewModel).ConfigureAwait(false);
      }
      else
      {
        JITOnboardingViewModel parentViewModel = new JITOnboardingViewModel(signInUrl);
        return await enrollmentViewModel.SignIn(EnrollViewState.JITOnboarding, parentViewModel.SignInViewModel, (BaseViewModel) parentViewModel).ConfigureAwait(false);
      }
    }
  }
}
