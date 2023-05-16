// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.TemplateSelectors.EnrollAccountTemplateSelector
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Okta.Authenticator.NativeApp.TemplateSelectors
{
  public class EnrollAccountTemplateSelector : DataTemplateSelector
  {
    public Dictionary<EnrollViewState, DataTemplate> ContentTemplates { get; set; } = new Dictionary<EnrollViewState, DataTemplate>();

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      DataTemplate dataTemplate;
      return !(((container is ContentPresenter contentPresenter ? contentPresenter.TemplatedParent : (DependencyObject) null) is ContentControl templatedParent ? templatedParent.DataContext : (object) null) is BaseEnrollAccountViewModel dataContext) || !this.ContentTemplates.TryGetValue(dataContext.EnrollViewState, out dataTemplate) ? base.SelectTemplate(item, container) : dataTemplate;
    }
  }
}
