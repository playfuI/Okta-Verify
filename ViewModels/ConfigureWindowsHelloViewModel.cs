// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.ConfigureWindowsHelloViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Authentication;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Telemetry;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class ConfigureWindowsHelloViewModel : AsyncInlineScenarioViewModel<bool>
  {
    private const int WIN_HELLO_RETRY_MS = 2000;
    private const int WIN_HELLO_CHECK_COUNT = 150;
    internal static int WIN_HELLO_WAIT_MS = 2000;
    private readonly IClientSignInManager signInManager;
    private readonly IDeviceConfigurationManager deviceConfigurationManager;

    public ConfigureWindowsHelloViewModel(string enrollEmail, bool enforceUserVerification)
      : base(AppTelemetryScenario.BiometricsSetup)
    {
      this.CurrentEnrollEmail = enrollEmail;
      this.EnforceUserVerification = enforceUserVerification;
      this.signInManager = AppInjector.Get<IClientSignInManager>();
      this.deviceConfigurationManager = AppInjector.Get<IDeviceConfigurationManager>();
      this.ConfigureWindowsHelloCommand = (ICommand) new DelegateCommand(new Action(this.ConfigureWindowsHello));
      this.CancelWindowsHelloConfigurationCommand = (ICommand) new DelegateCommand(new Action(this.CancelWindowsHelloConfiguration));
    }

    public string CurrentEnrollEmail { get; }

    public bool EnforceUserVerification { get; }

    public string ExtraVerificationRequiredText => Resources.ExtraVerificationOrgRequiresWindowsHello;

    public ICommand ConfigureWindowsHelloCommand { get; }

    public ICommand CancelWindowsHelloConfigurationCommand { get; }

    internal void CancelWindowsHelloConfiguration() => this.EndTelemetryTracking(false, TelemetryEventStatus.UserCancelled, "UserCancel");

    private void ConfigureWindowsHello() => this.ConfigureWindowsHelloAsync().AsBackgroundTask("Windows Hello configuration", this.Logger);

    private async Task ConfigureWindowsHelloAsync()
    {
      ConfigureWindowsHelloViewModel windowsHelloViewModel = this;
      bool flag = windowsHelloViewModel.signInManager.CheckIfWindowsHelloSignInConfigured();
      if (flag)
      {
        windowsHelloViewModel.TrySetResult(true);
      }
      else
      {
        windowsHelloViewModel.StartTelemetryTracking();
        int checkCount = 0;
        try
        {
          windowsHelloViewModel.LaunchLink(windowsHelloViewModel.deviceConfigurationManager.WindowsHelloConfigurationLink);
          for (; !flag && checkCount++ < 150 && !windowsHelloViewModel.IsFinished; flag = windowsHelloViewModel.signInManager.CheckIfWindowsHelloSignInConfigured())
            await Task.Delay(2000).ConfigureAwait(true);
          if (flag)
            windowsHelloViewModel.EndTelemetryTracking(true, TelemetryEventStatus.Success, (string) null);
          else
            windowsHelloViewModel.EndTelemetryTracking(false, TelemetryEventStatus.Abandoned, "Timeout");
        }
        catch (Exception ex) when (!ex.IsCritical())
        {
          windowsHelloViewModel.EndTelemetryTracking(false, ex);
          windowsHelloViewModel.Logger.WriteErrorEx("Failed to set up Windows Hello: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\ConfigureWindowsHelloViewModel.cs", nameof (ConfigureWindowsHelloAsync));
          windowsHelloViewModel.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
        }
      }
    }
  }
}
