// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Views.AccountsListView
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.Views
{
  public class AccountsListView : UserControl, IComponentConnector
  {
    internal ListBox Accounts;
    internal Button UseAnotherAccountsButton;
    private bool _contentLoaded;

    public AccountsListView() => this.InitializeComponent();

    public CustomPopupPlacement[] PlaceAccountMenuPopup(Size popupSize, Size targetSize, Point p) => new CustomPopupPlacement[1]
    {
      new CustomPopupPlacement(new Point(p.X - popupSize.Width + targetSize.Width, p.Y), PopupPrimaryAxis.Horizontal)
    };

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
      if (this._contentLoaded)
        return;
      this._contentLoaded = true;
      Application.LoadComponent((object) this, new Uri("/OktaVerify;V4.0.2.0;component/ui/views/accountslistview.xaml", UriKind.Relative));
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void IComponentConnector.Connect(int connectionId, object target)
    {
      if (connectionId != 1)
      {
        if (connectionId == 2)
          this.UseAnotherAccountsButton = (Button) target;
        else
          this._contentLoaded = true;
      }
      else
        this.Accounts = (ListBox) target;
    }
  }
}
