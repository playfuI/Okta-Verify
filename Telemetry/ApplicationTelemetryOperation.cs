// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.ApplicationTelemetryOperation
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Injector;
using Okta.Devices.SDK.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public class ApplicationTelemetryOperation : 
    ITelemetryOperationData,
    ITelemetryEventData,
    IDisposable
  {
    private readonly Stopwatch stopwatch;
    private readonly List<(string, object)> dataBag;
    private readonly bool isScenario;
    private bool isDisposed;

    public ApplicationTelemetryOperation(string operation, bool isScenario = false)
    {
      this.Name = operation;
      this.Path = (string) null;
      this.isScenario = isScenario;
      this.dataBag = new List<(string, object)>();
      this.stopwatch = new Stopwatch();
      this.stopwatch.Start();
    }

    public ApplicationTelemetryOperation(AppTelemetryScenario scenario)
      : this(scenario.ToString(), true)
    {
    }

    public TimeSpan Duration => this.stopwatch.Elapsed;

    public TelemetryEventStatus Status { get; set; }

    public string Name { get; }

    public string Path { get; }

    public ILookup<string, object> DataBag => this.dataBag.ToLookup<(string, object), string, object>((Func<(string, object), string>) (l => l.Item1), (Func<(string, object), object>) (l => l.Item2));

    public void AddData(AppTelemetryDataKey key, object value) => this.dataBag.Add((key.ToString(), value));

    public void AddData(TelemetryDataKey key, object value) => this.dataBag.Add((key.ToString(), value));

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.isDisposed)
        return;
      if (disposing)
      {
        this.stopwatch.Stop();
        ITelemetryTracker telemetryTracker = AppInjector.Get<ITelemetryTracker>();
        if (this.isScenario)
          telemetryTracker.TrackScenario((ITelemetryOperationData) this);
        else
          telemetryTracker.TrackOperation((ITelemetryOperationData) this);
      }
      this.isDisposed = true;
    }
  }
}
