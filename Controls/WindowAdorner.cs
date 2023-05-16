// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Controls.WindowAdorner
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Okta.Authenticator.NativeApp.Controls
{
  public class WindowAdorner : Adorner
  {
    private readonly VisualCollection visuals;
    private readonly ContentPresenter contentPresenter;

    internal WindowAdorner(UIElement adornedElement, UserControl content)
      : base(adornedElement)
    {
      this.contentPresenter = new ContentPresenter()
      {
        Content = (object) content
      };
      this.visuals = this.InitializeVisuals(this.contentPresenter);
    }

    public static WindowAdorner ShowContent(UserControl content, Window parentWindow)
    {
      if (content == null || parentWindow == null)
        return (WindowAdorner) null;
      UIElement content1 = parentWindow.Content as UIElement;
      AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer((Visual) content1);
      if (adornerLayer == null)
        return (WindowAdorner) null;
      WindowAdorner windowAdorner = new WindowAdorner(content1, content);
      adornerLayer.Add((Adorner) windowAdorner);
      return windowAdorner;
    }

    public void RemoveContent() => AdornerLayer.GetAdornerLayer((Visual) this.AdornedElement)?.Remove((Adorner) this);

    protected override Size ArrangeOverride(Size finalSize)
    {
      this.contentPresenter.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
      return base.ArrangeOverride(finalSize);
    }

    protected override Size MeasureOverride(Size constraint)
    {
      this.contentPresenter.Measure(constraint);
      return base.MeasureOverride(constraint);
    }

    protected override int VisualChildrenCount => this.visuals.Count;

    protected override Visual GetVisualChild(int index) => this.visuals[index];

    private VisualCollection InitializeVisuals(ContentPresenter content) => new VisualCollection((Visual) this)
    {
      (Visual) content
    };
  }
}
