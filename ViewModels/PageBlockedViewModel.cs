// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.ViewModels.PageBlockedViewModel
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.UIEvents;
using Okta.OktaVerify.Windows.Core.Properties;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Okta.Authenticator.NativeApp.ViewModels
{
  public class PageBlockedViewModel : BaseViewModel
  {
    internal static int PageBlockedTimeout = 900000;

    public PageBlockedViewModel()
    {
      this.PageBlockedText1 = Resources.OriginMismatchText1;
      this.PageBlockedText2 = Resources.OriginMismatchText2;
      this.ButtonClose = Resources.ButtonClose;
      this.ButtonCloseCommand = (ICommand) new DelegateCommand(new Action(this.BackToAccountListView));
      this.InitializeTimedOutTask();
      this.FireViewModelChangedEvent();
    }

    public string PageBlockedText1 { get; protected set; }

    public string PageBlockedText2 { get; protected set; }

    public string ButtonClose { get; protected set; }

    public ICommand ButtonCloseCommand { get; }

    private void InitializeTimedOutTask() => Task.Delay(PageBlockedViewModel.PageBlockedTimeout).ContinueWith(new Action<Task>(this.OnPageBlockedTimedOut));

    private void OnPageBlockedTimedOut(Task task) => this.BackToAccountListView();

    private void BackToAccountListView() => this.EventAggregator.GetEvent<MainViewStateEvent>().Publish(new MainViewState(MainViewType.OriginMismatch, (INotifyPropertyChanged) this, false));
  }
}
