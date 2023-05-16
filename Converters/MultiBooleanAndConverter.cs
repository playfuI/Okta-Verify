// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Converters.MultiBooleanAndConverter
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Okta.Authenticator.NativeApp.Converters
{
  public class MultiBooleanAndConverter : MarkupExtension, IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values == null || values.Length == 0)
        return (object) false;
      foreach (object obj in values)
      {
        if (!this.IsBooleanTrue(obj))
          return (object) false;
      }
      return (object) true;
    }

    public object[] ConvertBack(
      object value,
      Type[] targetTypes,
      object parameter,
      CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => (object) this;

    private bool IsBooleanTrue(object obj) => obj is bool flag && flag;
  }
}
