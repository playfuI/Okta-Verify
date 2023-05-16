// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Injector.DevicesSdk
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Sandbox;
using Okta.Devices.SDK;
using Okta.Devices.SDK.DependencyInjection;
using Okta.Devices.SDK.Telemetry;
using Okta.Devices.SDK.WebClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Injector
{
  public static class DevicesSdk
  {
    private static readonly SemaphoreSlim InitSemaphore = new SemaphoreSlim(1);
    private static readonly Lazy<IOktaWebClient> LazyWebClient = new Lazy<IOktaWebClient>(new Func<IOktaWebClient>(DevicesSdk.CreateLazy<IOktaWebClient>));
    private static readonly Lazy<IAppAssemblyInformation> LazyAssemblyInfo = new Lazy<IAppAssemblyInformation>(new Func<IAppAssemblyInformation>(DevicesSdk.CreateLazy<IAppAssemblyInformation>));
    private static readonly Lazy<IWebRequestProperties> LazyWebRequestProperties = new Lazy<IWebRequestProperties>(new Func<IWebRequestProperties>(DevicesSdk.CreateLazy<IWebRequestProperties>));
    private static readonly Lazy<ITelemetryContext> LazyTelemetryContext = new Lazy<ITelemetryContext>(new Func<ITelemetryContext>(DevicesSdk.CreateLazy<ITelemetryContext>));
    private static readonly Lazy<IDeviceInformation> LazyDeviceDeviceInformation = new Lazy<IDeviceInformation>(new Func<IDeviceInformation>(DevicesSdk.CreateLazy<IDeviceInformation>));
    private static readonly Lazy<ISecureConnnectionValidator> LazySecureConnnectionValidator = new Lazy<ISecureConnnectionValidator>(new Func<ISecureConnnectionValidator>(DevicesSdk.CreateLazy<ISecureConnnectionValidator>));

    public static IOktaWebClient WebClient => DevicesSdk.LazyWebClient.Value;

    public static ISecureConnnectionValidator ConnectionValidator => DevicesSdk.LazySecureConnnectionValidator.Value;

    public static IAppAssemblyInformation AssemblyInformation => DevicesSdk.LazyAssemblyInfo.Value;

    public static IWebRequestProperties WebRequestProperties => DevicesSdk.LazyWebRequestProperties.Value;

    public static ITelemetryContext Telemetry => DevicesSdk.LazyTelemetryContext.Value;

    public static IDeviceInformation DeviceInformation => DevicesSdk.LazyDeviceDeviceInformation.Value;

    public static bool IsInitialized { get; private set; }

    public static void SetLogger() => OktaAuthenticator.Logger = AppInjector.Get<ILogger>();

    public static async Task EnsureInitialized()
    {
      if (DevicesSdk.IsInitialized)
        return;
      await DevicesSdk.InitSemaphore.WaitAsync().ConfigureAwait(true);
      try
      {
        if (DevicesSdk.IsInitialized)
          return;
        OktaAuthenticator.Initialize((IOktaDevicesDependencyProvider) new SdkDependencyProvider(await AppInjector.Get<IAuthenticatorSandboxManager>().GetSandbox().ConfigureAwait(true)));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        OktaAuthenticator.Initialize((IOktaDevicesDependencyProvider) new SdkDependencyProvider((IAuthenticatorSandbox) new BaseAuthenticatorSandbox()));
      }
      finally
      {
        DevicesSdk.IsInitialized = true;
        DevicesSdk.InitSemaphore.Release();
      }
    }

    public static void Close()
    {
      OktaAuthenticator.Close();
      DevicesSdk.InitSemaphore.Dispose();
    }

    private static TService CreateLazy<TService>() where TService : class => OktaAuthenticator.GetService<TService>();
  }
}
