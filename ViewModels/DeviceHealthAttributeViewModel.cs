// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.DeviceHealthAttributeViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.Devices.SDK.Extensions;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class DeviceHealthAttributeViewModel : BaseViewModel
  {
    private bool launchFromAccountListView;

    public DeviceHealthAttributeViewModel(
      string title,
      bool isHealthy,
      bool launchFromAccountListView,
      Visibility isHideSeparator = Visibility.Visible)
      : this(title, isHealthy, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, launchFromAccountListView, isHideSeparator)
    {
    }

    public DeviceHealthAttributeViewModel(
      string title,
      bool isHealthy,
      string description,
      string remediationLabel = null,
      string remediationLink = null,
      string currentStateHint = null,
      string remediationHint = null,
      bool launchFromAccountListView = false,
      Visibility isHideSeparator = Visibility.Visible)
    {
      this.AttributeTitle = title;
      this.IsHealthy = isHealthy;
      this.AttributeDescription = description;
      this.CurrentStateHint = currentStateHint ?? string.Empty;
      this.RemediationHint = remediationHint ?? string.Empty;
      this.RemediationLabel = remediationLabel ?? string.Empty;
      this.RemediationLink = remediationLink ?? string.Empty;
      this.launchFromAccountListView = launchFromAccountListView;
      this.RemediationCommand = (ICommand) new DelegateCommand(new Action(this.Remediate));
      this.IsHideSeparator = isHideSeparator;
    }

    public Visibility IsHideSeparator { get; set; }

    public string AttributeTitle { get; }

    public string AttributeDescription { get; }

    public bool IsHealthy { get; }

    public string CurrentStateHint { get; }

    public string RemediationHint { get; }

    public string RemediationLabel { get; }

    public string RemediationLink { get; }

    public ICommand RemediationCommand { get; }

    public bool IsRemediationHintDisplayed => !string.IsNullOrEmpty(this.RemediationHint);

    public bool IsCurrentStateHintDisplayed => !string.IsNullOrEmpty(this.CurrentStateHint);

    public bool IsRemediationLinkAvailable => !string.IsNullOrEmpty(this.RemediationLink);

    private void Remediate()
    {
      if (!this.IsRemediationLinkAvailable)
        return;
      this.Logger.WriteInfoEx("Launching " + this.RemediationLink + "...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\DeviceHealthAttributeViewModel.cs", nameof (Remediate));
      this.LaunchLink(this.RemediationLink);
      MainViewStateEvent mainViewStateEvent = this.EventAggregator.GetEvent<MainViewStateEvent>();
      if (this.launchFromAccountListView)
        mainViewStateEvent.Publish(new MainViewState(MainViewType.DeviceHealth, (INotifyPropertyChanged) this, false));
      else
        mainViewStateEvent.Publish(new MainViewState(MainViewType.Settings, (INotifyPropertyChanged) new SettingsViewModel()));
    }
  }
}
