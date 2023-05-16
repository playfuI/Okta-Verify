// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.TelemetryExtensions
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Devices.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public static class TelemetryExtensions
  {
    public static IDictionary<string, string> ToStringPropertyBag<T>(this ILookup<T, object> data) => (IDictionary<string, string>) data.ToPropertyBag<T>().ToDictionary<KeyValuePair<T, string>, string, string>((Func<KeyValuePair<T, string>, string>) (x => x.Key.ToString()), (Func<KeyValuePair<T, string>, string>) (x => x.Value));

    public static IDictionary<T, string> ToPropertyBag<T>(this ILookup<T, object> data)
    {
      Dictionary<T, string> propertyBag = new Dictionary<T, string>();
      if (data == null || data.Count == 0)
        return (IDictionary<T, string>) propertyBag;
      foreach (IGrouping<T, object> source in (IEnumerable<IGrouping<T, object>>) data)
      {
        if (!string.IsNullOrEmpty(source.Key.ToString()))
          propertyBag.Add(source.Key, string.Join("||", source.Where<object>((Func<object, bool>) (o => !TelemetryExtensions.IsNullOrEmpty(o))).ToArray<object>()));
      }
      return (IDictionary<T, string>) propertyBag;
    }

    public static void TrackEvent(
      this IAnalyticsProvider provider,
      string name,
      params (string Key, object Value)[] parameters)
    {
      provider?.TrackEvent(name, ((IEnumerable<(string, object)>) parameters).ToLookup<(string, object), string, object>((Func<(string, object), string>) (d => d.Key), (Func<(string, object), object>) (d => d.Value)));
    }

    public static void TrackErrorWithLogsAndAppData(
      this IAnalyticsProvider provider,
      Exception ex,
      params (string FileName, string FileContent)[] additionalAttachements)
    {
      if (provider == null)
        return;
      ITelemetryDataManager telemetryDataManager = AppInjector.Get<ITelemetryDataManager>();
      if (telemetryDataManager == null)
        provider.TrackErrorWithLogs(ex, additionalAttachements);
      else
        provider.TrackErrorWithLogs(ex, telemetryDataManager.GetAllTelemetryDataPointsAsString(), additionalAttachements);
    }

    public static void TrackErrorWithLogs(
      this IAnalyticsProvider provider,
      string message,
      int hResult,
      [CallerMemberName] string sourceMethodName = null)
    {
      provider?.TrackErrorWithLogs((Exception) new OktaErrorReportException(message, hResult, sourceMethodName));
    }

    public static void TrackErrorWithLogsAndAppData(
      this IAnalyticsProvider provider,
      string message,
      int hResult,
      [CallerMemberName] string sourceMethodName = null)
    {
      if (provider == null)
        return;
      provider.TrackErrorWithLogsAndAppData((Exception) new OktaErrorReportException(message, hResult, sourceMethodName));
    }

    public static (string name, IDictionary<string, string> data) GetEventForException(
      AppTelemetryOperation operation,
      Exception exception = null,
      string stackTrace = null,
      string errorId = null,
      IDictionary<string, string> data = null,
      (string, string)[] fileAttachements = null,
      int attachementCount = 0)
    {
      string str1 = exception?.Message;
      if (string.IsNullOrEmpty(str1))
      {
        string str2 = fileAttachements != null ? ((IEnumerable<(string, string)>) fileAttachements).FirstOrDefault<(string, string)>((Func<(string, string), bool>) (a => a.Item1.EndsWith(".txt"))).Item2 : (string) null;
        if (str2 != null)
          str1 = ((IEnumerable<string>) str2.Split('\n')).Last<string>();
      }
      if (string.IsNullOrEmpty(str1))
      {
        string str3;
        if (!string.IsNullOrEmpty(stackTrace))
          str3 = ((IEnumerable<string>) stackTrace.Split('\n')).First<string>();
        else
          str3 = "Unknown error";
        str1 = str3;
      }
      IDictionary<string, string> dataBag = (IDictionary<string, string>) new Dictionary<string, string>();
      dataBag.AddTelemetryData(AppTelemetryDataKey.ErrorGroup, (object) str1);
      dataBag.AddTelemetryData(AppTelemetryDataKey.ErrorStackTrace, (object) (stackTrace ?? exception?.StackTrace));
      dataBag.AddTelemetryData(AppTelemetryDataKey.ErrorAttachmentCount, (object) (fileAttachements != null ? fileAttachements.Length : attachementCount));
      Exception innerException;
      if (exception.TryGetInnerException(out innerException))
        dataBag.AddTelemetryData(AppTelemetryDataKey.ErrorRootGroup, (object) innerException.Message);
      if (!string.IsNullOrEmpty(errorId))
        dataBag.AddTelemetryData(AppTelemetryDataKey.ErrorReportId, (object) errorId);
      dataBag.MergeTelemetryData((IEnumerable<KeyValuePair<string, string>>) (data ?? AppInjector.Get<ITelemetryDataManager>()?.GetAllTelemetryDataPointsAsString()));
      return (operation.ToString(), dataBag);
    }

    public static bool ShouldReport(this Exception error, out Exception normalizedError)
    {
      normalizedError = error;
      switch (error)
      {
        case OktaVerifyAppException verifyAppException when verifyAppException.SkipAnalyticsReport:
          return false;
        case HttpRequestException requestException when requestException.InnerException is WebException innerException:
          if (innerException.Status == WebExceptionStatus.ConnectFailure)
          {
            DevicesSdk.Telemetry.WriteWarningEx("Failed to connect, endpoint cannot be connected to.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\TelemetryExtensions.cs", nameof (ShouldReport));
            return false;
          }
          if (innerException.Status == WebExceptionStatus.NameResolutionFailure)
          {
            DevicesSdk.Telemetry.WriteWarningEx("Failed to resolve endpoint, check your internet connection.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\TelemetryExtensions.cs", nameof (ShouldReport));
            return false;
          }
          normalizedError = (Exception) innerException;
          break;
      }
      return true;
    }

    private static bool IsNullOrEmpty(object obj) => obj == null || obj is string str && str.Length == 0;

    private static void MergeTelemetryData(
      this IDictionary<string, string> dataBag,
      IEnumerable<KeyValuePair<string, string>> additionalData)
    {
      if (dataBag == null || additionalData == null)
        return;
      foreach (KeyValuePair<string, string> keyValuePair in additionalData)
        dataBag.AddTelemetryData(keyValuePair.Key, (object) keyValuePair.Value);
    }

    private static void AddTelemetryData(
      this IDictionary<string, string> dataBag,
      AppTelemetryDataKey key,
      object value,
      int maxLength = 255)
    {
      dataBag.AddTelemetryData(key.ToString(), value, maxLength);
    }

    private static void AddTelemetryData(
      this IDictionary<string, string> dataBag,
      string key,
      object value,
      int maxLength = 255)
    {
      if (dataBag == null)
        return;
      string key1 = key.ToString();
      string str1;
      string str2 = !dataBag.TryGetValue(key1, out str1) ? value?.ToString() : string.Format("{0} || {1}", (object) str1, value);
      if (str2 != null && str2.Length > maxLength)
        str2 = str2.Substring(0, maxLength - 3) + "...";
      dataBag.Add(key1, str2);
    }

    private static bool TryGetInnerException(this Exception exception, out Exception innerException)
    {
      innerException = (Exception) null;
      bool innerException1 = false;
      while ((exception = exception?.InnerException) != null)
      {
        innerException1 = true;
        innerException = exception;
      }
      return innerException1;
    }
  }
}
