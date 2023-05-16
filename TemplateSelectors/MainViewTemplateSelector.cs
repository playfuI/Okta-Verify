// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.TemplateSelectors.MainViewTemplateSelector
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Okta.Authenticator.NativeApp.TemplateSelectors
{
  public class MainViewTemplateSelector : DataTemplateSelector
  {
    public Dictionary<MainViewType, DataTemplate> ContentTemplates { get; set; } = new Dictionary<MainViewType, DataTemplate>();

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      Tuple<MainViewType, INotifyPropertyChanged> tuple = item as Tuple<MainViewType, INotifyPropertyChanged>;
      DataTemplate dataTemplate;
      return this.ContentTemplates.TryGetValue(item == null ? MainViewType.Unknown : tuple.Item1, out dataTemplate) ? dataTemplate : base.SelectTemplate(item, container);
    }
  }
}
