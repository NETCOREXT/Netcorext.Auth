using Netcorext.Contracts;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetFunctionId : IRequest<Result<IEnumerable<string>>> { }
