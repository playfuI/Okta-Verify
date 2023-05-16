// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.IAnalyticsRepository
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  public interface IAnalyticsRepository
  {
    bool IsPreConfigured { get; }

    bool CanReportIssue { get; }

    string DeviceId { get; set; }

    Task<bool> IsEnabled();

    Task<bool> Configure(bool enable);

    Task<bool> FlushReports(TimeSpan timeout);

    void TrackEvent(string name, IDictionary<string, string> data = null);

    void TrackError(
      Exception exception,
      IDictionary<string, string> data = null,
      params (string, string)[] fileAttachements);

    void SetUserId(string id);

    void OnApplicationCrash();
  }
}
