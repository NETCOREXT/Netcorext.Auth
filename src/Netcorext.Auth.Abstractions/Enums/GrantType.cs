namespace Netcorext.Auth.Enums;

[Flags]
public enum GrantType
{
    None = 0,
    ClientCredentials = 1,
    Password = 2,
    RefreshToken = 4
}