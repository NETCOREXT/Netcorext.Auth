using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries.Models;

public class RolePermissionRule
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public long Id { get; set; }
    public string FunctionId { get; set; } = null!;
    public int Priority { get; set; }
    public PermissionType PermissionType { get; set; }
    public bool Allowed { get; set; }
}