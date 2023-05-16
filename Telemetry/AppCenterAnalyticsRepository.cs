// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.AppCenterAnalyticsRepository
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.AppCenter.Crashes;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public class AppCenterAnalyticsRepository : AnalyticsRepositoryBase
  {
    public AppCenterAnalyticsRepository(
      ILogger logger,
      IConfigurationManager configManager,
      IClientStorageManager storageManager,
      IEventAggregator aggregator)
      : base(logger, configManager, storageManager, aggregator)
    {
    }

    public override void TrackEvent(string name, IDictionary<string, string> data = null) => this.QueueReport((Action) (() => Microsoft.AppCenter.Analytics.Analytics.TrackEvent(name, data)));

    public override void TrackError(
      Exception exception,
      IDictionary<string, string> data = null,
      params (string, string)[] fileAttachements)
    {
      this.QueueReport((Action) (() =>
      {
        Exception exception1 = exception ?? (Exception) new OktaErrorReportException("Attempted to track error while parameter exception null.", nameof (TrackError));
        Microsoft.AppCenter.Crashes.Crashes.TrackError(exception1, data, AppCenterAnalyticsRepository.GetLogsAndAttachments(this.CollectMemoryLogs(), fileAttachements).ToArray<ErrorAttachmentLog>());
        this.Logger.WriteInfoEx("=== Reported " + exception1.GetType().Name + " to App center ===", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (TrackError));
        (string name, IDictionary<string, string> data) eventForException = TelemetryExtensions.GetEventForException(AppTelemetryOperation.ErrorReport, exception1, data: data, fileAttachements: fileAttachements);
        Microsoft.AppCenter.Analytics.Analytics.TrackEvent(eventForException.name, eventForException.data);
      }));
    }

    public override async Task<bool> IsEnabled()
    {
      AppCenterAnalyticsRepository analyticsRepository = this;
      if (analyticsRepository.InitializationTask.Task.IsCompleted)
      {
        // ISSUE: reference to a compiler-generated method
        return await Task.Run<bool>(new Func<Task<bool>>(analyticsRepository.\u003CIsEnabled\u003Eb__3_0));
      }
      analyticsRepository.Logger.WriteWarningEx("Analytics still initializing; skip status check", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (IsEnabled));
      return false;
    }

    public override async Task<bool> Configure(bool enable)
    {
      bool updated = await base.Configure(enable).ConfigureAwait(false);
      if (updated)
        await this.UpdateAppCenterState(enable).ConfigureAwait(false);
      return updated;
    }

    public override void SetUserId(string userId) => this.QueueReport((Action) (() => Microsoft.AppCenter.AppCenter.SetUserId(userId)));

    protected override async Task<bool> InitializeAnalyticsState(bool reportToAnalyticsServer)
    {
      AppCenterAnalyticsRepository analyticsRepository = this;
      analyticsRepository.Logger.WriteInfoEx(string.Format("Initializing App Center services... Should report: {0}", (object) reportToAnalyticsServer), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (InitializeAnalyticsState));
      if (analyticsRepository.VerboseLogEnabled)
        Microsoft.AppCenter.AppCenter.LogLevel = Microsoft.AppCenter.LogLevel.Verbose;
      string analyticsKey = NativeLibrary.GetAppCenterKey(analyticsRepository.Logger);
      if (string.IsNullOrEmpty(analyticsKey))
      {
        analyticsRepository.Logger.WriteErrorEx("Failed to retrieve the services key; cancelling the initialization request...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (InitializeAnalyticsState));
        return false;
      }
      Guid? installIdAsync = await Microsoft.AppCenter.AppCenter.GetInstallIdAsync();
      if (!installIdAsync.HasValue)
        analyticsRepository.Logger.WriteWarningEx("Failed to retrieve Device Id from App Center", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (InitializeAnalyticsState));
      // ISSUE: explicit non-virtual call
      __nonvirtual (analyticsRepository.DeviceId) = installIdAsync?.ToString();
      analyticsRepository.RegisterAnalyticsCallbacks();
      return await analyticsRepository.StartServicesAsync(analyticsKey, reportToAnalyticsServer).ConfigureAwait(false);
    }

    protected override async Task<bool> OnShutdownAsync() => await base.OnShutdownAsync().ConfigureAwait(false);

    private static IEnumerable<ErrorAttachmentLog> GetLogsAndAttachments(
      string logs,
      params (string FileName, string FileContent)[] attachements)
    {
      if (!string.IsNullOrEmpty(logs))
        yield return ErrorAttachmentLog.AttachmentWithText(logs, AnalyticsRepositoryBase.GenerateLogFileName());
      if (attachements != null)
      {
        (string, string)[] valueTupleArray = attachements;
        for (int index = 0; index < valueTupleArray.Length; ++index)
        {
          (string fileName, string text) = valueTupleArray[index];
          if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(text))
            yield return ErrorAttachmentLog.AttachmentWithText(text, fileName);
        }
        valueTupleArray = ((string, string)[]) null;
      }
    }

    private Task<bool> StartServicesAsync(string analyticsKey, bool enableReports) => Task.Run<bool>((Func<Task<bool>>) (async () =>
    {
      bool analyticsStarted = false;
      bool crashesStarted = false;
      try
      {
        Microsoft.AppCenter.AppCenter.Configure(analyticsKey);
        if (!Microsoft.AppCenter.AppCenter.Configured)
        {
          this.Logger.WriteErrorEx("Failed to configure App Center services; cancelling the initialization request...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (StartServicesAsync));
          return false;
        }
        Microsoft.AppCenter.AppCenter.Start(typeof (Microsoft.AppCenter.Crashes.Crashes));
        this.Logger.WriteInfoEx("App Center 'Crashes' service started", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (StartServicesAsync));
        crashesStarted = true;
        Microsoft.AppCenter.AppCenter.Start(typeof (Microsoft.AppCenter.Analytics.Analytics));
        this.Logger.WriteInfoEx("App Center 'Analytics' service started", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (StartServicesAsync));
        analyticsStarted = true;
        await this.UpdateAppCenterState(enableReports, false).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.Logger.WriteException("Error while starting App Center services", ex);
        if (crashesStarted & enableReports)
          this.TrackError(ex, (IDictionary<string, string>) null, Array.Empty<(string, string)>());
      }
      return analyticsStarted & crashesStarted;
    }));

    private async Task UpdateAppCenterState(bool enable, bool waitForInit = true)
    {
      AppCenterAnalyticsRepository analyticsRepository = this;
      analyticsRepository.Logger.WriteInfoEx(string.Format("Request to update App Center services state to: {0}", (object) enable), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (UpdateAppCenterState));
      if (waitForInit)
      {
        int num = await analyticsRepository.InitializationTask.Task.ConfigureAwait(false) ? 1 : 0;
      }
      await Task.WhenAll(Microsoft.AppCenter.Crashes.Crashes.SetEnabledAsync(enable), Microsoft.AppCenter.Analytics.Analytics.SetEnabledAsync(enable)).ConfigureAwait(false);
      analyticsRepository.Logger.WriteInfoEx(string.Format("App Center reporting state updated to: {0}", (object) enable), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (UpdateAppCenterState));
    }

    private void RegisterAnalyticsCallbacks()
    {
      Microsoft.AppCenter.Crashes.Crashes.GetErrorAttachments = (GetErrorAttachmentsCallback) (report =>
      {
        IEnumerable<ErrorAttachmentLog> logs = this.CollectCrashLogs().Result;
        this.QueueReport((Action) (() =>
        {
          (string name, IDictionary<string, string> data) eventForException = TelemetryExtensions.GetEventForException(AppTelemetryOperation.CrashReport, stackTrace: report.StackTrace, errorId: report.Id, attachementCount: logs.Count<ErrorAttachmentLog>());
          Microsoft.AppCenter.Analytics.Analytics.TrackEvent(eventForException.name, eventForException.data);
        }));
        return logs;
      });
      this.Logger.WriteInfoEx("Analytics callbacks registered", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (RegisterAnalyticsCallbacks));
    }

    private async Task<IEnumerable<ErrorAttachmentLog>> CollectCrashLogs()
    {
      AppCenterAnalyticsRepository analyticsRepository = this;
      analyticsRepository.Logger.WriteDebugEx("Collecting crash logs...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (CollectCrashLogs));
      try
      {
        if (!await Microsoft.AppCenter.Crashes.Crashes.HasCrashedInLastSessionAsync().ConfigureAwait(false))
        {
          string logs = analyticsRepository.CollectMemoryLogs();
          analyticsRepository.Logger.WriteInfoEx(string.Format("Collected crash logs from memory: {0}", (object) !string.IsNullOrEmpty(logs)), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (CollectCrashLogs));
          analyticsRepository.DeleteLogFile();
          return AppCenterAnalyticsRepository.GetLogsAndAttachments(logs);
        }
        string str = await analyticsRepository.CollectFileLogsAsync().ConfigureAwait(false);
        analyticsRepository.Logger.WriteInfoEx(string.Format("Collected crash logs from file: {0}", (object) !string.IsNullOrEmpty(str)), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AppCenterAnalyticsRepository.cs", nameof (CollectCrashLogs));
        analyticsRepository.DeleteLogFile();
        return AppCenterAnalyticsRepository.GetLogsAndAttachments(analyticsRepository.CollectMemoryLogs(), ("CrashLog", str));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        analyticsRepository.Logger.WriteException("Failed to collect crash logs.", ex);
        analyticsRepository.TrackError(ex, (IDictionary<string, string>) null, Array.Empty<(string, string)>());
        return (IEnumerable<ErrorAttachmentLog>) Array.Empty<ErrorAttachmentLog>();
      }
    }
  }
}
