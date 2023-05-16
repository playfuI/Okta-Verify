// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Converters.IsDisabledToOpacityConverter
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK.Extensions;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.Converters
{
  [ValueConversion(typeof (bool), typeof (double))]
  public class IsDisabledToOpacityConverter : MarkupExtension, IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      targetType.EnsureNotNull(nameof (targetType));
      if (targetType != typeof (double))
        throw new InvalidOperationException("Cannot convert from " + targetType.Name + " to Opacity");
      return (object) ((bool) value ? 0.2 : 1.0);
    }

    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      targetType.EnsureNotNull(nameof (targetType));
      if (targetType != typeof (bool))
        throw new InvalidOperationException("Cannot convert from " + targetType.Name + " to Opacity");
      return (object) ((double) value < 0.5);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => (object) this;
  }
}
