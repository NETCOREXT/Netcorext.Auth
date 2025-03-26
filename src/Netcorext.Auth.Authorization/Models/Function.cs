using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Authorization.Models;

public class Function
{
    public string Id { get; set; } = null!;
    public PermissionType PermissionType { get; set; }
}
