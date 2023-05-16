// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ApplicationInstance.SingleInstanceIdentifier
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Interop;
using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace Okta.Authenticator.NativeApp.ApplicationInstance
{
  public class SingleInstanceIdentifier : ISingleInstanceIdentifier, IDisposable
  {
    private readonly string mutexName;
    private readonly string instanceMessageIdString;
    private Mutex mutex;
    private bool isPrimaryInstance;
    private bool disposedValue;
    private uint instanceMessageId;

    public SingleInstanceIdentifier()
    {
      this.ApplicationIdentifier = SingleInstanceIdentifier.GetApplicationIdentifier();
      this.mutexName = "Local\\" + this.ApplicationIdentifier;
      this.instanceMessageIdString = "WM_SIGNALVERIFYINSTANCE|" + this.ApplicationIdentifier;
      this.mutex = new Mutex(true, this.mutexName, out this.isPrimaryInstance);
      this.instanceMessageId = 0U;
    }

    public string ApplicationIdentifier { get; }

    public bool IsPrimaryInstance => this.isPrimaryInstance;

    public uint InstanceMessageId => this.EnsureInstanceMessageId();

    public bool TryPromoteInstance()
    {
      if (this.mutex == null)
        return false;
      this.mutex.Dispose();
      this.mutex = new Mutex(true, this.mutexName, out this.isPrimaryInstance);
      return this.isPrimaryInstance;
    }

    public bool TryDemoteInstance()
    {
      if (this.mutex == null)
        return false;
      if (!this.isPrimaryInstance)
        return true;
      this.mutex.ReleaseMutex();
      this.isPrimaryInstance = false;
      return true;
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposedValue)
        return;
      if (disposing)
        this.mutex?.Dispose();
      this.disposedValue = true;
    }

    private static string GetApplicationIdentifier()
    {
      Assembly executingAssembly = Assembly.GetExecutingAssembly();
      return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{{{0}}}{{{1}}}", (object) executingAssembly.GetType().GUID, (object) executingAssembly.GetName().Name);
    }

    private uint EnsureInstanceMessageId()
    {
      if (this.instanceMessageId > 0U)
        return this.instanceMessageId;
      this.instanceMessageId = NativeMethods.RegisterWindowMessage(this.instanceMessageIdString);
      return this.instanceMessageId;
    }
  }
}
