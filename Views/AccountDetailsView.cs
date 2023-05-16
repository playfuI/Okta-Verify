﻿// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Views.AccountDetailsView
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

namespace Okta.Authenticator.NativeApp.Views
{
  public class AccountDetailsView : UserControl, IComponentConnector
  {
    internal MenuItem WarningArea;
    internal Image AccountLogo;
    internal Control MainActionCompositeButton;
    private bool _contentLoaded;

    public AccountDetailsView() => this.InitializeComponent();

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public void InitializeComponent()
    {
      if (this._contentLoaded)
        return;
      this._contentLoaded = true;
      Application.LoadComponent((object) this, new Uri("/OktaVerify;V4.0.2.0;component/ui/views/accountdetailsview.xaml", UriKind.Relative));
    }

    [DebuggerNonUserCode]
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    void IComponentConnector.Connect(int connectionId, object target)
    {
      switch (connectionId)
      {
        case 1:
          this.WarningArea = (MenuItem) target;
          break;
        case 2:
          this.AccountLogo = (Image) target;
          break;
        case 3:
          this.MainActionCompositeButton = (Control) target;
          break;
        default:
          this._contentLoaded = true;
          break;
      }
    }
  }
}
