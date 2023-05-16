// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Converters.NegateBooleanConverter
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.Converters
{
  [ValueConversion(typeof (bool), typeof (bool))]
  public class NegateBooleanConverter : MarkupExtension, IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (object) this.Convert(value, targetType);

    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      return (object) this.Convert(value, targetType);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => (object) this;

    private bool Convert(object obj, Type t)
    {
      if (t != typeof (bool))
        throw new InvalidOperationException("Only bool values are supported.");
      return !(bool) obj;
    }
  }
}
