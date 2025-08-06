namespace Netcorext.Auth.API.Services.User.Queries.Models;

public class UserFunctionMixRole
{
    public IEnumerable<long> Roles { get; set; }
    public IEnumerable<UserFunction> UserFunctions { get; set; } = null!;
}
