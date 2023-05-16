// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.UI.Handlers.IWindowActivationHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.UI.Enums;
using Okta.Authenticator.NativeApp.UI.Models;

namespace Okta.Authenticator.NativeApp.UI.Handlers
{
  public interface IWindowActivationHandler
  {
    bool IsInitialized { get; }

    bool IsAttached { get; }

    NativeWindowModel MainWindow { get; }

    bool TryGetMainWindowState(out NativeWindowState state);

    void Initialize(NativeWindowModel mainWindow);

    bool AttachToCurrentForegroundWindow();

    bool AlignWindows();

    bool ForceMainWindowActivation(
      out (string ErrorMessage, uint ErrorCode) errorHandle);

    void RestoreToPreviousWindow();

    void ClearAttachedWindow();
  }
}
