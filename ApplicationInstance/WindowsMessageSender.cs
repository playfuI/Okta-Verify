// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.WindowsMessageSender
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.UI.Handlers;
using Okta.Authenticator.NativeApp.UI.Models;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public sealed class WindowsMessageSender : IWindowsMessageSender
  {
    private const int ErrorNotReady = -2147024875;
    private const int ErrorFail = -2147467259;
    private const int ErrorInvalidArg = -2147024809;
    private readonly ILogger logger;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly ISingleInstanceIdentifier singleInstanceIdentifier;
    private readonly IWindowActivationHandler windowHandler;
    private readonly Lazy<(string Name, int Id)> lazyProcessInfo = new Lazy<(string, int)>(new Func<(string, int)>(WindowsMessageSender.GetCurrentProcessInformation), true);

    public WindowsMessageSender(
      ILogger logger,
      IAnalyticsProvider analyticsProvider,
      ISingleInstanceIdentifier instanceIdentifier,
      IWindowActivationHandler windowHandler)
    {
      this.logger = logger;
      this.analyticsProvider = analyticsProvider;
      this.singleInstanceIdentifier = instanceIdentifier;
      this.windowHandler = windowHandler;
    }

    public int SignalInstanceAsync(SingletonSignals signals) => this.SignalInstance(new WindowsMessageSender.NativeSendOperation(Okta.Authenticator.NativeApp.Interop.NativeMethods.PostMessage), new Func<uint, bool>(WindowsMessageSender.NonZeroIsSuccess), signals, string.Format("posting {0} signals", (object) signals));

    public int SignalInstance(SingletonSignals signals) => this.SignalInstance(new WindowsMessageSender.NativeSendOperation(Okta.Authenticator.NativeApp.Interop.NativeMethods.SendMessage), new Func<uint, bool>(WindowsMessageSender.NonZeroIsSuccess), signals, string.Format("sending {0} signals", (object) signals));

    public int SendDataToInstance(byte[] data, SingletonSignals messageType)
    {
      if (data == null)
        return -2147024809;
      GCHandle data1 = new GCHandle();
      IntPtr num = IntPtr.Zero;
      try
      {
        data1 = GCHandle.Alloc((object) data, GCHandleType.Pinned);
        CopyDataStruct structure = new CopyDataStruct(messageType, data.Length, data1);
        num = Marshal.AllocCoTaskMem(Marshal.SizeOf<CopyDataStruct>(structure));
        Marshal.StructureToPtr<CopyDataStruct>(structure, num, false);
        (int Count, int Error) = this.InformAllInstances(new WindowsMessageSender.NativeSendOperation(Okta.Authenticator.NativeApp.Interop.NativeMethods.SendMessage), new Func<uint, bool>(WindowsMessageSender.NonZeroIsSuccess), 74U, num, "copying data", true);
        return Count > 0 ? 0 : Error;
      }
      finally
      {
        data1.Free();
        if (num != IntPtr.Zero)
          Marshal.FreeCoTaskMem(num);
      }
    }

    private static bool NonZeroIsSuccess(uint value) => value > 0U;

    private static (string Name, int id) GetCurrentProcessInformation()
    {
      using (Process currentProcess = Process.GetCurrentProcess())
        return (currentProcess.ProcessName, currentProcess.Id);
    }

    private static IEnumerable<IntPtr> GetWindowsByTitle(string processName, int currentProcessId)
    {
      foreach (NativeWindowModel nativeWindowModel in NativeWindowModel.FindWindowsByTitle(Resources.OktaVerifyLabel))
      {
        using (Process process = Process.GetProcessById((int) nativeWindowModel.ProcessId))
        {
          if (process.ProcessName == processName && process.Id != currentProcessId)
            yield return nativeWindowModel.Handle;
        }
      }
    }

    private int SignalInstance(
      WindowsMessageSender.NativeSendOperation sendOperation,
      Func<uint, bool> resultValidator,
      SingletonSignals signals,
      string messageDescription,
      bool sendSingle = false)
    {
      if (this.singleInstanceIdentifier.InstanceMessageId == 0U)
      {
        this.logger.LogAndReportToAnalytics(string.Format("Failed to send message because signalling message Id is not registered. Error 0x{0:X8}", (object) Okta.Authenticator.NativeApp.Interop.NativeMethods.GetLastError()), this.analyticsProvider);
        return -2147024875;
      }
      (int Count, int Error) = this.InformAllInstances(sendOperation, resultValidator, this.singleInstanceIdentifier.InstanceMessageId, (IntPtr) (int) signals, messageDescription, sendSingle);
      return Count <= 0 ? Error : 0;
    }

    private (int Count, int Error) InformAllInstances(
      WindowsMessageSender.NativeSendOperation sendOperation,
      Func<uint, bool> resultValidator,
      uint messageType,
      IntPtr payload,
      string messageDescription,
      bool sendSingle = false)
    {
      Process[] source = (Process[]) null;
      int num1 = 0;
      IntPtr sender = this.windowHandler.IsInitialized ? this.windowHandler.MainWindow.Handle : IntPtr.Zero;
      try
      {
        source = Process.GetProcessesByName(this.lazyProcessInfo.Value.Name);
        foreach (IntPtr receiver in ((IEnumerable<Process>) source).Where<Process>((Func<Process, bool>) (p => p.Id != this.lazyProcessInfo.Value.Id && p.MainWindowHandle != IntPtr.Zero)).Select<Process, IntPtr>((Func<Process, IntPtr>) (p => p.MainWindowHandle)).Union<IntPtr>(WindowsMessageSender.GetWindowsByTitle(this.lazyProcessInfo.Value.Name, this.lazyProcessInfo.Value.Id)))
        {
          try
          {
            uint num2 = sendOperation(receiver, messageType, sender, payload);
            if (resultValidator(num2))
            {
              ++num1;
              if (sendSingle)
                break;
            }
            else
            {
              uint lastError = Okta.Authenticator.NativeApp.Interop.NativeMethods.GetLastError();
              this.logger.WriteErrorEx(string.Format("An error occurred during {0}. Error 0x{1:X8}", (object) messageDescription, (object) lastError), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageSender.cs", nameof (InformAllInstances));
            }
          }
          catch (Exception ex) when (!ex.IsCritical())
          {
            this.HandleException(ex, messageDescription);
          }
        }
        if (num1 > 0)
        {
          this.logger.WriteDebugEx(string.Format("Successfully sent {0} messages {1} from sender window 0x{2:X8}.", (object) num1, (object) messageDescription, (object) sender), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageSender.cs", nameof (InformAllInstances));
          return (num1, 0);
        }
        string str = string.Format("Failed {0} from process ${1} from sender window 0x{2:X8}.", (object) messageDescription, (object) this.lazyProcessInfo.Value.Id, (object) sender);
        this.logger.WriteErrorEx(str, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\ApplicationInstance\\WindowsMessageSender.cs", nameof (InformAllInstances));
        this.analyticsProvider.TrackErrorWithLogs(str, sourceMethodName: nameof (InformAllInstances));
        return (num1, -2147467259);
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.HandleException(ex, messageDescription);
        return (num1, ex.HResult);
      }
      finally
      {
        if (source != null)
        {
          foreach (Component component in source)
            component.Dispose();
        }
      }
    }

    private void HandleException(Exception ex, string description)
    {
      string errorMessage = string.Format("An {0} with 0x{1:X8} occurred during {2}.", (object) ex.GetType().Name, (object) ex.HResult, (object) description);
      this.logger.WriteException(errorMessage + ": " + ex.Message, ex);
      this.analyticsProvider.TrackErrorWithLogsAndAppData(errorMessage, sourceMethodName: nameof (HandleException));
    }

    private delegate uint NativeSendOperation(
      IntPtr receiver,
      uint messageType,
      IntPtr sender,
      IntPtr data);
  }
}
