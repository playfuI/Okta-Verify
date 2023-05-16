// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Bindings.BindingsManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authentication;
using Okta.Devices.SDK.Bindings;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Telemetry;
using Okta.Devices.SDK.Windows.Bindings;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Bindings
{
  public class BindingsManager : IBindingsManager, IDisposable
  {
    private const int SecondaryActivationCallTimeout = 5000;
    private const int BinaryValidationCacheDurationInSeconds = 30;
    private static readonly Guid ApplicationId = new Guid("63c081db-1f13-5084-882f-e79e1e5e2da7");
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly ILogger logger;
    private readonly IEventAggregator eventAggregator;
    private readonly ISingletonHandler singletonHandler;
    private readonly IConfigurationManager configManager;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IPlatformOSProfileAbstraction profileAbstraction;
    private readonly Uri customUriSchemUri;
    private UserSessionChangedEvent bindingUpdateEvent;
    private bool disposed;
    private IAuthenticatorBindingManager<AuthenticatorBinding> bindingManager;
    private bool listenOnLoopback;
    private SecureLoopbackBindingConfiguration loopbackConfiguration;
    private LoopbackBindingOperationMode loopbackOperationMode;

    public BindingsManager(
      ILogger logger,
      IAnalyticsProvider analytics,
      IEventAggregator eventAggregator,
      ISingletonHandler singletonHandler,
      IConfigurationManager configManager,
      IApplicationStateMachine stateMachine,
      IPlatformOSProfileAbstraction profileAbstraction)
    {
      this.logger = logger;
      this.analyticsProvider = analytics;
      this.eventAggregator = eventAggregator;
      this.singletonHandler = singletonHandler;
      this.configManager = configManager;
      this.stateMachine = stateMachine;
      this.customUriSchemUri = new Uri("com-okta-authenticator:/");
      this.profileAbstraction = profileAbstraction;
      this.SubscribeToEvents();
    }

    public LoopbackBindingOperationMode LoopbackOperationMode
    {
      get
      {
        if (this.loopbackOperationMode == LoopbackBindingOperationMode.Unknown)
          this.loopbackOperationMode = this.configManager.GetLoopbackOperationMode(this.logger);
        return this.loopbackOperationMode;
      }
    }

    public SecureLoopbackBindingConfiguration LoopbackConfiguration
    {
      get
      {
        if (this.loopbackConfiguration == null)
          this.loopbackConfiguration = new SecureLoopbackBindingConfiguration(BindingsManager.ApplicationId, this.LoopbackOperationMode == LoopbackBindingOperationMode.EnabledHttps || this.LoopbackOperationMode == LoopbackBindingOperationMode.EnabledMutualAuth, this.LoopbackOperationMode == LoopbackBindingOperationMode.EnabledMutualAuth, this.configManager.GetCallerBinaryValidationType(this.logger) == CallerBinaryValidationType.FailOnUnsigned, new TimeSpan?(TimeSpan.FromSeconds(30.0)), this.profileAbstraction);
        return this.loopbackConfiguration;
      }
    }

    public bool StartBinding<TBinding>() where TBinding : AuthenticatorBinding
    {
      if (this.bindingManager == null || this.HasBinding<TBinding>())
        return false;
      Type type = typeof (TBinding);
      AuthenticatorBinding authenticatorBinding;
      if (type == typeof (UniversalCustomUriSchemeBinding))
      {
        authenticatorBinding = (AuthenticatorBinding) new UniversalCustomUriSchemeBinding(this.customUriSchemUri);
      }
      else
      {
        if (!(type == typeof (SecureLoopbackAuthenticatorBinding)))
          return false;
        authenticatorBinding = (AuthenticatorBinding) new SecureLoopbackAuthenticatorBinding(this.LoopbackConfiguration);
      }
      if (this.bindingManager.AddBinding(authenticatorBinding))
        return true;
      authenticatorBinding.SafeDispose();
      return false;
    }

    public bool StopBinding<TBinding>() where TBinding : AuthenticatorBinding
    {
      if (this.bindingManager == null)
        return false;
      AuthenticatorBinding binding = (AuthenticatorBinding) this.GetBinding<TBinding>();
      return binding != null && this.bindingManager.RemoveBinding(binding);
    }

    [AnalyticsScenario(ScenarioType.FastPassAuthentication)]
    public async Task ProcessUriSignalActivationAsync(string messageInfo)
    {
      this.logger.WriteInfoEx("Processing custom URI activation...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessUriSignalActivationAsync));
      this.ProcessBindingRefresh();
      Uri result;
      if (!Uri.TryCreate(messageInfo, UriKind.Absolute, out result))
      {
        string str = "Received " + messageInfo + " when activating for custom URI which is not a valid uri.";
        this.logger.WriteWarningEx(str, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessUriSignalActivationAsync));
        this.analyticsProvider.TrackErrorWithLogs(str, sourceMethodName: nameof (ProcessUriSignalActivationAsync));
      }
      else
        await this.ProcessCustomUriSchemeRequest(result).ConfigureAwait(false);
    }

    public async Task ProcessUriActivationAsync(Uri uri)
    {
      uri.EnsureNotNull(nameof (uri));
      await this.ProcessCustomUriSchemeRequest(uri).ConfigureAwait(false);
    }

    public void ProcessBindingRefresh()
    {
      this.logger.WriteDebugEx("Making sure bindings are running correctly.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessBindingRefresh));
      if (!this.HasBinding<UniversalCustomUriSchemeBinding>())
      {
        this.logger.WriteInfoEx("Starting custom URI scheme binding.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessBindingRefresh));
        this.StartBinding<UniversalCustomUriSchemeBinding>();
      }
      if (!this.listenOnLoopback)
      {
        this.logger.WriteInfoEx("Loopback binding is not required.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessBindingRefresh));
      }
      else
      {
        SecureLoopbackAuthenticatorBinding binding = this.GetBinding<SecureLoopbackAuthenticatorBinding>();
        if (binding != null)
        {
          Uri result;
          if (Uri.TryCreate(binding.ListenerPrefix, UriKind.Absolute, out result) && result.Port == 8769)
          {
            this.logger.WriteDebugEx("Loopback already running on IANA port.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessBindingRefresh));
            return;
          }
          this.logger.WriteInfoEx(string.Format("Loopback currently running on port '{0}', trying to upgrade port.", (object) result?.Port), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessBindingRefresh));
          this.StopBinding<SecureLoopbackAuthenticatorBinding>();
        }
        this.StartBinding<SecureLoopbackAuthenticatorBinding>();
      }
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    internal BindingStatus GetBindingStatus<TBinding>() where TBinding : AuthenticatorBinding
    {
      TBinding binding = this.GetBinding<TBinding>();
      return (object) binding != null ? this.bindingManager.GetBindingStatus((AuthenticatorBinding) binding) : BindingStatus.NotRegistered;
    }

    [AnalyticsScenario(ScenarioType.FastPassAuthentication)]
    internal async Task ProcessCustomUriSchemeRequest(Uri uri)
    {
      try
      {
        UniversalCustomUriSchemeBinding binding = this.GetBinding<UniversalCustomUriSchemeBinding>();
        if (binding == null)
        {
          string str = "Not processing custom URI scheme request because binding is not set.";
          this.logger.WriteWarningEx(str, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ProcessCustomUriSchemeRequest));
          this.analyticsProvider.TrackErrorWithLogs(str, sourceMethodName: nameof (ProcessCustomUriSchemeRequest));
        }
        else
          await binding.ProcessRequest(uri).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        this.logger.WriteException("Failed to process request for custom uri scheme binding", ex);
        if (ex is OktaWebException oktaWebException && oktaWebException.StatusCode == HttpStatusCode.Unauthorized)
          return;
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    internal void StartBindings(
      IAuthenticatorBindingManager<AuthenticatorBinding> authBindingMgr = null,
      bool? disableLoopback = null)
    {
      this.bindingManager = authBindingMgr ?? (IAuthenticatorBindingManager<AuthenticatorBinding>) new AuthenticatorBindingManager();
      if (this.StartBinding<UniversalCustomUriSchemeBinding>())
        this.analyticsProvider.AddOperationData<TelemetryDataKey>(TelemetryDataKey.BindingType, (object) "UniversalCustomUriSchemeBinding");
      this.listenOnLoopback = disableLoopback.HasValue ? !disableLoopback.Value : this.LoopbackOperationMode != LoopbackBindingOperationMode.Disabled;
      if (this.listenOnLoopback)
      {
        if (!this.StartBinding<SecureLoopbackAuthenticatorBinding>())
          return;
        this.analyticsProvider.AddOperationData<TelemetryDataKey>(TelemetryDataKey.BindingType, (object) "SecureLoopbackAuthenticatorBinding");
      }
      else
        this.logger.WriteWarningEx("Not starting loopback because it was disabled in registry", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (StartBindings));
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
        return;
      if (disposing)
      {
        try
        {
          this.StopBinding<SecureLoopbackAuthenticatorBinding>();
          this.StopBinding<UniversalCustomUriSchemeBinding>();
        }
        catch (OktaException ex)
        {
          this.logger.WriteException("Failed to stop binding.", (Exception) ex);
          this.analyticsProvider.TrackErrorWithLogsAndAppData((Exception) ex);
        }
        this.bindingManager.SafeDispose();
      }
      this.disposed = true;
    }

    private static void RemoveTransportLayerSecurity() => SecureLoopbackAuthenticatorBinding.ResetServerTransportLayerSecurity(BindingsManager.ApplicationId);

    private void SubscribeToEvents()
    {
      this.bindingUpdateEvent = this.eventAggregator.GetEvent<UserSessionChangedEvent>();
      this.bindingUpdateEvent?.Subscribe(new Action<UserSessionChangedEventType>(this.OnBindingUpdate));
      this.stateMachine.RegisterDeferral(ComputingStateType.Loading, new ComputingStateDeferral(this.HandleApplicationStart), "initializing bindings");
      this.stateMachine.RegisterDeferral(ComputingStateType.ShuttingDown, new ComputingStateDeferral(this.HandleApplicationShutdown), "shutting down bindings");
    }

    private async Task HandleApplicationStart(ComputingStateContext context)
    {
      this.StartBindings();
      Uri uri;
      if (!this.ValidateCustomUriArguments(context.Command, context.Arguments, out uri))
        return;
      try
      {
        await this.ProcessUriActivationAsync(uri).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Failed to process the URI activation", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    private async Task HandleApplicationShutdown(ComputingStateContext context)
    {
      switch (context.Command)
      {
        case StartupArgumentType.Uri:
          await this.HandleCustomUriActivation(context.Command, context.Arguments).ConfigureAwait(false);
          break;
        case StartupArgumentType.Configure:
          this.ConfigureTransportLayerSecurity();
          break;
        case StartupArgumentType.RemoveAll:
          BindingsManager.RemoveTransportLayerSecurity();
          break;
        default:
          this.ShutdownAllBindings();
          break;
      }
    }

    private void ConfigureTransportLayerSecurity()
    {
      if (!this.LoopbackConfiguration.EnableTransportLayerSecurity)
        return;
      this.logger.WriteInfoEx(string.Format("Configuring transport layer security. Mutual Auth: {0}", (object) this.LoopbackConfiguration.RequireMutualAuthentication), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ConfigureTransportLayerSecurity));
      SecureLoopbackAuthenticatorBinding.ConfigureServerTransportLayerSecurity(BindingsManager.ApplicationId, this.LoopbackConfiguration.RequireMutualAuthentication);
    }

    private void ShutdownAllBindings()
    {
      this.logger.WriteInfoEx("Shutting down bindings.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ShutdownAllBindings));
      this.StopBinding<SecureLoopbackAuthenticatorBinding>();
      this.StopBinding<UniversalCustomUriSchemeBinding>();
    }

    private async Task HandleCustomUriActivation(StartupArgumentType command, string[] arguments)
    {
      Uri uri;
      if (!this.ValidateCustomUriArguments(command, arguments, out uri))
        return;
      try
      {
        if (!this.singletonHandler.SendDataToMainInstance(Encoding.Unicode.GetBytes(uri.ToString()), SingletonSignals.URIActivate))
        {
          this.logger.WriteErrorEx("Failed to signal the primary instance for URI activation.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (HandleCustomUriActivation));
        }
        else
        {
          this.logger.WriteInfoEx(string.Format("Waiting for {0}ms on secondary for activation request from primary.", (object) 5000), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (HandleCustomUriActivation));
          await Task.Delay(5000).ConfigureAwait(false);
          this.logger.WriteDebugEx("Finished shutting down binding manager.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (HandleCustomUriActivation));
        }
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteErrorEx("Failed to signal main instance: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (HandleCustomUriActivation));
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    private void OnBindingUpdate(UserSessionChangedEventType type)
    {
      if (type == UserSessionChangedEventType.InactiveUserProfile)
      {
        this.StopBinding<SecureLoopbackAuthenticatorBinding>();
      }
      else
      {
        if (type != UserSessionChangedEventType.ActivatedUserProfile)
          return;
        this.ProcessBindingRefresh();
      }
    }

    private TBinding GetBinding<TBinding>() where TBinding : AuthenticatorBinding
    {
      if (this.bindingManager == null)
        return default (TBinding);
      IList<AuthenticatorBinding> bindings = this.bindingManager.GetBindings();
      return (bindings != null ? bindings.FirstOrDefault<AuthenticatorBinding>((Func<AuthenticatorBinding, bool>) (b => b.GetType() == typeof (TBinding))) : (AuthenticatorBinding) null) as TBinding;
    }

    private bool HasBinding<TBinding>() where TBinding : AuthenticatorBinding => (object) this.GetBinding<TBinding>() != null;

    private bool ValidateCustomUriArguments(
      StartupArgumentType argumentType,
      string[] arguments,
      out Uri uri)
    {
      uri = (Uri) null;
      if (argumentType != StartupArgumentType.Uri)
        return false;
      if (arguments == null || arguments.Length < 2)
      {
        this.logger.WriteErrorEx("URI Activation: Missing url in the arguments", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ValidateCustomUriArguments));
        return false;
      }
      if (!Uri.TryCreate(arguments[1], UriKind.Absolute, out uri))
      {
        this.logger.WriteErrorEx("URI Activation: Invalid url in the arguments", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ValidateCustomUriArguments));
        return false;
      }
      this.logger.WriteInfoEx("Okta Verify was launched by custom URI scheme", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Bindings\\BindingsManager.cs", nameof (ValidateCustomUriArguments));
      return true;
    }
  }
}
