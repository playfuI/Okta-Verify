// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Injector.SdkDependencyProvider
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInfo;
using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authentication;
using Okta.Devices.SDK.Authenticator;
using Okta.Devices.SDK.Authenticator.Integrations;
using Okta.Devices.SDK.DependencyInjection;
using Okta.Devices.SDK.Telemetry;
using Okta.Devices.SDK.WebClient;
using Okta.Devices.SDK.Win32;
using System;

namespace Okta.Authenticator.NativeApp.Injector
{
  public class SdkDependencyProvider : Win32DeviceDependencyProvider
  {
    private readonly IAuthenticatorSandbox sandbox;
    private readonly IConfigurationManager configurationManager;
    private readonly ILogger logger;

    public SdkDependencyProvider(IAuthenticatorSandbox sandbox)
    {
      this.logger = AppInjector.Get<ILogger>();
      this.configurationManager = AppInjector.Get<IConfigurationManager>();
      this.sandbox = sandbox;
    }

    public override IAuthenticatorApplication GetAuthenticatorApplication() => (IAuthenticatorApplication) new AuthenticatorApplication("okta.63c081db-1f13-5084-882f-e79e1e5e2da7");

    public override ICredentialOptions GetCredentialOptions() => AppInjector.Get<IClientSignInManager>()?.CredentialOptions;

    public override ILogger GetLogger() => this.logger;

    public override IStorageManager GetStorageManager() => AppInjector.Get<IClientStorageManager>().Store;

    protected override IAuthenticatorSandbox GetAuthenticatorSandbox() => this.sandbox;

    public override bool OnOptionalDependencyRequest(
      Type serviceType,
      IDependencyRegistrar dependencyRegistrar)
    {
      if (dependencyRegistrar == null)
        return false;
      if (serviceType == typeof (IAuthenticatorPluginConfiguration))
      {
        dependencyRegistrar.RegisterInstance<IAuthenticatorPluginConfiguration>((IAuthenticatorPluginConfiguration) AppInjector.Get<IApplicationInfoManager>());
        return true;
      }
      if (serviceType == typeof (IBindingEventHandler))
      {
        dependencyRegistrar.RegisterInstance<IBindingEventHandler>(AppInjector.Get<IBindingEventHandler>());
        return true;
      }
      if (serviceType == typeof (ITelemetryTracker))
      {
        dependencyRegistrar.RegisterInstance<ITelemetryTracker>(AppInjector.Get<ITelemetryTracker>());
        return true;
      }
      if (serviceType == typeof (ISecureConnnectionValidator) && this.configurationManager.TryGetMachineRegistryConfig<int>(this.logger, "DisableSslPinning", 0, (Func<object, int>) null) != 1)
      {
        dependencyRegistrar.RegisterInstance<ISecureConnnectionValidator>(AppInjector.Get<ISecureConnnectionValidator>());
        return true;
      }
      if (serviceType == typeof (IPluginSignalManagerFactory) && this.configurationManager.GlobalPluginManifestFileDirectory != null && this.configurationManager.TryGetMachineRegistryConfig<int>(this.logger, "Integrations", "DisablePlugins", 0, (Func<object, int>) null) != 1)
      {
        dependencyRegistrar.RegisterSingleton<IPluginSignalManagerFactory, PluginSignalManagerFactory>();
        return true;
      }
      if (!(serviceType == typeof (ICertificateFetcher)) || this.configurationManager.TryGetMachineRegistryConfig<int>(this.logger, "Integrations", "DisableCertificateCaching", 0, (Func<object, int>) null) == 1)
        return base.OnOptionalDependencyRequest(serviceType, dependencyRegistrar);
      dependencyRegistrar.RegisterSingleton<ICertificateFetcher, CachedStoreCertificateFetcher>();
      return true;
    }
  }
}
