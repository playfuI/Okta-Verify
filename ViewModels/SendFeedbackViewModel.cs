// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.SendFeedbackViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using Prism.Events;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class SendFeedbackViewModel : BaseViewModel, IDataErrorInfo
  {
    private const string ReportName = "CustomerReport";
    private readonly IEventAggregator eventAggregator;
    private readonly IAnalyticsProvider analyticsProvider;
    private ICommand sendFeedbackCommand;
    private ICommand closeWindowButtonClick;
    private string issueTitle;
    private string issueDescription;
    private string titleErrorMessage;
    private string descriptionErrorMessage;
    private bool titleModified;
    private bool descriptionModified;

    public SendFeedbackViewModel()
    {
      this.ViewState = SendFeedbackViewModelState.SendFeedback;
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.analyticsProvider = AppInjector.Get<IAnalyticsProvider>();
    }

    public SendFeedbackViewModelState ViewState { get; set; }

    public string IssueTitle
    {
      get => this.issueTitle;
      set
      {
        if (!(this.issueTitle != value))
          return;
        this.issueTitle = value;
        this.titleModified = true;
        this.FirePropertyChangedEvent(nameof (IssueTitle));
      }
    }

    public string TitleErrorMessage
    {
      get => this.titleErrorMessage;
      set
      {
        if (!(this.titleErrorMessage != value))
          return;
        this.titleErrorMessage = value;
        this.FirePropertyChangedEvent(nameof (TitleErrorMessage));
      }
    }

    public string UserEmail { get; set; }

    public string IssueDescription
    {
      get => this.issueDescription;
      set
      {
        if (!(this.issueDescription != value))
          return;
        this.issueDescription = value;
        this.descriptionModified = true;
        this.FirePropertyChangedEvent(nameof (IssueDescription));
      }
    }

    public string DescriptionErrorMessage
    {
      get => this.descriptionErrorMessage;
      set
      {
        if (!(this.descriptionErrorMessage != value))
          return;
        this.descriptionErrorMessage = value;
        this.FirePropertyChangedEvent(nameof (DescriptionErrorMessage));
      }
    }

    public ICommand SendFeedbackCommand
    {
      get
      {
        if (this.sendFeedbackCommand == null)
          this.sendFeedbackCommand = (ICommand) new DelegateCommand(new Action(this.SendFeedback));
        return this.sendFeedbackCommand;
      }
    }

    public ICommand CloseWindowButtonClick
    {
      get
      {
        if (this.closeWindowButtonClick == null)
          this.closeWindowButtonClick = (ICommand) new DelegateCommand(new Action(this.ReportSubmitted));
        return this.closeWindowButtonClick;
      }
    }

    public string Error => string.Empty;

    public string this[string columnName]
    {
      get
      {
        string str = string.Empty;
        switch (columnName)
        {
          case "IssueTitle":
            if (string.IsNullOrWhiteSpace(this.IssueTitle) && this.titleModified)
              str = Resources.FeedbackFormTitleEmptyMessage;
            this.TitleErrorMessage = str;
            break;
          case "IssueDescription":
            if (string.IsNullOrWhiteSpace(this.IssueDescription) && this.descriptionModified)
              str = Resources.FeedbackFormDescriptionEmptyMessage;
            else if (!string.IsNullOrEmpty(this.IssueDescription) && this.IssueDescription.Length > 800)
              str = Resources.FeedbackFormDescriptionLimitMessage;
            this.DescriptionErrorMessage = str;
            break;
        }
        return str;
      }
    }

    private bool ValidateFeedbackForm()
    {
      string[] strArray = new string[2]
      {
        "IssueTitle",
        "IssueDescription"
      };
      foreach (string columnName in strArray)
      {
        if (!string.IsNullOrEmpty(this[columnName]))
          return false;
      }
      return true;
    }

    private void SendFeedback()
    {
      if (!this.titleModified)
      {
        this.titleModified = true;
        this.FirePropertyChangedEvent("IssueTitle");
      }
      if (!this.descriptionModified)
      {
        this.descriptionModified = true;
        this.FirePropertyChangedEvent("IssueDescription");
      }
      if (!this.ValidateFeedbackForm())
        return;
      string str = "Problem summary: " + this.IssueTitle + "\n\n" + this.IssueDescription;
      if (!string.IsNullOrWhiteSpace(this.UserEmail))
        str = str + "\n\nUser email: " + this.UserEmail;
      if (this.analyticsProvider != null)
        this.analyticsProvider.TrackErrorWithLogsAndAppData((Exception) new OktaCustomerErrorReportException("Issues Reported by Customers: " + this.IssueTitle), ("CustomerReport", str));
      this.ViewState = SendFeedbackViewModelState.SubmittedFeedback;
      this.FirePropertyChangedEvent("ViewState");
    }

    private void ReportSubmitted() => this.eventAggregator.GetEvent<AnalyticsEvent>()?.Publish(new AnalyticsEventData(AnalyticsEventType.ReportSubmitted));
  }
}
