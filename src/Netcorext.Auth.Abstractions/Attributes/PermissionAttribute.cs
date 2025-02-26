using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PermissionAttribute : Attribute
{
    public PermissionAttribute(string functionId)
    {
        FunctionId = functionId;
    }

    public PermissionAttribute(string functionId, PermissionType nativePermission)
    {
        FunctionId = functionId;
        NativePermission = nativePermission;
    }

    public PermissionAttribute(string functionId, PermissionType nativePermission, string tag)
    {
        FunctionId = functionId;
        NativePermission = nativePermission;
        Tag = tag;
    }

    public string FunctionId { get; set; }
    public PermissionType NativePermission { get; set; } = PermissionType.None;
    public string? Tag { get; set; }
}