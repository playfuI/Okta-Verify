// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.SignInConfirmationViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Bindings;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Devices.SDK.Extensions;
using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Threading;
using System.Windows.Media.Imaging;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class SignInConfirmationViewModel : BaseConfirmDialogViewModel, IDisposable
  {
    private const int ConfirmationTimeout = 60000;
    private readonly CancellationTokenSource cancellationSource;
    private bool disposedValue;
    private string initials;

    public SignInConfirmationViewModel(BindingInformationModel bindingInformation)
      : base(DialogViewType.SignInConfirmation)
    {
      this.ConfirmMainText = Resources.URIVerificationConsentText;
      this.ConfirmLabel = Resources.URIVerificationConsentYes;
      this.CancelLabel = Resources.URIVerificationConsentNo;
      this.Originator = bindingInformation?.AppName;
      this.UserInfo = bindingInformation?.UserEmail;
      this.RequestReferrer = OktaWebClientExtensions.TryGetFQDN(bindingInformation.RequestReferrer);
      this.AccountLogo = bindingInformation?.Logo;
      this.RegisterToCancellations(out this.cancellationSource);
    }

    public string Originator { get; }

    public BitmapImage AccountLogo { get; }

    public string Initials
    {
      get
      {
        if (this.initials == null)
          this.initials = string.IsNullOrWhiteSpace(this.Originator) ? string.Empty : this.Originator.Substring(0, 1).ToUpperInvariant();
        return this.initials;
      }
    }

    public string RequestReferrer { get; set; }

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

    private void RegisterToCancellations(out CancellationTokenSource cancellationSource)
    {
      cancellationSource = new CancellationTokenSource(60000);
      cancellationSource.Token.Register((Action) (() =>
      {
        try
        {
          if (this.ConfirmTaskSource.Task.IsCompleted)
            return;
          this.Logger.WriteWarningEx(string.Format("Time out after {0} ms; considering the confirmation request denied...", (object) 60000), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\ViewModels\\SignInConfirmationViewModel.cs", nameof (RegisterToCancellations));
          this.ConfirmTaskSource.TrySetResult(false);
        }
        catch (Exception ex) when (!ex.IsCritical())
        {
          this.Logger.WriteException("An error occurred while cancelling the confirmation request:", ex);
        }
      }));
    }
  }
}
