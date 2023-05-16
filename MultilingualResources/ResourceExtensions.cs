// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.MultilingualResources.ResourceExtensions
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Okta.Authenticator.NativeApp.MultilingualResources
{
  public static class ResourceExtensions
  {
    private const string PlaceholderRegexMatch = "\\{(\\d+)\\}";
    private const string SinglePlaceholder = "{0}";
    private const int SinglePlaceholderLength = 3;
    private const char OpeningBracket = '{';
    private const char ClosingBracket = '}';

    public static string[] GetPartsWithPlaceholders(string resource) => resource == null ? (string[]) null : Regex.Split(resource, "\\{(\\d+)\\}");

    public static string[] GetPartsWithoutPlaceholder(string resource) => resource == null ? (string[]) null : Regex.Matches(resource, "\\{(\\d+)\\}").OfType<Match>().Select<Match, string>((Func<Match, string>) (m => m.Groups[0].Value)).ToArray<string>();

    public static string[] GetExactPartsWithoutPlaceholder(string resource, int count)
    {
      string[] withoutPlaceholder = ResourceExtensions.GetPartsWithoutPlaceholder(resource);
      if (withoutPlaceholder.Length == count)
        return withoutPlaceholder;
      throw new ArgumentException(string.Format("Resource string \"{0}\" cannot be split into {1} substrings.", (object) resource, (object) count));
    }

    public static (string, string) GetTwoPartsWithoutPlaceholder(string resource)
    {
      if (resource == null)
        return ((string) null, (string) null);
      int length = resource.IndexOf("{0}", StringComparison.CurrentCultureIgnoreCase);
      if (length < 0)
        return ((string) null, resource);
      if (length == 0)
        return ((string) null, resource.Substring(3));
      return length >= resource.Length - 3 ? (resource, (string) null) : (resource.Substring(0, length), resource.Substring(length + 3));
    }

    public static string ExtractCompositeResource(string compositeResource, params string[] args)
    {
      try
      {
        StringBuilder stringBuilder = new StringBuilder(compositeResource.CultureFormat((object[]) args));
        string compositeResource1 = stringBuilder.ToString();
        int startIndex = 0;
        while (startIndex < stringBuilder.Length)
        {
          int num1 = compositeResource1.IndexOf('{', startIndex);
          if (num1 != -1)
          {
            int num2 = compositeResource1.IndexOf('}', num1 + 1);
            string name = compositeResource1.Substring(num1 + 1, num2 - num1 - 1);
            string str = Resources.ResourceManager.GetString(name, Resources.Culture);
            stringBuilder = stringBuilder.Remove(num1, name.Length + 2).Insert(num1, str);
            startIndex = num1 + str.Length;
            compositeResource1 = stringBuilder.ToString();
          }
          else
            break;
        }
        return compositeResource1;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        return string.Empty;
      }
    }

    public static string CultureFormat(this string resource, params object[] parameters)
    {
      if (resource == null)
        return (string) null;
      try
      {
        return string.Format((IFormatProvider) Resources.Culture, resource, parameters);
      }
      catch (FormatException ex) when (!Extensions.IsTestBuild)
      {
        return resource;
      }
    }
  }
}
