using Grpc.Core;
using Mapster;
using Netcorext.Auth.API.Services.Permission.Commands;
using Netcorext.Auth.API.Services.Permission.Queries;
using Netcorext.Auth.Protobufs;
using Netcorext.Contracts.Protobufs;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission;

public class PermissionServiceFacade : PermissionService.PermissionServiceBase
{
    private readonly IDispatcher _dispatcher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionServiceFacade(IDispatcher dispatcher, IHttpContextAccessor httpContextAccessor)
    {
        _dispatcher = dispatcher;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<CreatePermissionRequest.Types.Result> CreatePermission(CreatePermissionRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<CreatePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<CreatePermissionRequest.Types.Result>();
    }

    public override async Task<Result> DeletePermission(DeletePermissionRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<DeletePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }

    public override async Task<GetPermissionRequest.Types.Result> GetPermission(GetPermissionRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<GetPermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<GetPermissionRequest.Types.Result>();
    }

    public override async Task<Result> UpdatePermission(UpdatePermissionRequest request, ServerCallContext context)
    {
        _httpContextAccessor.HttpContext = context.GetHttpContext();

        var req = request.Adapt<UpdatePermission>();
        var rep = await _dispatcher.SendAsync(req);

        return rep!.Adapt<Result>();
    }
}