using Grpc.Core;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Netcorext.Auth.Attributes;
using Netcorext.Auth.Gateway.Services.Route.Commands;
using Netcorext.Auth.Enums;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.Gateway.Services.Route;

[AllowAnonymous]
[Permission("AUTH", PermissionType.Write)]
public class RouteServiceFacade : RouteService.RouteServiceBase
{
    private readonly IDispatcher _dispatcher;

    public RouteServiceFacade(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<Result> RegisterRoute(RegisterRouteRequest request, ServerCallContext context)
    {
        var req = request.Adapt<RegisterRoute>();
        var rep = await _dispatcher.SendAsync(req);

        return rep.Adapt<Result>();
    }
}
