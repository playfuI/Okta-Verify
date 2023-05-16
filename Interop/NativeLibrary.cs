// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Interop.NativeLibrary
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Devices.SDK.Windows.Native;
using Okta.Devices.SDK.Windows.Native.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;

namespace Okta.Authenticator.NativeApp.Interop
{
  internal static class NativeLibrary
  {
    internal static byte[] CreateClientIdentifier(string clientName, ILogger logger)
    {
      SafeProcessHeapByteArrayHandle pbIdentifier = (SafeProcessHeapByteArrayHandle) null;
      try
      {
        uint cbIdentifier = 0;
        int instanceIdentifier = NativeMethods.CreateClientInstanceIdentifier(clientName, (uint) clientName.Length, out pbIdentifier, ref cbIdentifier, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
        if (instanceIdentifier < 0)
          throw new WindowsCryptographicException("Failed to create client identifier.", instanceIdentifier);
        return pbIdentifier.ToByteArray((int) cbIdentifier);
      }
      finally
      {
        pbIdentifier?.Dispose();
      }
    }

    internal static SecureString LoadClientAssociation(
      string clientName,
      byte[] identifier,
      ILogger logger)
    {
      SafeProcessHeapSecureStringHandle pbSecret = (SafeProcessHeapSecureStringHandle) null;
      try
      {
        uint cbSecret = 0;
        int hResult = NativeMethods.LoadClientIdentifier(clientName, (uint) clientName.Length, identifier, (uint) identifier.Length, out pbSecret, ref cbSecret, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
        if (hResult < 0)
          throw new WindowsCryptographicException("Failed to create client identifier.", hResult);
        return pbSecret.ToSecureString((int) cbSecret / 2);
      }
      finally
      {
        pbSecret?.Dispose();
      }
    }

    internal static byte[] GetAppSecret(ILogger logger)
    {
      SafeProcessHeapByteArrayHandle pbSecret = (SafeProcessHeapByteArrayHandle) null;
      try
      {
        uint cbSecret = 0;
        int appSecret = NativeMethods.GetAppSecret(out pbSecret, ref cbSecret, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
        if (appSecret < 0)
          throw new WindowsCryptographicException("GetAppSecret failed", appSecret);
        return pbSecret.ToByteArray((int) cbSecret);
      }
      finally
      {
        pbSecret?.Dispose();
      }
    }

    internal static string GetAppCenterKey(ILogger logger)
    {
      SafeProcessHeapByteArrayHandle data = (SafeProcessHeapByteArrayHandle) null;
      uint cbData = 0;
      try
      {
        int appCenterKey = NativeMethods.GetAppCenterKey(out data, ref cbData, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
        if (appCenterKey < 0)
          throw new WindowsCryptographicException("DeObfuscateHelper failed.", appCenterKey);
        if (cbData != 16U)
          throw new OktaException("Unexpected length of data.");
        return new Guid(data.ToByteArray((int) cbData)).ToString("D", (IFormatProvider) CultureInfo.InvariantCulture);
      }
      finally
      {
        data?.Dispose();
      }
    }

    internal static NativeOperationResult<IEnumerable<(string Name, string Sid)>> GetUsersInfo(
      ILogger logger)
    {
      SafeProcessHeapStructHandle pUserInfo = (SafeProcessHeapStructHandle) null;
      try
      {
        int cUserInfo;
        int usersInfo = NativeMethods.GetUsersInfo(out cUserInfo, out pUserInfo, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
        return new NativeOperationResult<IEnumerable<(string, string)>>(usersInfo == 0 ? ((IEnumerable<NativeMethods.UserInfo>) pUserInfo.ToStructArray<NativeMethods.UserInfo>(cUserInfo)).Select<NativeMethods.UserInfo, (string, string)>((Func<NativeMethods.UserInfo, (string, string)>) (u => (u.Name, u.Sid))) : Enumerable.Empty<(string, string)>(), usersInfo, nameof (GetUsersInfo));
      }
      finally
      {
        pUserInfo?.Dispose();
      }
    }

    internal static NativeOperationResult<MachineJoinStatusFlags> GetMachineJoinStatus(
      ILogger logger)
    {
      MachineJoinStatusFlags machineStatus;
      int machineJoinStatus = NativeMethods.GetMachineJoinStatus(out machineStatus, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
      return new NativeOperationResult<MachineJoinStatusFlags>(machineStatus, machineJoinStatus, nameof (GetMachineJoinStatus));
    }

    internal static NativeOperationResult<(int TotalMemory, int VirtualMemory, SystemInfoProcessorArchitecture Architecture, int CoreCount)> GetSystemInfo(
      ILogger logger)
    {
      int totalMemory;
      int totalVirtualMemory;
      SystemInfoProcessorArchitecture processorArchitecture;
      int processorCount;
      int machinePerformanceInfo = NativeMethods.GetMachinePerformanceInfo(out totalMemory, out totalVirtualMemory, out processorArchitecture, out int _, out processorCount, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
      return new NativeOperationResult<(int, int, SystemInfoProcessorArchitecture, int)>((totalMemory, totalVirtualMemory, processorArchitecture, processorCount), machinePerformanceInfo, nameof (GetSystemInfo));
    }

    internal static NativeOperationResult<KeyProviderImplementationTypes> GetKeyProviderImplementationType(
      string provider,
      ILogger logger)
    {
      KeyProviderImplementationTypes implementationTypes;
      int implementationType = NativeMethods.GetProviderImplementationType(provider, false, out implementationTypes, new NativeMethods.LoggingCallback(((LoggerExtensions) logger).Write));
      return new NativeOperationResult<KeyProviderImplementationTypes>(implementationTypes, implementationType, nameof (GetKeyProviderImplementationType));
    }
  }
}
