// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Exceptions.OktaErrorReportException
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using Okta.Authenticator.NativeApp.Telemetry;
using Okta.Devices.SDK;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Okta.Authenticator.NativeApp.Exceptions
{
  public class OktaErrorReportException : OktaException
  {
    internal const int EFail = -2147467259;
    private const string StackTraceFieldName = "_stackTraceString";

    public OktaErrorReportException()
      : this(string.Empty, string.Empty)
    {
    }

    public OktaErrorReportException(string message, [CallerMemberName] string sourceMethodName = null)
      : this(message, -2147467259, sourceMethodName)
    {
    }

    public OktaErrorReportException(string message, int hresult, [CallerMemberName] string sourceMethodName = null)
      : this(message, hresult, (System.Diagnostics.StackTrace) null, sourceMethodName)
    {
    }

    public OktaErrorReportException(string message, System.Diagnostics.StackTrace trace, [CallerMemberName] string sourceMethodName = null)
      : this(message, -2147467259, trace, sourceMethodName)
    {
    }

    public OktaErrorReportException(
      string message,
      int hresult,
      System.Diagnostics.StackTrace trace,
      [CallerMemberName] string sourceMethodName = null)
      : base(message)
    {
      this.HResult = hresult;
      this.UpdateErrorData(trace, sourceMethodName);
    }

    public override string Message => string.Format("{0}, Scenario='{1}', HResult='0x{2:X8}'", (object) base.Message, (object) this.Scenario, (object) this.HResult);

    public ScenarioType Scenario { get; private set; }

    public string ErrorCategory { get; private set; }

    private static bool IsFromAsyncMethod(MethodBase currentMethod, string sourceMethodName) => currentMethod != (MethodBase) null && currentMethod.DeclaringType != (Type) null && currentMethod.DeclaringType.Name.StartsWith("<" + sourceMethodName + ">");

    private static bool IsSourceMethod(StackFrame frame, string sourceMethodName)
    {
      MethodBase method = frame.GetMethod();
      return method.Name.Equals(sourceMethodName, StringComparison.Ordinal) || OktaErrorReportException.IsFromAsyncMethod(method, sourceMethodName);
    }

    private static (StackFrame frame, string trace, string sourceMethod) GetTraceInfo(
      string sourceMethodName)
    {
      System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(2, true);
      StringBuilder stringBuilder = new StringBuilder(stackTrace.FrameCount);
      int index1 = 0;
      if (string.IsNullOrEmpty(sourceMethodName))
        return (stackTrace.GetFrame(0), stackTrace.ToString(), sourceMethodName);
      while (index1 < stackTrace.FrameCount && !OktaErrorReportException.IsSourceMethod(stackTrace.GetFrame(index1), sourceMethodName))
        ++index1;
      if (index1 >= stackTrace.FrameCount)
        return (stackTrace.GetFrame(0), stackTrace.ToString(), (string) null);
      for (int index2 = index1; index2 < stackTrace.FrameCount; ++index2)
      {
        StackFrame frame = stackTrace.GetFrame(index2);
        stringBuilder.AppendLine("\t" + OktaErrorReportException.GetMethodInfo(frame.GetMethod()) + " " + OktaErrorReportException.GetFrameFileInfo(frame));
      }
      return (stackTrace.GetFrame(index1), stringBuilder.ToString(), sourceMethodName);
    }

    private static ScenarioType GetMethodScenario(MethodBase sourceMethod, string methodName = null)
    {
      AnalyticsScenarioAttribute customAttribute1 = sourceMethod.GetCustomAttribute<AnalyticsScenarioAttribute>();
      if (customAttribute1 != null)
        return customAttribute1.Scenario;
      if (!string.IsNullOrEmpty(methodName))
      {
        for (Type declaringType = sourceMethod.DeclaringType; declaringType != (Type) null; declaringType = declaringType.DeclaringType)
        {
          MethodBase method = (MethodBase) declaringType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
          if (method != (MethodBase) null)
          {
            AnalyticsScenarioAttribute customAttribute2 = method.GetCustomAttribute<AnalyticsScenarioAttribute>();
            return customAttribute2 != null ? customAttribute2.Scenario : ScenarioType.Unknown;
          }
        }
      }
      return ScenarioType.Unknown;
    }

    private static string GetFrameFileInfo(StackFrame frame)
    {
      string fileName = frame?.GetFileName();
      return !string.IsNullOrEmpty(fileName) ? string.Format("{0}: {1}", (object) fileName, (object) frame.GetFileLineNumber()) : string.Empty;
    }

    private static string GetMethodInfo(MethodBase frameMethod)
    {
      if (frameMethod == (MethodBase) null)
        return string.Empty;
      string str = frameMethod.GetParameters().Length != 0 ? "..." : string.Empty;
      return frameMethod.DeclaringType.FullName + "." + frameMethod.Name + " (" + str + ")";
    }

    private void UpdateErrorData(System.Diagnostics.StackTrace trace, string sourceMethod = null)
    {
      if (trace == null || trace.FrameCount < 1)
      {
        (StackFrame frame, string trace, string sourceMethod) traceInfo = OktaErrorReportException.GetTraceInfo(sourceMethod);
        this.UpdateErrorData(traceInfo.frame, traceInfo.trace, traceInfo.sourceMethod);
      }
      else
      {
        StackFrame frame = trace.GetFrame(0);
        this.UpdateErrorData(frame, trace.ToString(), OktaErrorReportException.IsSourceMethod(frame, sourceMethod) ? sourceMethod : (string) null);
      }
    }

    private void UpdateErrorData(StackFrame errorFrame, string traceText, string sourceMethodName = null)
    {
      MethodBase method = errorFrame.GetMethod();
      this.ErrorCategory = method.DeclaringType.Namespace;
      this.Scenario = OktaErrorReportException.GetMethodScenario(method, sourceMethodName);
      FieldInfo field = typeof (Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic);
      if (!(field != (FieldInfo) null))
        return;
      field.SetValue((object) this, (object) traceText);
    }
  }
}
