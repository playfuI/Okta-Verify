// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.AuthenticationRequestManager
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.ApplicationInstance.StateMachine;
using Okta.Authenticator.NativeApp.Events.EventPayloads;
using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Authentication;
using Okta.Devices.SDK.Extensions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class AuthenticationRequestManager : IAuthenticationRequestManager
  {
    private readonly ILogger logger;
    private readonly IApplicationStateMachine stateMachine;
    private readonly IAnalyticsProvider analyticsProvider;
    private readonly ConcurrentDictionary<string, AuthenticationRequestContext> requests;
    private readonly ConcurrentQueue<(Task AuthenticationTask, TaskCompletionSource<bool> InteractionAwaiter, string RequestId)> interactiveRequestQueue;
    private int currentActivationCount;

    public AuthenticationRequestManager(
      ILogger logger,
      IApplicationStateMachine stateMachine,
      IAnalyticsProvider analyticsProvider)
    {
      this.logger = logger;
      this.stateMachine = stateMachine;
      this.analyticsProvider = analyticsProvider;
      this.requests = new ConcurrentDictionary<string, AuthenticationRequestContext>();
      this.interactiveRequestQueue = new ConcurrentQueue<(Task, TaskCompletionSource<bool>, string)>();
      this.currentActivationCount = 0;
    }

    public AuthenticationRequestContext this[string requestId]
    {
      get
      {
        AuthenticationRequestContext authenticationRequestContext;
        return !this.requests.TryGetValue(requestId, out authenticationRequestContext) ? (AuthenticationRequestContext) null : authenticationRequestContext;
      }
    }

    public AuthenticationRequestContext StartOrUpdateAuthentication(BindingEventPayload payload) => string.IsNullOrEmpty(payload.CorrelationId) ? (AuthenticationRequestContext) null : this.requests.AddOrUpdate(payload.CorrelationId, (Func<string, AuthenticationRequestContext>) (k => AuthenticationRequestManager.UpdateContext(payload, false)), (Func<string, AuthenticationRequestContext, AuthenticationRequestContext>) ((k, c) => AuthenticationRequestManager.UpdateContext(payload, false, c)));

    public async Task<AuthenticationRequestContext> StartOrUpdateAuthenticationWithUserInteraction(
      BindingEventPayload payload)
    {
      if (string.IsNullOrEmpty(payload.CorrelationId))
        return (AuthenticationRequestContext) null;
      return await this.HandleUserInteraction(this.requests.AddOrUpdate(payload.CorrelationId, (Func<string, AuthenticationRequestContext>) (k => AuthenticationRequestManager.UpdateContext(payload, true)), (Func<string, AuthenticationRequestContext, AuthenticationRequestContext>) ((k, c) => AuthenticationRequestManager.UpdateContext(payload, true, c)))).ConfigureAwait(false);
    }

    public AuthenticationRequestContext StartOrUpdateAuthentication(CredentialEventPayload payload) => string.IsNullOrEmpty(payload?.CorrelationId) ? (AuthenticationRequestContext) null : this.requests.AddOrUpdate(payload.CorrelationId, (Func<string, AuthenticationRequestContext>) (k => AuthenticationRequestManager.UpdateContext(payload)), (Func<string, AuthenticationRequestContext, AuthenticationRequestContext>) ((s, c) => AuthenticationRequestManager.UpdateContext(payload, context: c)));

    public async Task<AuthenticationRequestContext> StartOrUpdateAuthenticationWithUserInteraction(
      CredentialEventPayload payload)
    {
      if (string.IsNullOrEmpty(payload?.CorrelationId))
        return (AuthenticationRequestContext) null;
      return await this.HandleUserInteraction(this.requests.AddOrUpdate(payload.CorrelationId, (Func<string, AuthenticationRequestContext>) (k => AuthenticationRequestManager.UpdateContext(payload, true)), (Func<string, AuthenticationRequestContext, AuthenticationRequestContext>) ((k, c) => AuthenticationRequestManager.UpdateContext(payload, true, context: c)))).ConfigureAwait(false);
    }

    public AuthenticationRequestContext TryUpdateAuthentication(CredentialEventPayload payload)
    {
      if (string.IsNullOrEmpty(payload?.CorrelationId))
        return (AuthenticationRequestContext) null;
      AuthenticationRequestStatus status = payload.IsFinal ? (payload.ErrorCode == PlatformErrorCode.None ? AuthenticationRequestStatus.Succeeded : AuthenticationRequestStatus.Failed) : AuthenticationRequestStatus.Unknown;
      return this.requests.AddOrUpdate(payload.CorrelationId, (Func<string, AuthenticationRequestContext>) (k => AuthenticationRequestManager.UpdateContext(payload, status: status, errorCode: payload.ErrorCode)), (Func<string, AuthenticationRequestContext, AuthenticationRequestContext>) ((k, c) => AuthenticationRequestManager.UpdateContext(payload, status: status, errorCode: payload.ErrorCode, context: c)));
    }

    public AuthenticationRequestContext EndAuthentication(
      BindingEventPayload payload,
      bool isSuccess)
    {
      if (string.IsNullOrEmpty(payload.CorrelationId))
        return (AuthenticationRequestContext) null;
      AuthenticationRequestContext context;
      return !this.requests.TryRemove(payload.CorrelationId, out context) ? AuthenticationRequestManager.UpdateFinalContext(payload, isSuccess) : AuthenticationRequestManager.UpdateFinalContext(payload, isSuccess, context);
    }

    private static AuthenticationRequestContext UpdateContext(
      BindingEventPayload payload,
      bool isUserInteraction,
      AuthenticationRequestContext context = null)
    {
      context = AuthenticationRequestManager.CreateOrUpdateContext(payload.CorrelationId, isUserInteraction, payload.OrgUrl, context);
      context.OrgId = AuthenticationRequestManager.EnsureSet(context.OrgId, payload.OrgId);
      context.UserId = AuthenticationRequestManager.EnsureSet(context.UserId, payload.UserId);
      context.AppName = AuthenticationRequestManager.EnsureSet(context.AppName, payload.AppName);
      context.EnrollmentId = AuthenticationRequestManager.EnsureSet(context.EnrollmentId, payload.EnrollmentId);
      context.IsBindingRequest = true;
      context.RequiresFocus = isUserInteraction;
      return context;
    }

    private static AuthenticationRequestContext UpdateFinalContext(
      BindingEventPayload payload,
      bool isSuccess,
      AuthenticationRequestContext context = null)
    {
      context = AuthenticationRequestManager.UpdateContext(payload, false, context);
      context.SetResult(isSuccess, payload.FailureReason);
      return context;
    }

    private static AuthenticationRequestContext UpdateContext(
      CredentialEventPayload payload,
      bool isUserInteraction = false,
      AuthenticationRequestStatus status = AuthenticationRequestStatus.Started,
      PlatformErrorCode errorCode = PlatformErrorCode.None,
      AuthenticationRequestContext context = null)
    {
      context = AuthenticationRequestManager.CreateOrUpdateContext(payload.CorrelationId, isUserInteraction, payload.RequestDomain, context);
      context.RequiresFocus = true;
      context.KeyInteractions[payload.CredentialType] = (status, payload.CredentialId, errorCode);
      return context;
    }

    private static AuthenticationRequestContext CreateOrUpdateContext(
      string correlationId,
      bool isUserInteraction,
      string orgUrl,
      AuthenticationRequestContext context = null)
    {
      if (context == null)
        context = new AuthenticationRequestContext(correlationId);
      context.HasUserInteraction |= isUserInteraction;
      context.OrgUrl = AuthenticationRequestManager.EnsureSet(context.OrgUrl, orgUrl);
      return context;
    }

    private static string EnsureSet(string oldValue, string newValue) => !string.IsNullOrEmpty(newValue) && string.IsNullOrEmpty(oldValue) ? newValue : oldValue;

    private async Task<AuthenticationRequestContext> HandleUserInteraction(
      AuthenticationRequestContext context)
    {
      AuthenticationRequestManager authenticationRequestManager = this;
      TaskCompletionSource<bool> source;
      bool flag1 = context.RequestInteractionQueuing(out source);
      bool flag2 = false;
      if (flag1)
      {
        flag2 = Interlocked.Increment(ref authenticationRequestManager.currentActivationCount) == 1;
        authenticationRequestManager.interactiveRequestQueue.Enqueue((context.CompletionTask, source, context.Id));
      }
      authenticationRequestManager.logger.WriteDebugEx(string.Format("Handling user interaction for {0}: {1}|{2}|{3}", (object) context.Id, (object) authenticationRequestManager.currentActivationCount, (object) flag2, (object) flag1), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\AuthenticationRequestManager.cs", nameof (HandleUserInteraction));
      if (!flag2)
      {
        authenticationRequestManager.logger.WriteDebugEx("Not activating authentication since there is already an active one.", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\AuthenticationRequestManager.cs", nameof (HandleUserInteraction));
      }
      else
      {
        // ISSUE: reference to a compiler-generated method
        if (!authenticationRequestManager.stateMachine.TransitionToWithDeferral(AppStateRequestType.TemporaryActivate, new ApplicationStateDeferral(authenticationRequestManager.\u003CHandleUserInteraction\u003Eb__20_0), "processing interactive authentications"))
          authenticationRequestManager.ProcessAuthenticationsInSerial().AsBackgroundTask("processing interactive authentications without deferral");
      }
      await context.PrioritizationTask.ConfigureAwait(false);
      // ISSUE: explicit non-virtual call
      return __nonvirtual (authenticationRequestManager[context.Id]);
    }

    private async Task ProcessAuthenticationsInSerial()
    {
      (Task AuthenticationTask, TaskCompletionSource<bool> InteractionAwaiter, string RequestId) result;
      while (this.interactiveRequestQueue.TryDequeue(out result))
      {
        try
        {
          result.InteractionAwaiter.SetResult(true);
          this.logger.WriteDebugEx(string.Format("Processing authentication {0}. Requires focus: {1}", (object) result.RequestId, (object) this[result.RequestId]?.RequiresFocus), "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\AuthenticationRequestManager.cs", nameof (ProcessAuthenticationsInSerial));
          IApplicationStateMachine stateMachine = this.stateMachine;
          ApplicationStateContext context = new ApplicationStateContext();
          AuthenticationRequestContext authenticationRequestContext = this[result.RequestId];
          context.ForceActivation = authenticationRequestContext != null && authenticationRequestContext.RequiresFocus;
          stateMachine.TransitionTo(AppStateRequestType.BringToFocus, context);
          await result.AuthenticationTask.ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCritical())
        {
          this.logger.LogAndReportToAnalytics("Failed to process authentications in serial: " + ex.Message, ex, this.analyticsProvider);
        }
        finally
        {
          Interlocked.Decrement(ref this.currentActivationCount);
        }
      }
    }
  }
}
