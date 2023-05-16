// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AsyncInlineScenarioViewModel`1
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK.Telemetry;
using System;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public abstract class AsyncInlineScenarioViewModel<T> : 
    AsyncInlineOperationViewModel<T>,
    IDisposable
  {
    private readonly AppTelemetryScenario scenario;
    private bool disposed;
    private ITrackedOperation trackedOperation;

    public AsyncInlineScenarioViewModel(AppTelemetryScenario scenario) => this.scenario = scenario;

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing)
        this.EndTelemetryTracking(default (T), TelemetryEventStatus.Abandoned, nameof (Dispose));
      this.disposed = true;
    }

    protected void StartTelemetryTracking()
    {
      if (this.trackedOperation != null)
      {
        this.AnalyticsProvider.AddOperationData<AppTelemetryDataKey>(AppTelemetryDataKey.DetailedFailureReason, (object) "UserReInit");
        this.trackedOperation.SetStatus(TelemetryEventStatus.Abandoned);
        this.trackedOperation.Dispose();
      }
      this.trackedOperation = this.AnalyticsProvider.StartScenario(this.scenario);
    }

    protected void EndTelemetryTracking(T result, Exception ex) => this.EndTelemetryTracking(result, TelemetryEventStatus.ClientError, ex?.GetType().Name);

    protected void EndTelemetryTracking(
      T result,
      TelemetryEventStatus status,
      string failureReason = null)
    {
      if (!string.IsNullOrEmpty(failureReason))
        this.AnalyticsProvider.AddOperationData<AppTelemetryDataKey>(AppTelemetryDataKey.DetailedFailureReason, (object) failureReason);
      this.trackedOperation?.SetStatus(status);
      this.trackedOperation?.Dispose();
      this.trackedOperation = (ITrackedOperation) null;
      this.disposed = true;
      this.TrySetResult(result);
    }
  }
}
