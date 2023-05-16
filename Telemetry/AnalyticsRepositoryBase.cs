// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.AnalyticsRepositoryBase
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Models;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public abstract class AnalyticsRepositoryBase : IAnalyticsRepository, IDisposable
  {
    private const string LogDefaultPrefix = "Log";
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2.0);
    private readonly ConcurrentQueue<Action> reportingQueue;
    private readonly CancellationTokenSource reportingCancellation;
    private readonly IConfigurationManager configurationManager;
    private readonly IClientStorageManager storageManager;
    private readonly IEventAggregator eventAggregator;
    private readonly string logFile;
    private SemaphoreSlim reportingSemaphore;
    private bool isDisposed;

    protected AnalyticsRepositoryBase(
      ILogger logger,
      IConfigurationManager configManager,
      IClientStorageManager storageManager,
      IEventAggregator aggregator)
    {
      this.configurationManager = configManager;
      this.storageManager = storageManager;
      this.Logger = logger;
      this.eventAggregator = aggregator;
      this.reportingQueue = new ConcurrentQueue<Action>();
      this.reportingSemaphore = new SemaphoreSlim(1, 1);
      this.reportingCancellation = new CancellationTokenSource();
      this.logFile = this.RetrieveLogFile();
      this.InitializationTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      this.InitializeAppStateEvent();
    }

    public AnalyticsConfigurationType SystemConfiguration { get; private set; }

    public bool IsPreConfigured => this.SystemConfiguration == AnalyticsConfigurationType.Enabled || this.SystemConfiguration == AnalyticsConfigurationType.Disabled;

    public bool CanReportIssue => this.SystemConfiguration == AnalyticsConfigurationType.Default || this.SystemConfiguration == AnalyticsConfigurationType.Enabled;

    public string DeviceId { get; set; }

    protected ILogger Logger { get; }

    protected bool VerboseLogEnabled { get; private set; }

    protected TaskCompletionSource<bool> InitializationTask { get; }

    public abstract void TrackEvent(string name, IDictionary<string, string> data = null);

    public abstract void TrackError(
      Exception exception,
      IDictionary<string, string> data = null,
      params (string, string)[] fileAttachements);

    public abstract void SetUserId(string userId);

    public async Task Initialize(bool enableVerboseLog = false)
    {
      this.Logger.WriteInfoEx("Initializing analytics...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (Initialize));
      this.VerboseLogEnabled = enableVerboseLog;
      this.SystemConfiguration = this.configurationManager.GetAnalyticsConfigurationFromRegistry(this.Logger);
      bool flag = !Okta.Authenticator.NativeApp.Extensions.IsTestBuild;
      ConfiguredTaskAwaitable<bool> configuredTaskAwaitable;
      if (flag)
      {
        configuredTaskAwaitable = this.GetSavedReportChoice().ConfigureAwait(false);
        flag = await configuredTaskAwaitable;
      }
      bool shouldReport = flag;
      configuredTaskAwaitable = this.InitializeAnalyticsState(shouldReport).ConfigureAwait(false);
      bool result = await configuredTaskAwaitable;
      this.Logger.WriteInfoEx(string.Format("Analytics initialized: {0}; system configuration: {1} - Reports enabled: {2} - Test build: {3}", (object) result, (object) this.SystemConfiguration, (object) shouldReport, (object) Okta.Authenticator.NativeApp.Extensions.IsTestBuild), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (Initialize));
      this.InitializationTask.TrySetResult(result);
    }

    public void OnApplicationCrash()
    {
      try
      {
        string contents = this.CollectMemoryLogs();
        if (string.IsNullOrEmpty(contents))
          return;
        this.Logger.WriteInfoEx("App crashing; saving logs to file...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (OnApplicationCrash));
        File.WriteAllText(this.logFile, contents);
      }
      catch (Exception ex) when (!ex.IsCritical(this.Logger))
      {
        this.Logger.WriteException("Unable to write memory log to file.", ex);
      }
    }

    public abstract Task<bool> IsEnabled();

    public virtual async Task<bool> Configure(bool enable) => await this.UpdateSavedReportChoice(enable).ConfigureAwait(false);

    public Task<bool> FlushReports(TimeSpan timeout) => this.FlushReportsInternal(timeout);

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.isDisposed)
        return;
      this.reportingCancellation.Cancel();
      if (disposing)
      {
        this.reportingSemaphore.Dispose();
        this.reportingSemaphore = (SemaphoreSlim) null;
        this.reportingCancellation.Dispose();
      }
      this.isDisposed = true;
    }

    protected abstract Task<bool> InitializeAnalyticsState(bool reportToAnalyticsServer);

    protected virtual Task<bool> OnShutdownAsync() => this.FlushReportsInternal(AnalyticsRepositoryBase.ShutdownTimeout, true);

    protected void QueueReport(Action action)
    {
      if (this.reportingCancellation.IsCancellationRequested)
      {
        this.Logger.WriteDebugEx("Not queuing telemetry report, processing is cancelled", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (QueueReport));
      }
      else
      {
        this.reportingQueue.Enqueue(action);
        this.EnsureProcessingAsync().AsBackgroundTask("ensure processing report queue", this.Logger, handler: new Okta.Authenticator.NativeApp.Extensions.BackgroundTaskHandler(Okta.Authenticator.NativeApp.Extensions.UnobservedTaskContinuationLogErrorOnly));
      }
    }

    protected string CollectMemoryLogs()
    {
      ILogger logger1;
      return this.Logger is CompositeLogger logger2 && logger2.GetLoggerByType(typeof (InMemoryLogger), out logger1) ? logger1.ToString() : (string) null;
    }

    protected Task<string> CollectFileLogsAsync() => Task.Run<string>((Func<string>) (() =>
    {
      try
      {
        if (File.Exists(this.logFile))
          return File.ReadAllText(this.logFile);
        this.Logger.WriteInfoEx("Log file not found", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (CollectFileLogsAsync));
        return string.Empty;
      }
      catch (Exception ex) when (!ex.IsCritical(this.Logger))
      {
        this.Logger.WriteException("Failed to read logs from the log file.", ex);
      }
      return string.Empty;
    }));

    protected void DeleteLogFile()
    {
      try
      {
        if (!File.Exists(this.logFile))
          return;
        File.Delete(this.logFile);
      }
      catch (Exception ex) when (!ex.IsCritical(this.Logger))
      {
        this.Logger.WriteException("Failed to delete log file.", ex);
      }
    }

    protected static string GenerateLogFileName(string logPrefix = "Log") => string.Format("{0}-{1:yyyy-MM-dd_hh-mm-ss-tt}.txt", (object) logPrefix, (object) DateTime.Now);

    private async Task<bool> GetSavedReportChoice()
    {
      if (this.IsPreConfigured)
        return this.SystemConfiguration == AnalyticsConfigurationType.Enabled;
      bool? analyticsReportChoice = (bool?) (await this.storageManager.GetAppSettings().ConfigureAwait(false))?.AnalyticsReportChoice;
      bool? nullable = analyticsReportChoice;
      bool flag = false;
      if (nullable.GetValueOrDefault() == flag & nullable.HasValue)
      {
        this.Logger.WriteInfoEx("User opted out of analytics report", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (GetSavedReportChoice));
        return false;
      }
      nullable = analyticsReportChoice;
      return ((int) nullable ?? 1) != 0;
    }

    private async Task<bool> UpdateSavedReportChoice(bool enable)
    {
      try
      {
        if (!this.IsPreConfigured)
          return await this.storageManager.UpdateAppSettings((Action<OktaVerifySettingsModel>) (appSettings => appSettings.AnalyticsReportChoice = new bool?(enable))).ConfigureAwait(false);
        this.Logger.WriteInfoEx(string.Format("Analytics state already pre-configured to {0}, ignoring update request...", (object) this.SystemConfiguration), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (UpdateSavedReportChoice));
        return false;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.Logger.WriteException("Failed to update the analytics state", ex);
        this.TrackError(ex, (IDictionary<string, string>) null);
        return false;
      }
    }

    private async Task<bool> EnsureProcessingAsync(bool isFinal = false)
    {
      if (this.reportingCancellation.IsCancellationRequested)
      {
        this.Logger.WriteDebugEx("Not processing telemetry queue, processing is cancelled", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (EnsureProcessingAsync));
        return false;
      }
      if (!await this.InitializationTask.Task.ConfigureAwait(false))
        return false;
      await this.reportingSemaphore.WaitAsync(this.reportingCancellation.Token).ConfigureAwait(true);
      try
      {
        if (isFinal)
          this.reportingCancellation.Cancel();
        this.ProcessQueue();
      }
      finally
      {
        this.reportingSemaphore?.Release();
      }
      return true;
    }

    private async Task<bool> FlushReportsInternal(TimeSpan timeout, bool isFinal = false)
    {
      Task<bool> processingTask = this.WaitForInitAndProcess(isFinal);
      Task task = await Task.WhenAny(Task.Delay(timeout), (Task) processingTask).ConfigureAwait(false);
      bool flag = processingTask.IsCompleted && processingTask.Result;
      processingTask = (Task<bool>) null;
      return flag;
    }

    private async Task<bool> WaitForInitAndProcess(bool isFinal = false) => await this.EnsureProcessingAsync(isFinal).ConfigureAwait(false);

    private void ProcessQueue()
    {
      Action result;
      while (this.reportingQueue.TryDequeue(out result))
      {
        try
        {
          result();
        }
        catch (Exception ex) when (!ex.IsCritical(this.Logger))
        {
          this.Logger.WriteErrorEx("Failed to process report with " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Telemetry\\AnalyticsRepositoryBase.cs", nameof (ProcessQueue));
          this.TrackError(ex, (IDictionary<string, string>) null, Array.Empty<(string, string)>());
        }
      }
    }

    private string RetrieveLogFile()
    {
      string logDirectory = this.configurationManager.LogDirectory;
      logDirectory.EnsureNotNullOrBlank("logDirectory");
      if (!Directory.Exists(logDirectory))
        Directory.CreateDirectory(logDirectory);
      return Path.Combine(logDirectory, "log.txt");
    }

    private void InitializeAppStateEvent()
    {
      this.eventAggregator.GetEvent<AppStateEvent>().Subscribe(new Action<AppState>(this.OnAppStateUpdated));
      AppInjector.Get<IApplicationStateMachine>()?.RegisterDeferral(ComputingStateType.ShuttingDown, (ComputingStateDeferral) (s => (Task) this.OnShutdownAsync()), "flushing analytics repository for shutdown");
    }

    private void OnAppStateUpdated(AppState updatedState)
    {
      if (updatedState.StateType != ComputingStateType.Bootstrapping)
        return;
      this.Initialize().AsBackgroundTask("initialization for analytics", this.Logger);
    }
  }
}
