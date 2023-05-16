// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Logging.ClientCompositeLogger
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Configuration;
using Okta.Devices.SDK;

namespace Okta.Authenticator.NativeApp.Logging
{
  public class ClientCompositeLogger : CompositeLogger
  {
    private readonly IConfigurationManager configurationManager;

    public ClientCompositeLogger(IConfigurationManager configurationManager)
    {
      this.configurationManager = configurationManager;
      this.SetupLogger();
    }

    private void SetupLogger()
    {
      this.AddLogger(this.configurationManager.CreateLoggerWithConfiguredLogLevel(this.configurationManager.EventLogSource, "Okta"));
      this.AddLogger((ILogger) new InMemoryLogger(200, LogLevel.Debug));
    }
  }
}
