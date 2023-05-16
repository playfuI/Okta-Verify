// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.SingletonHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using System;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public sealed class SingletonHandler : ISingletonHandler
  {
    private static readonly int PromotionRetryDelayInMilliseconds = 200;
    private readonly ILogger logger;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly ISingleInstanceIdentifier instanceIdentifier;
    private readonly IWindowsMessageSender messageSender;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IApplicationHandler applicationHandler;

    public SingletonHandler(
      ISingleInstanceIdentifier instanceIdentifier,
      IWindowsMessageSender messageSender,
      ILogger logger,
      IAnalyticsProvider analytics,
      IApplicationStateMachine stateMachine,
      IApplicationHandler applicationHandler)
    {
      this.instanceIdentifier = instanceIdentifier;
      this.messageSender = messageSender;
      this.logger = logger;
      this.analyticsProvider = analytics;
      this.stateMachine = stateMachine;
      this.applicationHandler = applicationHandler;
      this.Initialize();
    }

    public bool IsPrimaryInstance => this.instanceIdentifier.IsPrimaryInstance;

    public bool SignalMainInstance(SingletonSignals signals)
    {
      this.logger.WriteInfoEx(string.Format("Signaling {0} to primary instance...", (object) signals), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (SignalMainInstance));
      if (this.IsPrimaryInstance)
        return false;
      int num = this.messageSender.SignalInstanceAsync(signals);
      if (num == 0)
        return true;
      this.logger.WriteWarningEx(string.Format("Failed to signal main instance with error 0x{0:X8}.", (object) num), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (SignalMainInstance));
      return false;
    }

    public bool SendDataToMainInstance(byte[] data, SingletonSignals messageType)
    {
      this.logger.WriteInfoEx("Send data to primary instance...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (SendDataToMainInstance));
      if (this.IsPrimaryInstance)
        return false;
      int instance = this.messageSender.SendDataToInstance(data, messageType);
      if (instance == 0)
        return true;
      this.logger.WriteWarningEx(string.Format("Failed to send data to main instance with error 0x{0:X8}.", (object) instance), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (SendDataToMainInstance));
      return false;
    }

    public async Task<bool> PromoteInstance()
    {
      try
      {
        int retry = 0;
        while (!this.IsPrimaryInstance)
        {
          if (retry < 3)
          {
            if (this.instanceIdentifier.TryPromoteInstance())
            {
              this.logger.WriteInfoEx("Successfully promoted current application instance", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (PromoteInstance));
            }
            else
            {
              this.logger.WriteWarningEx(string.Format("Failed to promote current application instance after {0} attempts.", (object) retry), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (PromoteInstance));
              await Task.Delay(SingletonHandler.PromotionRetryDelayInMilliseconds).ConfigureAwait(true);
              ++retry;
            }
          }
          else
            break;
        }
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("An error occurred while attempting to promote the current instance: ", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return this.IsPrimaryInstance;
    }

    public bool DemoteOrSignalShutdown()
    {
      if (!this.IsPrimaryInstance)
        return this.SignalMainInstance(SingletonSignals.Shutdown);
      try
      {
        this.logger.WriteInfoEx("Demoting current primary instance...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (DemoteOrSignalShutdown));
        this.logger.WriteInfoEx(string.Format("Primary instance demoted: {0}", (object) this.applicationHandler.InvokeOnUIThread<bool>(new Func<bool>(this.instanceIdentifier.TryDemoteInstance))), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (DemoteOrSignalShutdown));
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("An error occurred while attempting to demote the primary instance: ", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      return !this.IsPrimaryInstance;
    }

    private void Initialize()
    {
      this.stateMachine.RegisterDeferral(ComputingStateType.Loading, (ComputingStateDeferral) (s => this.ForcePrimary()), "forcing primary instance");
      this.stateMachine.RegisterDeferral(ComputingStateType.ShuttingDown, (ComputingStateDeferral) (s => Task.Run((Action) (() => this.HandleShutdown(s)))), "Singleton shutdown");
    }

    private async Task ForcePrimary()
    {
      if (this.IsPrimaryInstance)
        return;
      this.SignalMainInstance(SingletonSignals.Shutdown);
      int num = await this.PromoteInstance().ConfigureAwait(true) ? 1 : 0;
    }

    private void SignalToPrimary(SingletonSignals signal)
    {
      if (this.IsPrimaryInstance)
        return;
      this.SignalMainInstance(signal);
    }

    private void HandleShutdown(ComputingStateContext context)
    {
      switch (context.Command)
      {
        case StartupArgumentType.None:
          this.SignalToPrimary(SingletonSignals.Activate);
          break;
        case StartupArgumentType.Background:
          this.SignalToPrimary(SingletonSignals.CheckBindings);
          break;
        case StartupArgumentType.Shutdown:
          this.DemoteOrSignalShutdown();
          break;
        case StartupArgumentType.ShowReportIssue:
          this.SignalToPrimary(SingletonSignals.OpenReportIssueWindow);
          break;
        case StartupArgumentType.ShowAbout:
          this.SignalToPrimary(SingletonSignals.OpenAboutWindow);
          break;
        case StartupArgumentType.ShowSettings:
          this.SignalToPrimary(SingletonSignals.OpenSettingsWindow);
          break;
        default:
          this.logger.WriteInfoEx(string.Format("Shutdown argument {0} will not be processed.", (object) context.Command), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\SingletonHandler.cs", nameof (HandleShutdown));
          break;
      }
    }
  }
}
