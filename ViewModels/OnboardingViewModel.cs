// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.OnboardingViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using Prism.Events;
using System;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class OnboardingViewModel : BaseViewModel
  {
    private readonly IEventAggregator eventAggregator;
    private OnboardingStep currentStep = OnboardingStep.Welcome;

    public OnboardingViewModel()
    {
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.GetStartedCommand = (ICommand) new DelegateCommand(new Action(this.GetStarted));
      this.AddAccountCommand = (ICommand) new DelegateCommand(new Action(this.AddAccount));
      this.BackToWelcomeCommand = (ICommand) new DelegateCommand(new Action(this.BackToWelcome));
      (this.DetailsMessageBeforeButtonName, this.DetailsMessageAfterButtonName) = ResourceExtensions.GetTwoPartsWithoutPlaceholder(Resources.OnboardingHowItWorksMessage);
    }

    protected OnboardingViewModel(string signInUrl)
      : this()
    {
      this.SignInUrl = signInUrl;
    }

    public ICommand GetStartedCommand { get; }

    public ICommand AddAccountCommand { get; }

    public ICommand BackToWelcomeCommand { get; }

    public string DetailsMessageBeforeButtonName { get; }

    public string DetailsMessageAfterButtonName { get; }

    public string SignInUrl { get; }

    public OnboardingStep CurrentStep
    {
      get => this.currentStep;
      protected set
      {
        this.currentStep = value;
        this.FirePropertyChangedEvent(nameof (CurrentStep));
      }
    }

    protected virtual void AddAccount() => this.eventAggregator.GetEvent<AccountEnrollStartEvent>()?.Publish(EnrollmentStartEventArg.AsManualEnrollmentRequest());

    protected virtual void BackToWelcome() => this.CurrentStep = OnboardingStep.Welcome;

    private void GetStarted() => this.CurrentStep = OnboardingStep.GetStarted;
  }
}
