namespace Netcorext.Auth;

public static class Constants
{
    public static class OAuth
    {
        public const string TOKEN_TYPE_BEARER = "Bearer";
        public const string TOKEN_TYPE_BEARER_NORMALIZED = "BEARER";
        public const string TOKEN_TYPE_BASIC = "Basic";
        public const string TOKEN_TYPE_BASIC_NORMALIZED = "BASIC";
        public const string ACCESS_DENIED = "access_denied";
        public const string ACCESS_DENIED_MESSAGE = "Access denied";
        public const string INVALID_CLIENT = "invalid_client";
        public const string INVALID_USER = "invalid_user";
        public const string INVALID_GRANT = "invalid_grant";
        public const string INVALID_REQUEST = "invalid_request";
        public const string INVALID_REQUEST_TOKEN = "Token invalid";
        public const string INVALID_REQUEST_USERNAME_OR_PASSWORD_MESSAGE = "Username or Password incorrect";
        public const string INVALID_REQUEST_ID_OR_SECRET_MESSAGE = "ClientId or ClientSecret incorrect";
        public const string INVALID_SCOPE = "invalid_scope";
        public const string INVALID_SCOPE_MESSAGE = "{0} Scope invalid";
        public const string UNAUTHORIZED_CLIENT = "unauthorized_client";
        public const string UNAUTHORIZED_CLIENT_MESSAGE = "Client authentication failed";
        public const string UNAUTHORIZED_USER = "unauthorized_user";
        public const string UNAUTHORIZED_USER_MESSAGE = "User authentication failed";
        public const string UNSUPPORTED_RESPONSE_TYPE = "unsupported_response_type";
        public const string INTERNAL_SERVER_ERROR = "internal_server_error";
        public const string UNSUPPORTED_GRANT_TYPE = "unsupported_grant_type";

        public const string GRANT_TYPE_AUTHORIZATION_CODE = "authorization_code";
        public const string GRANT_TYPE_CLIENT_CREDENTIALS = "client_credentials";
        public const string GRANT_TYPE_PASSWORD = "password";
        public const string GRANT_TYPE_REFRESH_TOKEN = "refresh_token";
    }
}