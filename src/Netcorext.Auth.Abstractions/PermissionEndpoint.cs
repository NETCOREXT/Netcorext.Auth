using Netcorext.Auth.Enums;

namespace Netcorext.Auth;

public class PermissionEndpoint
{
    public string Group { get; set; } = null!;
    public string Protocol { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public string RelativePath { get; set; } = null!;
    public string Template { get; set; } = null!;
    public virtual IDictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
    public string FunctionId { get; set; } = null!;
    public PermissionType NativePermission { get; set; }
    public bool AllowAnonymous { get; set; }
    public string? Tag { get; set; }
}