// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Views.NotificationBannerView
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace Okta.Authenticator.NativeApp.Views
{
  public class NotificationBannerView : UserControl, IComponentConnector
  {
    internal DockPanel NotificationPanel;
    private bool _contentLoaded;

    public NotificationBannerView() => this.InitializeComponent();

    private void Storyboard_Completed(object sender, EventArgs e)
    {
      if ((sender is ClockGroup clockGroup ? clockGroup.Timeline : (TimelineGroup) null) is Storyboard timeline)
        timeline.Stop();
      this.NotificationPanel.IsEnabled = false;
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
      if (this._contentLoaded)
        return;
      this._contentLoaded = true;
      Application.LoadComponent((object) this, new Uri("/OktaVerify;V4.0.2.0;component/ui/views/notificationbannerview.xaml", UriKind.Relative));
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void IComponentConnector.Connect(int connectionId, object target)
    {
      if (connectionId != 1)
      {
        if (connectionId == 2)
          this.NotificationPanel = (DockPanel) target;
        else
          this._contentLoaded = true;
      }
      else
        ((Timeline) target).Completed += new EventHandler(this.Storyboard_Completed);
    }
  }
}
