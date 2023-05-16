// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Storage.ClientStorageManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInfo;
using Okta.Authenticator.NativeApp.Configuration;
using Okta.Authenticator.NativeApp.Exceptions;
using Okta.Authenticator.NativeApp.Interop;
using Okta.Authenticator.NativeApp.Models;
using Okta.Authenticator.NativeApp.Properties;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Storage
{
  public class ClientStorageManager : IClientStorageManager, IDisposable
  {
    private const string AppSettingsKey = "OVSettings";
    private readonly Lazy<IStorageManager> storeInitializer;
    private readonly ILogger logger;
    private readonly IConfigurationManager configurationManager;
    private bool disposedValue;

    public ClientStorageManager(IConfigurationManager configurationManager, ILogger logger)
    {
      this.configurationManager = configurationManager;
      this.logger = logger;
      this.storeInitializer = new Lazy<IStorageManager>(new Func<IStorageManager>(this.StoreInitializer), true);
    }

    public IStorageManager Store => this.storeInitializer.Value;

    public async Task<OktaVerifySettingsModel> GetAppSettings() => await this.Store.TryGetDataAsync<OktaVerifySettingsModel>("OVSettings").ConfigureAwait(false);

    public async Task<bool> UpdateAppSettings(Action<OktaVerifySettingsModel> updateAction)
    {
      if (updateAction == null)
        return false;
      OktaVerifySettingsModel data = await this.Store.TryGetDataAsync<OktaVerifySettingsModel>("OVSettings").ConfigureAwait(false);
      if (data == null)
        data = new OktaVerifySettingsModel()
        {
          Id = "OVSettings"
        };
      updateAction(data);
      bool flag = await this.Store.PutDataAsync<OktaVerifySettingsModel>("OVSettings", data).ConfigureAwait(false);
      this.logger.WriteInfoEx(string.Format("App settings updated in the store: {0}", (object) flag), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Storage\\ClientStorageManager.cs", nameof (UpdateAppSettings));
      return flag;
    }

    public void SubscribeToStoreUpdates(Action<StoreChangeType, IList> storeUpdateAction)
    {
      if (storeUpdateAction == null)
        return;
      this.Store.OnStoreDataUpdated += storeUpdateAction;
    }

    public void UnsubscribeFromStoreUpdates(Action<StoreChangeType, IList> storeUpdateAction)
    {
      if (!this.storeInitializer.IsValueCreated || storeUpdateAction == null)
        return;
      this.Store.OnStoreDataUpdated -= storeUpdateAction;
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
      if (disposing && this.storeInitializer.IsValueCreated)
        this.Store.SafeDispose();
      this.disposedValue = true;
    }

    private IStorageManager StoreInitializer()
    {
      this.configurationManager.EnsureApplicationDataFolderExists(this.logger);
      byte[] secret = (byte[]) null;
      try
      {
        secret = NativeLibrary.GetAppSecret(this.logger);
        IStorageManager result = this.InitializeAndValidateStore(secret, true).Result;
        if (result == null)
          this.RecoverCorruptedStore();
        return result;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("Failed to initialize storage manager.", ex);
        throw;
      }
      finally
      {
        if (secret != null)
          Array.Clear((Array) secret, 0, secret.Length);
      }
    }

    private async Task<IStorageManager> InitializeAndValidateStore(byte[] secret, bool canRecover)
    {
      SQLiteStorageManager store = (SQLiteStorageManager) null;
      try
      {
        store = new SQLiteStorageManager(this.configurationManager.ApplicationDataPath, this.configurationManager.ApplicationStoreFileNamePrefix, secret);
        List<OktaVerifyInformation> verifyInformationList = await store.GetAllAsync<OktaVerifyInformation>().ConfigureAwait(false);
        this.logger.WriteDebugEx("Using storage file " + Path.GetFileName(store.StoragePath), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Storage\\ClientStorageManager.cs", nameof (InitializeAndValidateStore));
        return (IStorageManager) store;
      }
      catch (SQLiteException ex) when (canRecover && ex.Result == SQLite3.Result.NonDBFile)
      {
        store?.Dispose();
        return (IStorageManager) null;
      }
    }

    private void RecoverCorruptedStore()
    {
      string path2 = string.Format("{0}_{1}.bkp", (object) this.configurationManager.ApplicationStoreFileNamePrefix, (object) DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
      string[] files = Directory.GetFiles(this.configurationManager.ApplicationDataPath, "*.bkp");
      if (files.Length > 2)
      {
        string str = files[0];
        for (int index = 1; index < files.Length; ++index)
        {
          if (string.Compare(str, files[index], StringComparison.Ordinal) < 0)
            str = files[index];
        }
        File.Delete(str);
      }
      this.logger.WriteErrorEx("Database is corrupted, creating new db and storing old content in " + path2, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Storage\\ClientStorageManager.cs", nameof (RecoverCorruptedStore));
      string str1 = Path.Combine(this.configurationManager.ApplicationDataPath, path2);
      if (!File.Exists(str1))
        File.Move(Path.Combine(this.configurationManager.ApplicationDataPath, this.configurationManager.ApplicationStoreFileNamePrefix + ".db"), str1);
      throw new OktaErrorReportException(string.Format("SQLite store corrupted: Org: {0}, Occurrences: {1}, IsMainBuild: {2}", (object) this.configurationManager.TryGetRegistryConfig<string>(this.logger, "SignInUrl", (string) null), (object) (files.Length + 1), (object) BuildSettings.IsMainBuild), nameof (RecoverCorruptedStore));
    }
  }
}
