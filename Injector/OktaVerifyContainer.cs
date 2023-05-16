// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Injector.OktaVerifyContainer
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInfo;
using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Bindings;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Installation;
using Okta.Authenticator.NativeApp.Logging;
using Okta.Authenticator.NativeApp.Sandbox;
using Okta.Authenticator.NativeApp.Storage;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UI.Handlers;
using Okta.Authenticator.NativeApp.WebClient;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authentication;
using Okta.Devices.SDK.Bindings;
using Okta.Devices.SDK.Telemetry;
using Okta.Devices.SDK.WebClient;
using Okta.Devices.SDK.Windows.WebClient;
using OktaVerify.Bridge;
using OktaVerify.Bridge.Contracts;
using Prism.Events;

namespace Okta.Authenticator.NativeApp.Injector
{
  public class OktaVerifyContainer : BaseContainer
  {
    private readonly ISingleInstanceIdentifier instanceIdentifier;

    public OktaVerifyContainer(ISingleInstanceIdentifier instanceIdentifier) => this.instanceIdentifier = instanceIdentifier;

    protected override void RegisterDependencies()
    {
      this.RegisterInstance<ISingleInstanceIdentifier>(this.instanceIdentifier);
      this.RegisterSingleton<ILogger, ClientCompositeLogger>();
      this.RegisterSingleton<IAnalyticsRepository, AppCenterAnalyticsRepository>();
      this.RegisterSingleton<OktaVerifyAnalyticsProvider>(typeof (IAnalyticsProvider), typeof (ITelemetryTracker));
      this.RegisterSingleton<IApplicationStateMachine, ApplicationStateMachine>();
      this.RegisterSingleton<IApplicationHandler, ApplicationHandler>();
      this.RegisterSingleton<IJumpListHandler, JumpListHandler>();
      this.RegisterSingleton<IConfigurationManager, OktaVerifyConfigurationManager>();
      this.RegisterSingleton<ISingletonHandler, SingletonHandler>();
      this.RegisterSingleton<IAuthenticatorSandboxManager, AuthenticatorSandboxManager>();
      this.RegisterSingleton<IBindingEventHandler, BindingEventHandler>();
      this.RegisterSingleton<IClientAccountManager, ClientAccountManager>();
      this.RegisterSingleton<IOidcFactory, OktaVerifyOidcFactory>();
      this.RegisterSingleton<IClientSignInManager, ClientSignInManager>();
      this.RegisterSingleton<IBindingsManager, BindingsManager>();
      this.RegisterSingleton<IApplicationInfoManager, OktaVerifyApplicationInfoManager>();
      this.RegisterSingleton<ISystemSettingsManager, SystemSettingsManager>();
      this.RegisterSingleton<IClientStorageManager, ClientStorageManager>();
      this.RegisterSingleton<IEventAggregator, EventAggregator>();
      this.RegisterSingleton<IAppInstallationManager, AppInstallationManager>();
      this.RegisterSingleton<IFeatureSettings, FeatureSettings>();
      this.RegisterSingleton<IAuthenticationRequestManager, AuthenticationRequestManager>();
      this.RegisterSingleton<ITelemetryDataManager, TelemetryDataManager>();
      this.RegisterSingleton<IBuildSettingsConfig, BuildSettingsConfig>();
      this.RegisterSingleton<IAccountStateManager, AccountStateManager>();
      this.RegisterSingleton<IDeviceConfigurationManager, DeviceConfigurationManager>();
      this.RegisterSingleton<IPlatformOSProfileAbstraction, WindowsOSProfileAbstraction>();
      this.RegisterSingleton<IWindowActivationHandler, WindowActivationHandler>();
      this.RegisterSingleton<IWindowsMessageSender, WindowsMessageSender>();
      this.RegisterSingleton<IWindowsMessageReceiver, WindowsMessageReceiver>();
      this.RegisterSingleton<ICertificateChainValidator, X509ChainValidator>();
      this.RegisterSingleton<IPublicKeyList, OktaPublicKeyList>();
      this.RegisterSingleton<ISecureConnnectionValidator, CertificatePinningValidator>();
    }
  }
}
