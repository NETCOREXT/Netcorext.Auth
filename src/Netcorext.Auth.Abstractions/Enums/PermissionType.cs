namespace Netcorext.Auth.Enums;

[Flags]
public enum PermissionType
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write,
    Delete = 4,
    ReadDelete = Read | Delete,
    WriteDelete = Write | Delete,
    All = Read | Write | Delete
}