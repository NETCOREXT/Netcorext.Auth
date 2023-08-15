using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.Authentication.Services.Permission.Queries;

public class GetRolePermission : IRequest<Result<IEnumerable<Models.RolePermission>>>
{
    public long[]? Ids { get; set; }
}