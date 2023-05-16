// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.AboutViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Injector;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class AboutViewModel : BaseViewModel
  {
    private static readonly Lazy<string> LazyVersionText = new Lazy<string>(new Func<string>(AboutViewModel.GetVersionText));
    private static readonly Lazy<string> LazyCopyrightText = new Lazy<string>(new Func<string>(AboutViewModel.GetCopyrightText));

    public AboutViewModel() => this.NavigateToCommand = (ICommand) new DelegateCommand<string>(new Action<string>(((BaseViewModel) this).LaunchLink));

    public string VersionText => AboutViewModel.LazyVersionText.Value;

    public string CopyrightText => AboutViewModel.LazyCopyrightText.Value;

    public ICommand NavigateToCommand { get; }

    private static string GetVersionText() => string.Format("{0} {1}", (object) Resources.OktaVerifyVersionText, (object) DevicesSdk.AssemblyInformation.ApplicationVersion);

    private static string GetCopyrightText() => DevicesSdk.AssemblyInformation.ApplicationCopyright;
  }
}
