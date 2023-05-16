// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Authentication.OktaVerifyOidcClient
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

using IdentityModel.OidcClient;
using Okta.Authenticator.NativeApp.Injector;
using Okta.Devices.SDK;
using Okta.Devices.SDK.Extensions;
using Okta.Oidc.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

namespace Okta.Authenticator.NativeApp.Authentication
{
  public class OktaVerifyOidcClient : BaseOktaClient
  {
    private static List<string> scopes;

    public static List<int> OAuthLoopbackPorts => new List<int>()
    {
      65112,
      65122,
      65132,
      65142,
      65152
    };

    public OktaVerifyOidcClient(
      string signInUrl,
      bool useSystemBrowser,
      ILogger logger,
      UserAgentHandler userAgent,
      IApplicationHandler app)
      : base((IOktaClientConfiguration) OktaVerifyOidcClient.GetOidcClientConfiguration(signInUrl, app, useSystemBrowser), userAgent, MicrosoftExtensionLogger.CreateFromOktaLogger(logger))
    {
    }

    internal static string OidcClientId => "okta.63c081db-1f13-5084-882f-e79e1e5e2da7";

    internal static List<string> Scopes
    {
      get
      {
        if (OktaVerifyOidcClient.scopes == null)
          OktaVerifyOidcClient.scopes = OktaVerifyOidcClient.GetScopesForEnrollment().ToList<string>();
        return OktaVerifyOidcClient.scopes;
      }
    }

    public static UserAgentHandler GetUserAgentHandler()
    {
      string[] strArray = DevicesSdk.WebRequestProperties.UserAgentString.Split(new char[1]
      {
        ' '
      }, 2);
      return new UserAgentHandler("okta-auth-dotnet", typeof (BaseOktaClient).GetTypeInfo().Assembly.GetName().Version, strArray.Length == 1 ? (string) null : strArray[1], strArray[0]);
    }

    private static OktaClientConfiguration GetOidcClientConfiguration(
      string signInUrl,
      IApplicationHandler app,
      bool useSystemBrowser)
    {
      if (app == null)
        throw new ArgumentNullException(nameof (app));
      OktaClientConfiguration configuration = new OktaClientConfiguration()
      {
        OktaDomain = signInUrl,
        ClientId = OktaVerifyOidcClient.OidcClientId,
        Scope = (IList<string>) OktaVerifyOidcClient.Scopes
      };
      if (useSystemBrowser)
      {
        configuration.ResponseMode = OidcClientOptions.AuthorizeResponseMode.FormPost;
        configuration = app.SetDefaultSystemBrowser(configuration);
        configuration.RedirectUri = string.Format((IFormatProvider) CultureInfo.InvariantCulture, "http://localhost:{0}/login/callback/", (object) OktaVerifyOidcClient.GetAvailablePortForLogin());
      }
      else
      {
        configuration.ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect;
        app.SetDefaultIntegratedBrowser(configuration);
        configuration.RedirectUri = "com-okta-authenticator:/oauth/callback";
      }
      return configuration;
    }

    private static IEnumerable<string> GetScopesForEnrollment()
    {
      yield return "openid";
      yield return "profile";
      yield return "okta.authenticators.read";
      yield return "okta.authenticators.manage.self";
    }

    private static int GetAvailablePortForLogin()
    {
      IPEndPoint[] source = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
      if (source == null)
      {
        if (OktaVerifyOidcClient.CanCreateListener(OktaVerifyOidcClient.OAuthLoopbackPorts[0]))
          return OktaVerifyOidcClient.OAuthLoopbackPorts[0];
        source = Array.Empty<IPEndPoint>();
      }
      HashSet<int> intSet = new HashSet<int>(((IEnumerable<IPEndPoint>) source).Select<IPEndPoint, int>((Func<IPEndPoint, int>) (p => p.Port)));
      foreach (int oauthLoopbackPort in OktaVerifyOidcClient.OAuthLoopbackPorts)
      {
        if (!intSet.Contains(oauthLoopbackPort) && OktaVerifyOidcClient.CanCreateListener(oauthLoopbackPort))
          return oauthLoopbackPort;
      }
      throw new InvalidOperationException("Unable to find open port for OIDC login with system browser");
    }

    private static bool CanCreateListener(int port)
    {
      HttpListener httpListener = (HttpListener) null;
      try
      {
        httpListener = new HttpListener();
        httpListener.Prefixes.Add(string.Format((IFormatProvider) CultureInfo.InvariantCulture, "http://localhost:{0}/login/callback/", (object) port));
        httpListener.Start();
        return true;
      }
      catch (HttpListenerException ex)
      {
        DevicesSdk.Telemetry.WriteInfoEx("Failed to check port availability with " + ex.Message + ".", "C:\\jenkins\\workspace\\okta-windows-authenticator\\MYGIT\\src\\OktaVerifyApplication\\Authentication\\OktaVerifyOidcClient.cs", nameof (CanCreateListener));
        return false;
      }
      finally
      {
        httpListener?.Close();
      }
    }
  }
}
