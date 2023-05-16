// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.SetAsDefaultAccountViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.MultilingualResources;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class SetAsDefaultAccountViewModel : AsyncInlineOperationViewModel<bool>
  {
    public SetAsDefaultAccountViewModel(string userEmail, string orgDomain)
    {
      this.UserEmail = userEmail;
      this.OrgDomain = orgDomain;
      this.Initialize();
    }

    public string UserEmail { get; }

    public string OrgDomain { get; }

    public string SetAsDefaultTitle { get; private set; }

    public string SetAsDefaultMessage { get; private set; }

    public string ConfirmSetAsDefaultText { get; private set; }

    public string SkipSetAsDefaultText { get; private set; }

    public string OrgInitials { get; private set; }

    public ICommand SetAsDefaultCommand { get; private set; }

    public ICommand SkipCommand { get; private set; }

    private void Skip() => this.TrySetResult(false);

    private void SetAsDefault() => this.TrySetResult(true);

    private void Initialize()
    {
      this.SetAsDefaultTitle = ResourceExtensions.ExtractCompositeResource(Resources.EnrollSetAsDefaultAccountTitle);
      this.SetAsDefaultMessage = Resources.EnrollSetAsDefaultAccountMessage;
      this.ConfirmSetAsDefaultText = Resources.EnrollSetAsDefaultButtonText;
      this.SkipSetAsDefaultText = Resources.EnrollSetAsDefaultSkipButtonText;
      this.OrgInitials = this.OrgDomain.Substring(0, 1).ToUpperInvariant();
      this.SetAsDefaultCommand = (ICommand) new DelegateCommand(new Action(this.SetAsDefault));
      this.SkipCommand = (ICommand) new DelegateCommand(new Action(this.Skip));
      this.FireViewModelChangedEvent();
    }
  }
}
