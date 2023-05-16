// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.UI.Models.AttachedWindowModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Interop;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;

namespace Okta.Authenticator.NativeApp.UI.Models
{
  internal class AttachedWindowModel
  {
    private const int SwpDoNotActivate = 16;
    private NativeMethods.WindowPosition previousMainWindowPosition;

    public AttachedWindowModel(NativeWindowModel attachedWindow, NativeWindowModel mainWindow)
    {
      this.AttachedWindow = attachedWindow;
      this.MainWindow = mainWindow;
      this.previousMainWindowPosition = new NativeMethods.WindowPosition();
    }

    public NativeWindowModel AttachedWindow { get; }

    public NativeWindowModel MainWindow { get; }

    public bool IsAligned { get; private set; }

    public bool Align(ILogger logger)
    {
      this.previousMainWindowPosition = new NativeMethods.WindowPosition();
            return this.IsAligned;
    }

    public bool Restore()
    {
      if (this.IsAligned)
        this.IsAligned = !NativeMethods.SetWindowPos(this.MainWindow.Handle, this.AttachedWindow.Handle, this.previousMainWindowPosition.Left, this.previousMainWindowPosition.Top, this.previousMainWindowPosition.Width, this.previousMainWindowPosition.Height, 16);
      NativeMethods.SwitchToThisWindow(this.AttachedWindow.Handle, true);
      return !this.IsAligned;
    }
  }
}
