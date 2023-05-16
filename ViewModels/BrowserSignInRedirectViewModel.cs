// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.BrowserSignInRedirectViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Devices.SDK.Extensions;
using Prism.Commands;
using System;
using System.Threading;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class BrowserSignInRedirectViewModel : BaseViewModel, IDisposable
  {
    private readonly CancellationTokenSource cancellationSource;
    private bool disposedValue;

    public BrowserSignInRedirectViewModel()
    {
      this.cancellationSource = new CancellationTokenSource();
      this.CancelSignInTaskCommand = (ICommand) new DelegateCommand(new Action(this.CancelSignInTask));
    }

    public ICommand CancelSignInTaskCommand { get; }

    public bool IsSignInCancelled { get; private set; }

    public CancellationToken SignInCancellationToken => this.cancellationSource.Token;

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
        this.cancellationSource.Dispose();
      this.disposedValue = true;
    }

    private void CancelSignInTask()
    {
      try
      {
        if (!this.cancellationSource.Token.CanBeCanceled)
          return;
        this.cancellationSource.Cancel();
        this.IsSignInCancelled = true;
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.Logger.WriteErrorEx("An error occurred while cancelling browser sign-in: " + ex.Message, "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\BrowserSignInRedirectViewModel.cs", nameof (CancelSignInTask));
        this.AnalyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }
  }
}
