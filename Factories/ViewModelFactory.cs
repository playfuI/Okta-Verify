// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Factories.ViewModelFactory
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.ViewModels;
using System.ComponentModel;

namespace Okta.Authenticator.NativeApp.Factories
{
  public static class ViewModelFactory
  {
    public static INotifyPropertyChanged BuildEnrollmentViewModel(EnrollmentStateContext context)
    {
      if (context == null)
        return (INotifyPropertyChanged) null;
      switch (context.EnrollmentState)
      {
        case EnrollmentStateType.Manual:
          return (INotifyPropertyChanged) new ManualEnrollmentViewModel();
        case EnrollmentStateType.JustInTime:
          return (INotifyPropertyChanged) new JITEnrollmentViewModel(context.AccountDomain, context.AccountOrgId, context.AppName);
        case EnrollmentStateType.EnableUserVerification:
        case EnrollmentStateType.DisableUserVerification:
          string accountDomain1 = context.AccountDomain;
          string accountOrgId1 = context.AccountOrgId;
          string accountId1 = context.AccountId;
          string userEmail1 = context.UserEmail;
          string userId1 = context.UserId;
          string accountId2 = accountId1;
          int num = context.EnrollmentState == EnrollmentStateType.EnableUserVerification ? 1 : 0;
          return (INotifyPropertyChanged) new UserVerificationEnrollmentViewModel(accountDomain1, accountOrgId1, userEmail1, userId1, accountId2, num != 0);
        case EnrollmentStateType.ReEnroll:
          string accountDomain2 = context.AccountDomain;
          string accountId3 = context.AccountId;
          string accountOrgId2 = context.AccountOrgId;
          string accountId4 = accountId3;
          string userEmail2 = context.UserEmail;
          string userId2 = context.UserId;
          return (INotifyPropertyChanged) new ReEnrollViewModel(accountDomain2, accountOrgId2, accountId4, userEmail2, userId2);
        default:
          return (INotifyPropertyChanged) null;
      }
    }

    public static INotifyPropertyChanged BuildViewModel(ViewStateRequest viewState)
    {
      switch (viewState.ViewType)
      {
        case MainViewType.EnrollAccount:
          return ViewModelFactory.BuildEnrollmentViewModel(viewState.GetContext<EnrollmentStateContext>());
        case MainViewType.AccountDetails:
          IOktaAccount context1 = viewState.GetContext<IOktaAccount>();
          return context1 != null ? (INotifyPropertyChanged) new AccountDetailsViewModel(context1) : (INotifyPropertyChanged) null;
        case MainViewType.UpdateWindowsHelloSettings:
          AccountStateContext context2 = viewState.GetContext<AccountStateContext>();
          return context2 != null ? (INotifyPropertyChanged) new UpdateWindowsHelloSettingsViewModel(context2) : (INotifyPropertyChanged) null;
        case MainViewType.OriginMismatch:
          return (INotifyPropertyChanged) new PageBlockedViewModel();
        default:
          return (INotifyPropertyChanged) null;
      }
    }
  }
}
