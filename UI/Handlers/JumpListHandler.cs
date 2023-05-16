// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.UI.Handlers.JumpListHandler
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance;
using Okta.OktaVerify.Windows.Core.Properties;
using System;
using System.Collections.Generic;
using System.Windows.Shell;

namespace Okta.Authenticator.NativeApp.UI.Handlers
{
  public class JumpListHandler : IJumpListHandler
  {
    public static readonly Dictionary<string, System.Windows.Shell.JumpTask> JumpTaskDict = new Dictionary<string, System.Windows.Shell.JumpTask>()
    {
      {
        Okta.Authenticator.NativeApp.UI.Enums.JumpTask.AboutJumpTask.ToString(),
        new System.Windows.Shell.JumpTask()
        {
          Title = Resources.OktaAboutTitle,
          Arguments = JumpListHandler.GetArgumentFromStartupArgument(StartupArgumentType.ShowAbout)
        }
      },
      {
        Okta.Authenticator.NativeApp.UI.Enums.JumpTask.SettingsJumpTask.ToString(),
        new System.Windows.Shell.JumpTask()
        {
          Title = Resources.SettingsLabel,
          Arguments = JumpListHandler.GetArgumentFromStartupArgument(StartupArgumentType.ShowSettings)
        }
      },
      {
        Okta.Authenticator.NativeApp.UI.Enums.JumpTask.ReportIssueJumpTask.ToString(),
        new System.Windows.Shell.JumpTask()
        {
          Title = Resources.SendFeedbackButtonText,
          Arguments = JumpListHandler.GetArgumentFromStartupArgument(StartupArgumentType.ShowReportIssue)
        }
      }
    };
    private readonly IApplicationHandler appHandler;

    public JumpListHandler(IApplicationHandler appHandler) => this.appHandler = appHandler;

    public void Initialize(bool sendFeedbackEnabled = false) => this.appHandler.InvokeOnUIThread((Action) (() =>
    {
      JumpList jumpList = this.appHandler.JumpList;
      jumpList.JumpItems.Clear();
      foreach (KeyValuePair<string, System.Windows.Shell.JumpTask> keyValuePair in JumpListHandler.JumpTaskDict)
      {
        if (!(keyValuePair.Key == Okta.Authenticator.NativeApp.UI.Enums.JumpTask.ReportIssueJumpTask.ToString()) || sendFeedbackEnabled)
          jumpList.JumpItems.Add((JumpItem) keyValuePair.Value);
      }
      this.appHandler.JumpList = jumpList;
    }));

    private static string GetArgumentFromStartupArgument(StartupArgumentType arg) => string.Format("--{0}", (object) arg);
  }
}
