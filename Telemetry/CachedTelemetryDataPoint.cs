// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.CachedTelemetryDataPoint
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using System;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  internal class CachedTelemetryDataPoint
  {
    private readonly AppTelemetryDataKey key;
    private readonly ILogger logger;
    private readonly TimeSpan cacheLifetime;
    private readonly Func<(object, bool)> valueProducer;
    private DateTime cacheExpiry;
    private object value;

    public CachedTelemetryDataPoint(
      AppTelemetryDataKey key,
      ILogger logger,
      Func<(object, bool)> valueProducer,
      TimeSpan? cacheLifetime = null)
    {
      this.key = key;
      this.logger = logger;
      this.valueProducer = valueProducer;
      this.cacheLifetime = cacheLifetime ?? TimeSpan.FromHours(4.0);
      this.cacheExpiry = DateTime.MinValue;
    }

    public object Value
    {
      get
      {
        this.RefreshValue();
        return this.value;
      }
    }

    private void RefreshValue()
    {
      DateTime utcNow = DateTime.UtcNow;
      if (!(utcNow > this.cacheExpiry))
        return;
      try
      {
        (object, bool) valueTuple = this.valueProducer();
        this.value = valueTuple.Item1;
        if (!valueTuple.Item2)
          return;
        this.cacheExpiry = utcNow.Add(this.cacheLifetime);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteExceptionAsWarning(string.Format("Failed to get cached telemetry data point for {0}.", (object) this.key), ex);
      }
    }
  }
}
