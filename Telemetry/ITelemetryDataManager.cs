// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.ITelemetryDataManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System.Collections.Generic;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public interface ITelemetryDataManager
  {
    (AppTelemetryDataKey Key, object Value) this[AppTelemetryDataKey key] { get; }

    IDictionary<AppTelemetryDataKey, object> GetAllTelemetryDataPoints();

    IDictionary<string, string> GetAllTelemetryDataPointsAsString();

    IDictionary<AppTelemetryDataKey, object> GetSpecificDataPoints(params AppTelemetryDataKey[] keys);

    IDictionary<string, string> GetSpecificDataPointsAsString(params AppTelemetryDataKey[] keys);

    void PersistDataInLog();

    bool AddCustomDataPoint(AppTelemetryDataKey key, object value);
  }
}
