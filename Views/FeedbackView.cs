// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Views.FeedbackView
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Prism.Events;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.Views
{
  public class FeedbackView : Window, IDisposable, IComponentConnector
  {
    private readonly AnalyticsEvent dialogViewStateEvent;
    private readonly IEventAggregator eventAggregator;
    private bool disposedValue;
    private bool _contentLoaded;

    public FeedbackView()
    {
      this.InitializeComponent();
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.dialogViewStateEvent = this.eventAggregator.GetEvent<AnalyticsEvent>();
      this.dialogViewStateEvent.Subscribe(new Action<AnalyticsEventData>(this.CloseDialog));
    }

    public void ShowCustomerReportWindow() => this.ShowDialog();

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposedValue)
        return;
      if (disposing)
        this.dialogViewStateEvent.Unsubscribe(new Action<AnalyticsEventData>(this.CloseDialog));
      this.disposedValue = true;
    }

    private void CloseDialog(AnalyticsEventData eventData)
    {
      bool flag1 = eventData.Type == AnalyticsEventType.ReportSubmitted;
      bool flag2 = eventData.Type == AnalyticsEventType.StatusChanged && !eventData.IsEnabled;
      if (!(flag1 | flag2))
        return;
      this.DialogResult = new bool?(!flag2 & flag1);
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
      if (this._contentLoaded)
        return;
      this._contentLoaded = true;
      Application.LoadComponent((object) this, new Uri("/OktaVerify;V4.0.2.0;component/ui/views/feedbackview.xaml", UriKind.Relative));
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void IComponentConnector.Connect(int connectionId, object target) => this._contentLoaded = true;
  }
}
