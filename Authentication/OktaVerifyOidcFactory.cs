// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.OktaVerifyOidcFactory
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Configuration;
using Okta.Devices.SDK;
using Okta.Oidc.Abstractions;
using System;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class OktaVerifyOidcFactory : IOidcFactory, IDisposable
  {
    private readonly ILogger logger;
    private readonly IApplicationHandler application;
    private readonly IConfigurationManager configurationManager;
    private readonly Lazy<UserAgentHandler> userAgentInitializer;
    private bool? useSystemBrowser;
    private bool disposed;

    public OktaVerifyOidcFactory(
      ILogger logger,
      IApplicationHandler application,
      IConfigurationManager configurationManager)
    {
      this.logger = logger;
      this.application = application;
      this.configurationManager = configurationManager;
      this.userAgentInitializer = new Lazy<UserAgentHandler>(new Func<UserAgentHandler>(OktaVerifyOidcClient.GetUserAgentHandler), true);
    }

    public UserAgentHandler UserAgentHandler => this.userAgentInitializer.Value;

    public bool UseSystemBrowser
    {
      get
      {
        if (!this.useSystemBrowser.HasValue)
        {
          IConfigurationManager configurationManager = this.configurationManager;
          this.useSystemBrowser = new bool?(configurationManager == null || configurationManager.ShouldUseSystemBrowser(this.logger));
        }
        bool? useSystemBrowser = this.useSystemBrowser;
        bool flag = true;
        return useSystemBrowser.GetValueOrDefault() == flag & useSystemBrowser.HasValue;
      }
    }

    public IOktaClient GetOidcClient(string signInUrl)
    {
      try
      {
        return (IOktaClient) new OktaVerifyOidcClient(signInUrl, this.UseSystemBrowser, this.logger, this.UserAgentHandler, this.application);
      }
      catch (Exception ex)
      {
        this.logger.WriteException("Failed to create OIDC client", ex);
        throw;
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing && this.userAgentInitializer.IsValueCreated)
        this.userAgentInitializer.Value.Dispose();
      this.disposed = true;
    }
  }
}
