// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.UI.Models.NativeWindowModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Okta.Authenticator.NativeApp.UI.Models
{
  public class NativeWindowModel
  {
    private string title;
    private uint processId;
    private uint threadId;

    public NativeWindowModel(IntPtr windowHandle) => this.Handle = windowHandle;

    public IntPtr Handle { get; }

    public bool IsValid => this.Handle != IntPtr.Zero;

    public string Title
    {
      get
      {
        if (this.title == null)
          this.title = NativeWindowModel.GetWindowText(this.Handle);
        return this.title;
      }
    }

    public uint ProcessId
    {
      get
      {
        if (this.processId == 0U)
          this.GetThreadAndProcessId();
        return this.processId;
      }
    }

    public uint ThreadId
    {
      get
      {
        if (this.threadId == 0U)
          this.GetThreadAndProcessId();
        return this.threadId;
      }
    }

    public static bool TryGetForegroundWindow(out NativeWindowModel nativeWindow)
    {
      IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
      nativeWindow = (NativeWindowModel) null;
      if (!(foregroundWindow != IntPtr.Zero))
        return false;
      nativeWindow = new NativeWindowModel(foregroundWindow);
      return true;
    }

    public static IEnumerable<NativeWindowModel> FindWindowsByTitle(string titleText)
    {
      List<IntPtr> windows = new List<IntPtr>();
      NativeMethods.EnumWindows((NativeMethods.EnumerateWindows) ((hWnd, param) =>
      {
        string windowText = NativeWindowModel.GetWindowText(hWnd);
        if ((windowText != null ? (windowText.Contains(titleText) ? 1 : 0) : 0) != 0)
          windows.Add(hWnd);
        return true;
      }), IntPtr.Zero);
      return windows.Select<IntPtr, NativeWindowModel>((Func<IntPtr, NativeWindowModel>) (h => new NativeWindowModel(h)));
    }

    public static string GetWindowText(IntPtr hWnd)
    {
      if (hWnd == IntPtr.Zero)
        return (string) null;
      int windowTextLength = NativeMethods.GetWindowTextLength(hWnd);
      if (windowTextLength <= 0)
        return (string) null;
      StringBuilder strText = new StringBuilder(windowTextLength + 1);
      NativeMethods.GetWindowText(hWnd, strText, strText.Capacity);
      return strText.ToString();
    }

    public override string ToString() => string.Format("{0} [{1}]", (object) this.Title, (object) this.Handle);

    private void GetThreadAndProcessId() => this.threadId = NativeMethods.GetWindowThreadProcessId(this.Handle, out this.processId);
  }
}
