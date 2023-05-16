// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.UI.Handlers.WindowActivationHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.UI.Enums;
using Okta.Authenticator.NativeApp.UI.Models;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using System.Security;

namespace Okta.Authenticator.NativeApp.UI.Handlers
{
  internal class WindowActivationHandler : IWindowActivationHandler
  {
    private readonly ILogger logger;
    private AttachedWindowModel attachement;

    public WindowActivationHandler(ILogger logger)
    {
      this.logger = logger;
      this.attachement = (AttachedWindowModel) null;
      this.MainWindow = (NativeWindowModel) null;
    }

    public bool IsInitialized
    {
      get
      {
        NativeWindowModel mainWindow = this.MainWindow;
        return mainWindow != null && mainWindow.IsValid;
      }
    }

    public bool IsAttached
    {
      get
      {
        AttachedWindowModel attachement = this.attachement;
        return attachement != null && attachement.IsAligned;
      }
    }

    public NativeWindowModel MainWindow { get; private set; }

    public bool TryGetMainWindowState(out NativeWindowState state)
    {
      if (this.IsInitialized)
        return NativeMethods.GetWindowState(this.MainWindow.Handle, out state);
      state = NativeWindowState.Unknown;
      return false;
    }

    public void Initialize(NativeWindowModel mainWindow)
    {
      if (mainWindow == null || !mainWindow.IsValid)
        return;
      this.MainWindow = mainWindow;
    }

    public bool AttachToCurrentForegroundWindow()
    {
      NativeWindowModel nativeWindow;
      if (this.IsInitialized && NativeWindowModel.TryGetForegroundWindow(out nativeWindow) && nativeWindow.Handle != this.MainWindow.Handle)
      {
        this.attachement = new AttachedWindowModel(nativeWindow, this.MainWindow);
        this.logger.WriteDebugEx(string.Format("Attached to foreground window: {0}", (object) nativeWindow), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\WindowActivationHandler.cs", nameof (AttachToCurrentForegroundWindow));
        return true;
      }
      this.logger.WriteDebugEx("Did not attach to foreground window", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\WindowActivationHandler.cs", nameof (AttachToCurrentForegroundWindow));
      return false;
    }

    public bool AlignWindows()
    {
      AttachedWindowModel attachement = this.attachement;
      return attachement != null && attachement.Align(this.logger);
    }

    [SecurityCritical]
    public bool ForceMainWindowActivation(out (string ErrorMessage, uint ErrorCode) error)
    {
      error = ;
      if (this.attachement == null)
        return false;
      bool flag1 = (int) this.attachement.AttachedWindow.ThreadId == (int) this.attachement.MainWindow.ThreadId;
      bool flag2 = false;
      this.logger.WriteInfoEx("Activating window from " + (flag1 ? "UI" : "background") + " thread.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\WindowActivationHandler.cs", nameof (ForceMainWindowActivation));
      if (!flag1)
      {
        flag2 = NativeMethods.AttachThreadInput(this.attachement.AttachedWindow.ThreadId, this.attachement.MainWindow.ThreadId, true);
        if (!flag2)
          error = this.GetAndLogNativeError("Failed to attach our thread to foreground thread.");
      }
      try
      {
        if (NativeMethods.SetForegroundWindow(this.attachement.MainWindow.Handle))
          return true;
        error = this.GetAndLogNativeError("Failed to set foreground window.");
        return false;
      }
      finally
      {
        if (flag2 && !NativeMethods.AttachThreadInput(this.attachement.AttachedWindow.ThreadId, this.attachement.MainWindow.ThreadId, false))
        {
          (string, uint) andLogNativeError = this.GetAndLogNativeError("Failed to detach the background thread from the UI thread.");
          (string, uint) valueTuple = error;
          if (valueTuple.Item1 == (string) null && valueTuple.Item2 == 0U)
            error = andLogNativeError;
        }
      }
    }

    public void RestoreToPreviousWindow()
    {
      if (this.attachement == null)
        return;
      this.logger.WriteDebugEx(string.Format("Restoring previous foreground window {0}", (object) this.attachement.AttachedWindow), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\WindowActivationHandler.cs", nameof (RestoreToPreviousWindow));
      this.attachement.Restore();
    }

    public void ClearAttachedWindow() => this.attachement = (AttachedWindowModel) null;

    private (string ErrorMessage, uint ErrorCode) GetAndLogNativeError(string message)
    {
      uint lastError = NativeMethods.GetLastError();
      string message1 = string.Format("{0} Code: 0x{1:X8}.", (object) message, (object) lastError);
      this.logger.WriteDebugEx(string.Format("Attached window: {0}", (object) this.attachement?.AttachedWindow), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\WindowActivationHandler.cs", nameof (GetAndLogNativeError));
      this.logger.WriteWarningEx(message1, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Handlers\\WindowActivationHandler.cs", nameof (GetAndLogNativeError));
      return (message1, lastError);
    }
  }
}
