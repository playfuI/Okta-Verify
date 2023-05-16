// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.OktaVerifyAnalyticsProvider
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Devices.SDK.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public class OktaVerifyAnalyticsProvider : IAnalyticsProvider, ITelemetryTracker
  {
    private readonly IAnalyticsRepository repository;

    public OktaVerifyAnalyticsProvider(IAnalyticsRepository repository) => this.repository = repository;

    public void TrackErrorWithLogs(
      Exception exception,
      params (string FileName, string FileContent)[] additionalAttachements)
    {
      this.TrackErrorWithLogs(exception, (IDictionary<string, string>) null, additionalAttachements);
    }

    public void TrackErrorWithLogs(
      Exception exception,
      IDictionary<string, string> properties = null,
      params (string FileName, string FileContent)[] additionalAttachements)
    {
      Exception normalizedError;
      if (!exception.ShouldReport(out normalizedError))
        return;
      this.repository.TrackError(normalizedError, properties, additionalAttachements);
    }

    public void TrackEvent(string name, ILookup<string, object> properties) => this.TrackEvent(name, properties.ToPropertyBag<string>());

    public void TrackEvent(string name, IDictionary<string, string> properties = null) => this.repository.TrackEvent(name, properties);

    public ITrackedOperation StartScenario(AppTelemetryScenario scenario, Stopwatch stopwatch = null) => DevicesSdk.Telemetry.StartScenario(scenario.ToString(), stopwatch);

    public void AddOperationData<TDataKey>(TDataKey dataKey, object value) where TDataKey : Enum => DevicesSdk.Telemetry.AddOperationData(dataKey.ToString(), value);

    public void AddOperationData(
      params (TelemetryDataKey Key, object Value)[] operationData)
    {
      DevicesSdk.Telemetry.AddOperationData(operationData);
    }

    public void AddScenarioData<TDataKey>(TDataKey dataKey, object value) where TDataKey : Enum => DevicesSdk.Telemetry.AddScenarioData(dataKey.ToString(), value);

    public void AddScenarioData(
      params (TelemetryDataKey Key, object Value)[] operationData)
    {
      DevicesSdk.Telemetry.AddScenarioData(operationData);
    }

    public void SetUserId(string userId) => this.repository.SetUserId(userId);

    void ITelemetryTracker.TrackError(ITelemetryErrorReport telemetryErrorReport)
    {
      if (telemetryErrorReport == null)
        return;
      this.TrackErrorWithLogs(telemetryErrorReport.Exception ?? (Exception) new OktaErrorReportException(telemetryErrorReport.Name, "TrackError"), telemetryErrorReport.DataBag.ToStringPropertyBag<string>(), Array.Empty<(string, string)>());
    }

    void ITelemetryTracker.TrackEvent(ITelemetryEventData telemetryEvent)
    {
      if (telemetryEvent == null)
        return;
      this.TrackEvent(telemetryEvent.Name, telemetryEvent.DataBag);
    }

    void ITelemetryTracker.TrackOperation(ITelemetryOperationData telemetryOperation)
    {
      if (telemetryOperation == null)
        return;
      this.TrackEvent(telemetryOperation.Name, OktaVerifyAnalyticsProvider.GetEventData(telemetryOperation, true));
    }

    void ITelemetryTracker.TrackScenario(ITelemetryOperationData telemetryOperation)
    {
      if (telemetryOperation == null)
        return;
      this.TrackEvent(OktaVerifyAnalyticsProvider.GetScenarioName(telemetryOperation), OktaVerifyAnalyticsProvider.GetEventData(telemetryOperation, telemetryOperation.Status != TelemetryEventStatus.Success));
    }

    private static IDictionary<string, string> GetEventData(
      ITelemetryOperationData operationData,
      bool addStatus)
    {
      IDictionary<string, string> propertyBag = operationData.DataBag.ToPropertyBag<string>();
      if (operationData.Duration != TimeSpan.MinValue)
        propertyBag.Add("DurationMS", operationData.Duration.TotalMilliseconds.ToString((IFormatProvider) CultureInfo.InvariantCulture));
      if (addStatus)
        propertyBag.Add("Status", operationData.Status.ToString());
      return propertyBag;
    }

    private static string GetScenarioName(ITelemetryOperationData operationData) => operationData.Name + (operationData.Status == TelemetryEventStatus.Success ? "Success" : "Failure");
  }
}
