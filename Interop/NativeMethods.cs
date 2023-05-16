// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.NativeMethods
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.UI.Enums;
using Okta.Devices.SDK.Windows.Native;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Okta.Authenticator.NativeApp.Interop
{
  internal static class NativeMethods
  {
    private const string SdkNativeLib = "Okta.Devices.SDK.Windows.Native.dll";
    private const string AppNativeLib = "OktaVerify.Native.dll";
    private const string UserDll = "user32.dll";
    private const string KernelDll = "kernel32.dll";

    [DllImport("Okta.Devices.SDK.Windows.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Error)]
    public static extern int FreeProcessHeapMemory(IntPtr pHandle);

    [DllImport("Okta.Devices.SDK.Windows.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Error)]
    public static extern int GetProviderImplementationType(
      string providerName,
      bool confirmedSupported,
      out KeyProviderImplementationTypes implementationTypes,
      [MarshalAs(UnmanagedType.FunctionPtr)] NativeMethods.LoggingCallback loggingCallback);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CreateClientInstanceIdentifier(
      string clientName,
      uint clientNameLen,
      out SafeProcessHeapByteArrayHandle pbIdentifier,
      ref uint cbIdentifier,
      [MarshalAs(UnmanagedType.FunctionPtr)] NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern int LoadClientIdentifier(
      string clientName,
      uint clientNameLen,
      [MarshalAs(UnmanagedType.LPArray)] byte[] pbIdentifier,
      uint cbIdentifier,
      out SafeProcessHeapSecureStringHandle pbSecret,
      ref uint cbSecret,
      [MarshalAs(UnmanagedType.FunctionPtr)] NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetAppSecret(
      out SafeProcessHeapByteArrayHandle pbSecret,
      ref uint cbSecret,
      [MarshalAs(UnmanagedType.FunctionPtr)] NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetAppCenterKey(
      out SafeProcessHeapByteArrayHandle data,
      ref uint cbData,
      [MarshalAs(UnmanagedType.FunctionPtr)] NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Error)]
    public static extern int GetUsersInfo(
      out int cUserInfo,
      out SafeProcessHeapStructHandle pUserInfo,
      NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Error)]
    public static extern int GetMachineJoinStatus(
      out MachineJoinStatusFlags machineStatus,
      NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Error)]
    public static extern int GetMachinePerformanceInfo(
      out int totalMemory,
      out int totalVirtualMemory,
      out SystemInfoProcessorArchitecture processorArchitecture,
      out int processorLevel,
      out int processorCount,
      NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OverlayApplicationWindows(
      IntPtr hwndReference,
      IntPtr hwndToAlign,
      ref NativeMethods.WindowPosition oldPosition,
      NativeMethods.LoggingCallback fpLog);

    [DllImport("OktaVerify.Native.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowState(IntPtr hWnd, out NativeWindowState state);

    [DllImport("user32.dll")]
    public static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern uint PostMessage(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows([MarshalAs(UnmanagedType.FunctionPtr)] NativeMethods.EnumerateWindows filter, IntPtr callback);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hwnd, bool fSwitch);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(
      IntPtr hWnd,
      IntPtr hWndInsertAfter,
      int Left,
      int Top,
      int Width,
      int Height,
      int Flags);

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern ushort SetThreadUILanguage(ushort langId);

    internal delegate void LoggingCallback(Okta.Devices.SDK.LogLevel logLevel, [MarshalAs(UnmanagedType.LPStr)] string component, [MarshalAs(UnmanagedType.LPStr)] string message);

    internal delegate bool EnumerateWindows(IntPtr hWnd, IntPtr callback);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct UserInfo
    {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      internal string Name;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      internal string Sid;
    }

    internal struct WindowPosition
    {
      public int Left;
      public int Top;
      public int Width;
      public int Height;
    }
  }
}
