namespace Netcorext.Auth.Models;

public class Traffic
{
    public DateTimeOffset TrafficDate { get; set; }
    public string Protocol { get; set; } = null!;
    public string Scheme { get; set; } = null!;
    public string Method { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Host { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string? QueryString { get; set; }
    public string? Headers { get; set; }
    public string? ResponseHeaders { get; set; }
    public string StatusCode { get; set; } = null!;
    public TimeSpan Elapsed { get; set; }
    public string? DeviceId { get; set; }
    public string? Ip { get; set; }
    public string? TraceIdentifier { get; set; }
    public string? XRequestId { get; set; }
    public UserAgent? UserAgent { get; set; }
    public User? User { get; set; }
}

public class User
{
    public string? Aud { get; set; }
    public string? Exp { get; set; }
    public string? Iat { get; set; }
    public string? Iss { get; set; }
    public string? Jti { get; set; }
    public string? Nbf { get; set; }
    public string? Sub { get; set; }
    public string? Label{ get; set; }
    public string? Name { get; set; }
    public string? NameId { get; set; }
    public string? Nickname { get; set; }
    public string? Role { get; set; }
    public string? Uid { get; set; }
    public string? Rt { get; set; }
    public string? Tt { get; set; }
    public string? UserData { get; set; }

}

public class UserAgent
{
    public string? Device { get; set; }
    public string? DeviceType { get; set; }
}
