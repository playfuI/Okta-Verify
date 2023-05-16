// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Extensions
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Properties;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Extensions.Error;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp
{
  public static class Extensions
  {
    private static readonly Lazy<bool> LazyTestBuildCheck = new Lazy<bool>(new Func<bool>(Okta.Authenticator.NativeApp.Extensions.CheckIsTestBuild), true);

    public static void WriteException(this ILogger logger, string message, Exception ex)
    {
      ex.EnsureNotNull(nameof (ex));
      Okta.Authenticator.NativeApp.Extensions.WriteException((Action<string>) (m => logger.WriteErrorEx(m, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (WriteException))), message, ex);
    }

    public static void WriteExceptionAsWarning(this ILogger logger, string message, Exception ex)
    {
      ex.EnsureNotNull(nameof (ex));
      Okta.Authenticator.NativeApp.Extensions.WriteException((Action<string>) (m => logger.WriteWarningEx(m, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (WriteExceptionAsWarning))), message, ex);
    }

    public static bool NormalizeWebAddress(string urlOrHost, out string validURL)
    {
      validURL = urlOrHost?.Trim();
      if (string.IsNullOrEmpty(validURL))
        return false;
      validURL = validURL.ToLower(CultureInfo.InvariantCulture);
      if (validURL.StartsWith("http"))
      {
        if (!validURL.StartsWith("https"))
          validURL = "https" + validURL.Substring(4);
      }
      else
        validURL = "https://" + validURL;
      return true;
    }

    public static string GetValueOrDefault(this IEnumerable<Claim> claims, string type) => claims.Where<Claim>((Func<Claim, bool>) (i => i.Type.Equals(type, StringComparison.Ordinal))).FirstOrDefault<Claim>()?.Value;

    public static bool SafeDispose(this object disposable)
    {
      if (!(disposable is IDisposable disposable1))
        return false;
      try
      {
        disposable1.Dispose();
      }
      catch (ObjectDisposedException ex)
      {
      }
      return true;
    }

    internal static void TrackErrorWithLogs(
      this IAnalyticsProvider analyticsManager,
      string errorMessage,
      StackTrace errorTrace,
      int hResult = -2147467259,
      [CallerMemberName] string sourceMethodName = null)
    {
      analyticsManager.TrackErrorWithLogs((Exception) new OktaErrorReportException(errorMessage, hResult, errorTrace, sourceMethodName));
    }

    internal static void TrackErrorWithLogs(
      this IAnalyticsProvider analyticsManager,
      string errorMessage,
      int hResult = -2147467259,
      [CallerMemberName] string sourceMethodName = null)
    {
      analyticsManager.TrackErrorWithLogs((Exception) new OktaErrorReportException(errorMessage, hResult, sourceMethodName));
    }

    internal static void TrackErrorWithLogsAndAppData(
      this IAnalyticsProvider analyticsManager,
      string errorMessage,
      StackTrace errorTrace,
      int hResult = -2147467259,
      [CallerMemberName] string sourceMethodName = null)
    {
      analyticsManager.TrackErrorWithLogsAndAppData((Exception) new OktaErrorReportException(errorMessage, hResult, errorTrace, sourceMethodName));
    }

    internal static void TrackErrorWithLogsAndAppData(
      this IAnalyticsProvider analyticsManager,
      string errorMessage,
      int hResult = -2147467259,
      [CallerMemberName] string sourceMethodName = null)
    {
      analyticsManager.TrackErrorWithLogsAndAppData((Exception) new OktaErrorReportException(errorMessage, hResult, sourceMethodName));
    }

    internal static bool IsCritical(this Exception ex, ILogger logger = null)
    {
      switch (ex)
      {
        case OktaCriticalRuntimeException _:
        case StackOverflowException _:
        case OutOfMemoryException _:
        case ThreadAbortException _:
        case AccessViolationException _:
label_3:
          if (logger != null)
            logger.WriteErrorEx("Encountered critical exception: " + ex.GetType().Name + ":" + ex.Message + ":" + (Okta.Authenticator.NativeApp.Extensions.IsDebugMode ? ex.StackTrace : string.Empty), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (IsCritical));
          return true;
        default:
          if (Okta.Authenticator.NativeApp.Extensions.IsDebugMode)
          {
            switch (ex)
            {
              case NullReferenceException _:
              case IndexOutOfRangeException _:
                goto label_3;
            }
          }
          return false;
      }
    }

    internal static void LogAndReportToAnalytics(
      this ILogger logger,
      string reportMessage,
      IAnalyticsProvider analytics)
    {
      if (logger != null)
        logger.WriteErrorEx(reportMessage, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (LogAndReportToAnalytics));
      logger.ReportToAnalytics((Action<IAnalyticsProvider>) (a => a.TrackErrorWithLogsAndAppData(reportMessage, sourceMethodName: nameof (LogAndReportToAnalytics))), analytics);
    }

    internal static void LogAndReportToAnalytics(
      this ILogger logger,
      string reportMessage,
      Exception ex,
      IAnalyticsProvider analytics)
    {
      if (logger != null)
        logger.WriteException(reportMessage, ex);
      logger.ReportToAnalytics((Action<IAnalyticsProvider>) (a => a.TrackErrorWithLogsAndAppData(ex)), analytics);
    }

    private static void ReportToAnalytics(
      this ILogger logger,
      Action<IAnalyticsProvider> report,
      IAnalyticsProvider analyticsManager)
    {
      if (analyticsManager == null)
      {
        if (logger == null)
          return;
        logger.WriteErrorEx("Failed to report to analytics provider.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (ReportToAnalytics));
      }
      else
        report(analyticsManager);
    }

    internal static void AsBackgroundTask(this Task task, string operation) => task.AsBackgroundTask(operation, AppInjector.Get<ILogger>());

    internal static void AsBackgroundTask(
      this Task task,
      string operation,
      ILogger logger,
      IAnalyticsProvider analytics = null,
      Okta.Authenticator.NativeApp.Extensions.BackgroundTaskHandler handler = null)
    {
      task?.ContinueWith((Action<Task>) (t =>
      {
        if (handler == null)
          Okta.Authenticator.NativeApp.Extensions.UnobservedTaskContinuation(t, operation, logger, analytics);
        else
          handler(t, operation, logger, analytics);
      }), TaskScheduler.Default);
    }

    internal static bool IsDebugMode => false;

    internal static bool IsTestBuild => Okta.Authenticator.NativeApp.Extensions.LazyTestBuildCheck.Value;

    internal static bool AllowLogPersonalInformation { get; set; } = false;

    private static void WriteException(Action<string> logAction, string message, Exception ex)
    {
      string str = ex is AggregateException exception ? exception.FlattenException().Message : ex.Message;
      if (Okta.Authenticator.NativeApp.Extensions.AllowLogPersonalInformation && Okta.Authenticator.NativeApp.Extensions.IsDebugMode)
        logAction(message + ". Exception: " + str + Environment.NewLine + ex.StackTrace);
      else
        logAction(message + ". Exception: " + str);
    }

    internal static void UnobservedTaskContinuation(
      Task task,
      string operation,
      ILogger logger,
      IAnalyticsProvider analytics)
    {
      string str = string.Format("Background task {0} doing {1} on thread {2}", (object) task.Id, (object) operation, (object) Thread.CurrentThread.ManagedThreadId);
      switch (task.Status)
      {
        case TaskStatus.RanToCompletion:
          if (logger == null)
            break;
          logger.WriteDebugEx(str + " finished successfully.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (UnobservedTaskContinuation));
          break;
        case TaskStatus.Canceled:
          if (logger == null)
            break;
          logger.WriteInfoEx(str + " was cancelled.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (UnobservedTaskContinuation));
          break;
        case TaskStatus.Faulted:
          Exception ex = task.Exception.FlattenException();
          if (logger != null)
            logger.WriteException(str + " failed.", ex);
          IAnalyticsProvider provider = analytics ?? AppInjector.Get<IAnalyticsProvider>();
          if (provider == null)
            break;
          provider.TrackErrorWithLogsAndAppData(ex);
          break;
        default:
          if (logger == null)
            break;
          logger.WriteWarningEx(string.Format("{0} is in unexpected state {1}.", (object) str, (object) task.Status), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (UnobservedTaskContinuation));
          break;
      }
    }

    internal static void UnobservedTaskContinuationLogErrorOnly(
      Task task,
      string operation,
      ILogger logger,
      IAnalyticsProvider analytics)
    {
      if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled)
        return;
      string str = string.Format("Background task {0} doing {1} on thread {2}", (object) task.Id, (object) operation, (object) Thread.CurrentThread.ManagedThreadId);
      if (task.Status == TaskStatus.Faulted)
      {
        Exception ex = task.Exception.FlattenException();
        if (logger != null)
          logger.WriteException(str + " failed.", ex);
        IAnalyticsProvider provider = analytics ?? AppInjector.Get<IAnalyticsProvider>();
        if (provider == null)
          return;
        provider.TrackErrorWithLogsAndAppData(ex);
      }
      else
      {
        if (logger == null)
          return;
        logger.WriteWarningEx(string.Format("{0} is in unexpected state {1}.", (object) str, (object) task.Status), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Extensions.cs", nameof (UnobservedTaskContinuationLogErrorOnly));
      }
    }

    private static bool CheckIsTestBuild()
    {
      if (BuildSettings.IsMainBuild)
        return false;
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      if (executingAssembly == (Assembly) null)
        return false;
      Module module = ((IEnumerable<Module>) executingAssembly.GetModules()).FirstOrDefault<Module>();
      return module != (Module) null && module.GetSignerCertificate() == null;
    }

    internal delegate void BackgroundTaskHandler(
      Task task,
      string operation,
      ILogger logger,
      IAnalyticsProvider analytics);
  }
}
