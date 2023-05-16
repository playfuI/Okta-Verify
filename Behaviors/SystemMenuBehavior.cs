// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Behaviors.SystemMenuBehavior
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Microsoft.Xaml.Behaviors;
using Okta.Authenticator.NativeApp.Events;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Injector;
using Prism.Events;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Okta.Authenticator.NativeApp.Behaviors
{
  public class SystemMenuBehavior : Behavior<Window>, IDisposable
  {
    private static readonly EventInfo ResourcesChangedEvent = typeof (FrameworkElement).GetEvent("ResourcesChanged", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly IEventAggregator eventAggregator;
    private object[] resourcesHandler;
    private NotifyIcon notifyIcon;
    private bool disposedValue;

    public SystemMenuBehavior()
    {
      this.eventAggregator = AppInjector.Get<IEventAggregator>();
      this.eventAggregator.GetEvent<AppStateEvent>().Subscribe(new Action<AppState>(this.OnAppStateChanged));
    }

    public System.Windows.Controls.ContextMenu ContextMenu { get; set; }

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
      {
        if (this.AssociatedObject != null && this.resourcesHandler != null)
          SystemMenuBehavior.ResourcesChangedEvent.RemoveMethod.Invoke((object) this.AssociatedObject, this.resourcesHandler);
        if (this.notifyIcon != null)
        {
          this.notifyIcon.MouseDown -= new MouseEventHandler(this.OnNotifyIconMouseDown);
          this.notifyIcon.Visible = false;
          this.notifyIcon.Dispose();
        }
      }
      this.disposedValue = true;
    }

    private void Initialize()
    {
      this.notifyIcon = new NotifyIcon();
      this.notifyIcon.ContextMenuStrip = new ContextMenuStrip();
      this.resourcesHandler = new object[1]
      {
        (object) new EventHandler(this.OnResourcesUpdated)
      };
      SystemMenuBehavior.ResourcesChangedEvent.AddMethod.Invoke((object) this.AssociatedObject, this.resourcesHandler);
      this.notifyIcon.MouseDown += new MouseEventHandler(this.OnNotifyIconMouseDown);
      this.notifyIcon.Text = this.AssociatedObject.Title;
      this.notifyIcon.Visible = true;
      this.EnsureIconUpdated();
      if (this.ContextMenu == null)
        return;
      this.ContextMenu.PlacementTarget = (UIElement) this.AssociatedObject;
      this.ContextMenu.Placement = PlacementMode.MousePoint;
      this.ContextMenu.HorizontalOffset = this.ContextMenu.MinWidth;
      this.ContextMenu.DataContext = this.AssociatedObject.DataContext;
    }

    private void EnsureIconUpdated()
    {
      if (this.notifyIcon == null || !(this.AssociatedObject.Icon is BitmapImage icon))
        return;
      using (Stream stream = System.Windows.Application.GetResourceStream(icon.UriSource).Stream)
        this.notifyIcon.Icon = new Icon(stream);
    }

    private void EnsureMainMenuAction()
    {
      if (this.ContextMenu == null || !this.ContextMenu.HasItems)
        this.AssociatedObject.Activate();
      if (!(this.ContextMenu.Items[0] is System.Windows.Controls.MenuItem menuItem))
        return;
      menuItem.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
      menuItem.Command?.Execute(menuItem.CommandParameter);
    }

    private void OnResourcesUpdated(object sender, EventArgs e) => this.EnsureIconUpdated();

    private void OnNotifyIconMouseDown(object sender, MouseEventArgs e)
    {
      if (this.ContextMenu == null)
        this.EnsureMainMenuAction();
      else if (this.ContextMenu.IsOpen)
        this.ContextMenu.IsOpen = false;
      else if (e.Button == MouseButtons.Right)
        this.ContextMenu.IsOpen = true;
      else
        this.EnsureMainMenuAction();
    }

    private void OnAppStateChanged(AppState newState)
    {
      switch (newState.StateType)
      {
        case ComputingStateType.Loading:
          this.Initialize();
          break;
        case ComputingStateType.ShuttingDown:
          this.Dispose();
          break;
      }
    }
  }
}
