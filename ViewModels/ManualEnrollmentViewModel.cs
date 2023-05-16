// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.ManualEnrollmentViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Devices.SDK.Extensions;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class ManualEnrollmentViewModel : EnrollAccountViewModel
  {
    public ManualEnrollmentViewModel()
      : base((string) null, (string) null)
    {
      this.SignInUrl = this.SignInManager.DefaultSignInUrl;
      this.InitializeEnrollment();
    }

    protected override async Task<SignInResult> StartSignIn()
    {
      ManualEnrollmentViewModel enrollmentViewModel = this;
      SignInResult signInResult = new SignInResult();
      try
      {
        while (true)
        {
          using (SignInViewModel viewModel = new SignInViewModel(enrollmentViewModel.SignInUrl))
            signInResult = await enrollmentViewModel.SignIn(EnrollViewState.SignInURL, viewModel).ConfigureAwait(true);
          if (!signInResult.IsSuccess)
          {
            if (signInResult.ErrorCode != SignInErrorCode.UserCancelled)
            {
              if (signInResult.ErrorCode != SignInErrorCode.UserAbandoned)
                enrollmentViewModel.ResetEnrollmentProcess(signInResult.ErrorMessage);
              else
                break;
            }
            else
              break;
          }
          else
            break;
        }
      }
      catch (Exception ex) when (!ex.IsCritical(enrollmentViewModel.Logger))
      {
        enrollmentViewModel.Logger.WriteException("An error occurred while enrolling a new account", ex);
        enrollmentViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      SignInResult signInResult1 = signInResult;
      signInResult = new SignInResult();
      return signInResult1;
    }

    protected override async Task StartEnrollment(
      EnrollmentParameters enrollmentParameters,
      bool uvRequired)
    {
      ManualEnrollmentViewModel enrollmentViewModel = this;
      try
      {
        await enrollmentViewModel.InitiateEnrollmentWithBiometrics(enrollmentParameters, uvRequired).ConfigureAwait(true);
      }
      catch (Exception ex) when (!ex.IsCritical(enrollmentViewModel.Logger))
      {
        enrollmentViewModel.Logger.WriteException("An error occurred while enrolling a new account", ex);
        enrollmentViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    protected override async Task ResetFailedEnrollment(string signInUrl, string errorMessage)
    {
      ManualEnrollmentViewModel enrollmentViewModel = this;
      enrollmentViewModel.Logger.WriteInfoEx("Failed to enroll the account associated with " + signInUrl + "; starting over...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\ManualEnrollmentViewModel.cs", nameof (ResetFailedEnrollment));
      enrollmentViewModel.ResetEnrollmentProcess(errorMessage, signInUrl);
      await enrollmentViewModel.InitializeEnrollmentFlows().ConfigureAwait(false);
    }

    private void ResetEnrollmentProcess(string errorMessage = null, string signUrl = null)
    {
      if (!string.IsNullOrEmpty(errorMessage))
        this.EventAggregator.GetEvent<BannerNotificationEvent>()?.Publish(new BannerNotification(BannerType.Error, errorMessage));
      this.EnrollmentInProcess = false;
      this.OrgId = (string) null;
      this.SignInUrl = signUrl ?? this.SignInManager.DefaultSignInUrl;
      this.UpdateCurrentState(EnrollViewState.Unknown, (BaseViewModel) null);
    }
  }
}
