// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.IApplicationHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Models;
using Okta.Devices.SDK;
using Okta.Oidc.Abstractions;
using System;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace Okta.Authenticator.NativeApp
{
  public interface IApplicationHandler : IApplicationInteraction
  {
    AppThemeSettingType DefaultAppTheme { get; }

    JumpList JumpList { get; set; }

    OktaClientConfiguration SetDefaultSystemBrowser(OktaClientConfiguration configuration);

    OktaClientConfiguration SetDefaultIntegratedBrowser(OktaClientConfiguration configuration);

    Task<AppThemeSettingType> GetSavedAppTheme();

    Task<bool> TryUpdateAppTheme(AppThemeSettingType themeName);

    bool InvokeUri(string address);

    void InvokeOnUIThread(Action action);

    T InvokeOnUIThread<T>(Func<T> action);

    Task InvokeOnUIThreadAsync(Func<Task> action);

    void ShowAboutWindow();

    void ShowReportIssueWindow();

    void ShowSettings();
  }
}
