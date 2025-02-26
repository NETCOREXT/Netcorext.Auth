using Netcorext.Auth.Enums;

namespace Netcorext.Auth.Extensions;

public static class PermissionTypeExtension
{
    public static PermissionType ToPermissionType<TEnum>(this TEnum source, PermissionType defaultValue = PermissionType.None) where TEnum : struct, Enum
    {
        if (!Enum.TryParse(source.ToString(), true, out PermissionType pt))
        {
            pt = defaultValue;
        }

        return pt;
    }
    
    public static TEnum FromPermissionType<TEnum>(this PermissionType source, TEnum defaultValue = default) where TEnum : struct, Enum
    {
        if (!Enum.TryParse(source.ToString(), true, out TEnum pt))
        {
            pt = defaultValue;
        }

        return pt;
    }
}