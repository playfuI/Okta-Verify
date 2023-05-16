// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Behaviors.WindowBehavior
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Xaml.Behaviors;
using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Bindings;
using Okta.Authenticator.NativeApp.Controls;
using Okta.Authenticator.NativeApp.Enrollment;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Authenticator.NativeApp.ViewModels;
using Okta.Authenticator.NativeApp.Views;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Prism.Events;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Okta.Authenticator.NativeApp.Behaviors
{
  public class WindowBehavior : Behavior<Window>
  {
    private readonly ILogger logger;
    private readonly IEventAggregator eventAggregator;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly IApplicationStateMachine stateMachine;
    private DialogViewStateEvent dialogViewStateEvent;
    private WindowAdorner currentAdorner;
    private Task<bool> currentDialogTask;

    public WindowBehavior()
    {
      this.logger = AppInjector.Get<ILogger>();
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.analyticsProvider = AppInjector.Get<IAnalyticsProvider>();
      this.stateMachine = AppInjector.Get<IApplicationStateMachine>();
      this.RegisterToEvents();
    }

    protected override void OnAttached()
    {
      base.OnAttached();
      this.AssociatedObject.LocationChanged += new EventHandler(this.OnLocationUpdated);
    }

    protected override void OnDetaching()
    {
      this.AssociatedObject.LocationChanged -= new EventHandler(this.OnLocationUpdated);
      base.OnDetaching();
    }

    private void OnLocationUpdated(object sender, EventArgs e) => this.EnsureDialogClosed(true);

    private void OnDialogViewStateChanged(IDialogViewState updatedState)
    {
      if (updatedState == null)
        return;
      this.Dispatcher.InvokeAsync((Action) (() =>
      {
        if (updatedState.ShowDialog)
          this.ShowDialog(updatedState.ViewType, updatedState.Payload).ConfigureAwait(true);
        else
          this.EnsureDialogClosed(updatedState.ViewType);
      }));
    }

    private void RegisterToEvents()
    {
      this.dialogViewStateEvent = this.eventAggregator.GetEvent<DialogViewStateEvent>();
      this.dialogViewStateEvent.Subscribe(new Action<IDialogViewState>(this.OnDialogViewStateChanged));
    }

    private async Task ShowDialog(DialogViewType viewType, object payload = null)
    {
      try
      {
        DialogViewType viewType1;
        switch (viewType)
        {
          case DialogViewType.SignInConfirmation:
            using (SignInConfirmationViewModel confirmVM = new SignInConfirmationViewModel((BindingInformationModel) payload))
            {
              bool result = await this.ShowInlineDialog((UserControl) new SignInConfirmationView(), (BaseConfirmDialogViewModel) confirmVM).ConfigureAwait(true);
              this.PublishDialogResult(viewType, result);
            }
            break;
          case DialogViewType.DisableVerificationConfirmation:
            DisableVerificationDialogViewModel context1 = new DisableVerificationDialogViewModel((IOktaAccount) payload);
            this.ShowDialog((Window) new ConfirmDialogView(), (BaseConfirmDialogViewModel) context1);
            viewType1 = viewType;
            bool result1 = await context1.ConfirmTask.ConfigureAwait(true);
            this.PublishDialogResult(viewType1, result1);
            break;
          case DialogViewType.RemoveAccountConfirmation:
            RemoveAccountDialogViewModel context2 = new RemoveAccountDialogViewModel((IOktaAccount) payload);
            this.ShowDialog((Window) new ConfirmDialogView(), (BaseConfirmDialogViewModel) context2);
            viewType1 = viewType;
            bool result2 = await context2.ConfirmTask.ConfigureAwait(true);
            this.PublishDialogResult(viewType1, result2);
            break;
          case DialogViewType.InProgress:
            this.ShowAdorner(viewType, (UserControl) new InProgressDialogView(), (INotifyPropertyChanged) new InProgressDialogViewModel((string) payload));
            break;
          case DialogViewType.AccountVerified:
            bool result3 = await this.ShowInlineDialog((UserControl) new AccountVerifiedView(), (BaseConfirmDialogViewModel) new AccountVerifiedViewModel((BindingInformationModel) payload)).ConfigureAwait(true);
            this.PublishDialogResult(viewType, result3);
            break;
        }
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        this.logger.WriteException(string.Format("Failed to display the dialog window associated with {0}", (object) viewType), ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    private void ShowDialog(Window dialogView, BaseConfirmDialogViewModel context = null)
    {
      this.EnsureDialogClosed(true);
      if (context != null)
      {
        dialogView.DataContext = (object) context;
        this.currentDialogTask = context.ConfirmTask;
      }
      dialogView.Owner = this.AssociatedObject;
      dialogView.ShowDialog();
      this.currentDialogTask = (Task<bool>) null;
    }

    private async Task<bool> ShowInlineDialog(
      UserControl dialogView,
      BaseConfirmDialogViewModel dialogViewModel)
    {
      this.stateMachine.TransitionTo(AppStateRequestType.BringToFocus);
      if (!this.ShowAdorner(dialogViewModel.DialogType, dialogView, (INotifyPropertyChanged) dialogViewModel))
        return false;
      this.currentDialogTask = dialogViewModel.ConfirmTask;
      int num = await this.currentDialogTask.ConfigureAwait(true) ? 1 : 0;
      this.EnsureAdornerRemoved();
      this.currentDialogTask = (Task<bool>) null;
      return num != 0;
    }

    private bool ShowAdorner(
      DialogViewType viewType,
      UserControl contentView,
      INotifyPropertyChanged context = null)
    {
      if (this.currentDialogTask != null && !this.currentDialogTask.IsCompleted)
        return false;
      this.EnsureAdornerRemoved();
      if (context != null)
        contentView.DataContext = (object) context;
      this.currentAdorner = WindowAdorner.ShowContent(contentView, this.AssociatedObject);
      int num = this.currentAdorner != null ? 1 : 0;
      if (num != 0)
        return num != 0;
      this.logger.WriteWarningEx(string.Format("Failed to display the inline dialog. View: {0} - State: {1} - Loaded: {2}", (object) viewType, (object) this.AssociatedObject.WindowState, (object) this.AssociatedObject.IsLoaded), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Behaviors\\WindowBehavior.cs", nameof (ShowAdorner));
      this.analyticsProvider.TrackErrorWithLogs(string.Format("{0} display failed", (object) viewType), new StackTrace(true), sourceMethodName: nameof (ShowAdorner));
      return num != 0;
    }

    private void EnsureAdornerRemoved()
    {
      if (this.currentAdorner == null)
        return;
      try
      {
        this.logger.WriteInfoEx("Removing current adorner...", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Behaviors\\WindowBehavior.cs", nameof (EnsureAdornerRemoved));
        this.currentAdorner.RemoveContent();
      }
      catch (Exception ex) when (!ex.IsCritical())
      {
        this.logger.WriteException("An error occurred while removing the window adorner", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
      finally
      {
        this.currentAdorner = (WindowAdorner) null;
      }
    }

    private void EnsureDialogClosed(bool forceClose = false)
    {
      try
      {
        Window window = this.AssociatedObject.OwnedWindows.OfType<Window>().FirstOrDefault<Window>((Func<Window, bool>) (w => w is ConfirmDialogView));
        if (window == null)
          return;
        if (forceClose)
          window.Close();
        else if (window is ConfirmDialogView)
          window.Close();
        else
          this.logger.WriteWarningEx("Ignoring the request to close a confirmation dialog that's not open", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\UI\\Behaviors\\WindowBehavior.cs", nameof (EnsureDialogClosed));
      }
      catch (Exception ex) when (!ex.IsCritical(this.logger))
      {
        this.logger.WriteException("An error occurred while closing the Sign In dialog", ex);
        this.analyticsProvider.TrackErrorWithLogsAndAppData(ex);
      }
    }

    private void EnsureDialogClosed(DialogViewType viewType)
    {
      switch (viewType)
      {
        case DialogViewType.SignInConfirmation:
        case DialogViewType.InProgress:
        case DialogViewType.AccountVerified:
          this.EnsureAdornerRemoved();
          break;
        case DialogViewType.DisableVerificationConfirmation:
        case DialogViewType.RemoveAccountConfirmation:
          this.EnsureDialogClosed();
          break;
      }
    }

    private void PublishDialogResult(DialogViewType viewType, bool result) => this.eventAggregator.GetEvent<DialogViewStateResultEvent>().Publish(new DialogViewStateResult(viewType, result));
  }
}
