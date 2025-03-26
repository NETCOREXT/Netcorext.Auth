using Netcorext.Contracts;
using Netcorext.EntityFramework.UserIdentityPattern;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Services.Permission.Queries;

public class GetFunctionIdHandler : IRequestHandler<GetFunctionId, Result<IEnumerable<string>>>
{
    private readonly DatabaseContext _context;

    public GetFunctionIdHandler(DatabaseContextAdapter context)
    {
        _context = context.Slave;
    }

    public async Task<Result<IEnumerable<string>>> Handle(GetFunctionId request, CancellationToken cancellationToken = new CancellationToken())
    {
        var ds = _context.Set<Domain.Entities.Rule>();

        var ids = ds.Select(t => t.FunctionId)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToArray();

        return Result<IEnumerable<string>>.Success.Clone(ids);
    }
}
