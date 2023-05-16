﻿// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Telemetry.AnalyticsScenarioAttribute
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using System;

namespace Okta.Authenticator.NativeApp.Telemetry
{
  [AttributeUsage(AttributeTargets.Method)]
  public class AnalyticsScenarioAttribute : Attribute
  {
    public AnalyticsScenarioAttribute(ScenarioType scenario) => this.Scenario = scenario;

    public ScenarioType Scenario { get; }
  }
}
