// Decompiled with JetBrains decompiler
// Type: Okta.Authenticator.NativeApp.Constants
// Assembly: OktaVerify, Version=4.0.2.0, Culture=neutral, PublicKeyToken=null
// MVID: DC50DA4D-F710-472F-8C8B-1589B8CC931B
// Assembly location: C:\Program Files\Okta\Okta Verify\OktaVerify.exe

namespace Okta.Authenticator.NativeApp
{
  public static class Constants
  {
    public const int MAX_IN_MEMORY_LOG_LINES = 200;
    public const string AUTHENTICATOR_CLIENT_GUID = "63c081db-1f13-5084-882f-e79e1e5e2da7";
    public const string AUTHENTICATOR_PUBLIC_CLIENT_ID = "okta.63c081db-1f13-5084-882f-e79e1e5e2da7";
    public const string AUTHENTICATOR_CUSTOM_URI_SCHEME = "com-okta-authenticator:/";
    public const string AUTHENTICATOR_CUSTOM_URI_SCHEME_OAUTH = "com-okta-authenticator:/oauth/callback";
    public const string AUTHENTICATOR_OAUTH_LOOPBACK_TEMPLATE = "http://localhost:{0}/login/callback/";
    public const string REGISTRY_ROOT_OKTA = "Software\\Okta";
    public const string REGISTRY_AUTHENTICATOR_KEY = "Okta Verify";
    public const string REGISTRY_OKTA_DEVICE_ACCESS = "Okta Device Access";
    public const string REGISTRY_INTEGRATIONS_KEY = "Integrations";
    public const string REGISTRY_VALUE_SIGNIN_URL = "SignInUrl";
    public const string REGISTRY_USE_INTEGRATED_BROWSER = "OIDCUseIntegratedBrowser";
    public const string REGISTRY_LOG_LEVEL = "LogLevel";
    public const string REGISTRY_KEY_CREATION_FLAGS = "KeyCreationFlags";
    public const string REGISTRY_KEY_DISABLE_SANDBOX = "DisableSandbox";
    public const string REGISTRY_KEY_DISABLE_PLUGINS = "DisablePlugins";
    public const string REGISTRY_KEY_FORCE_DEBUGGER = "ForceDebugger";
    public const string REGISTRY_KEY_AUTOUPDATE_POLLING_FREQUENCY = "AutoUpdatePollingInSecond";
    public const string REGISTRY_KEY_AUTOUPDATE_VERSION_OVERRIDE = "AutoUpdateClientVersionOverride";
    public const string REGISTRY_KEY_AUTOUPDATE_DEFERRED_ROLLOUT = "AutoUpdateDeferredByDays";
    public const string REGISTRY_KEY_AUTOUPDATE_BUCKET_ID_OVERRIDE = "AutoUpdateBucketIdOverride";
    public const string REGISTRY_KEY_REPORT_TO_APPCENTER = "ReportToAppCenter";
    public const string REGISTRY_KEY_JIT_CONFIGURATION = "JustInTimeEnrollmentConfiguration";
    public const string REGISTRY_KEY_REQUIRE_FRESH_SIGNAL = "RequireFreshSignal";
    public const string REGISTRY_KEY_SIGNALS_COLLECTION_TIMEOUT = "CollectionTimeout";
    public const string REGISTRY_KEY_PLUGIN_RE_INIT_INTERVAL = "ReInitializationInterval";
    public const string REGISTRY_KEY_SIGNAL_CERT_CACHING_DISABLED = "DisableCertificateCaching";
    public const string REGISTRY_KEY_ENROLL_IN_BETA_PROGRAM = "EnrollInBetaProgram";
    public const string REGISTRY_KEY_OVERRIDE_SANDBOX_LOCATION = "SandboxLocationOverride";
    public const string REGISTRY_OKTA_DEVICE_ACCESS_CLIENT_ID = "ClientId";
    public const string REGISTRY_OKTA_DEVICE_ACCESS_CLIENT_SECRET = "ClientSecret";
    public const string REGISTRY_OKTA_DEVICE_ACCESS_ORG_URL = "OrgUrl";
    public const string REGISTRY_KEY_DISABLE_SSL_PINNING = "DisableSslPinning";
    public const string REGISTRY_KEY_LOOPBACK_TLS_MODE = "LoopbackBindingMode";
    public const string REGISTRY_KEY_CALLER_BINARY_VALIDATION_MODE = "CallerBinaryValidationMode";
    public const string REGISTRY_KEY_FEATURE_FLAGS = "FeatureFlags";
    public const string REGISTRY_KEY_DEVICE_HEALTH_OPTIONS = "DeviceHealthOptions";
    public const string APP_PATH = "Okta\\OktaVerify";
    public const string LOG_FOLDER = "Logs";
    public const string SANDBOX_FOLDER = "Sandbox";
    public const string PLUGINS_FOLDER = "Plugins";
    public const string APP_LOG_FILE = "log.txt";
    public const string APP_DB_FILE_NO_EXTENSION = "OVStore";
    public const string APP_TITLE = "Okta Verify";
    public const string APP_DEBUG_SUFFIX = "_DBG";
    public const string LOG_SOURCE = "Okta Verify";
    public const string LOG_NAME = "Okta";
    public const string AUTO_UPDATE_LOG_SOURCE_NAME = "OktaUpdate";
    public const string SANDBOX_PREFIX = "OVSvc";
    public const int ERROR_DURATION = 4000;
    public const string AUTH_SUCCESS_PATH = "/oktaverify-auth/success";
  }
}
