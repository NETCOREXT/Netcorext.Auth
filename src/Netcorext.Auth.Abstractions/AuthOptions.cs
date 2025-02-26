using System.Security.Claims;
using Netcorext.Auth.Enums;

namespace Netcorext.Auth;

public class AuthOptions
{
    public string SigningKey { get; set; } = null!;
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; }
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;
    public bool IncludeErrorDetails { get; set; }
    public bool AllowedRefreshToken { get; set; }
    public string NameClaimType { get; set; } = ClaimTypes.NameIdentifier;
    public string RoleClaimType { get; set; } = ClaimTypes.Role;
    public int ClockSkewSeconds { get; set; } = 60 * 5;
    public int TokenExpireSeconds { get; set; } = 60 * 60;
    public int RefreshTokenExpireSeconds { get; set; } = 60 * 60 * 6;
    public int CodeExpireSeconds { get; set; } = 60 * 10;
    public int? LockoutAccessFailedCount { get; set; }
    public string OtpAuthScheme { get; set; } = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&algorithm=SHA1&digits=6&period=30";
    public GrantType AllowedGrantType { get; set; } = GrantType.ClientCredentials | GrantType.Password | GrantType.RefreshToken;
}