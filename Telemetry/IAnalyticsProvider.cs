// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.IAnalyticsProvider
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public interface IAnalyticsProvider
  {
    void TrackErrorWithLogs(
      Exception exception,
      params (string FileName, string FileContent)[] additionalAttachements);

    void TrackErrorWithLogs(
      Exception exception,
      IDictionary<string, string> properties,
      params (string FileName, string FileContent)[] additionalAttachements);

    void TrackEvent(string name, ILookup<string, object> properties);

    void TrackEvent(string name, IDictionary<string, string> properties = null);

    ITrackedOperation StartScenario(AppTelemetryScenario scenario, Stopwatch stopwatch = null);

    void AddOperationData<TDataKey>(TDataKey dataKey, object value) where TDataKey : Enum;

    void AddOperationData(
      params (TelemetryDataKey Key, object Value)[] operationData);

    void AddScenarioData<TDataKey>(TDataKey dataKey, object value) where TDataKey : Enum;

    void AddScenarioData(
      params (TelemetryDataKey Key, object Value)[] operationData);

    void SetUserId(string userId);
  }
}
