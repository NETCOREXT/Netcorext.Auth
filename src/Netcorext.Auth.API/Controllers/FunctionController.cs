using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netcorext.Auth.API.Services.Permission.Queries;
using Netcorext.Auth.Attributes;
using Netcorext.Contracts;
using Netcorext.Extensions.Contracts.AspNetCore;
using Netcorext.Mediator;

namespace Netcorext.Auth.API.Controllers;

[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
[Permission("AUTH-COMMON")]
public class FunctionController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public FunctionController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(Result<IEnumerable<string>>), 200)]
    [ProducesResponseType(typeof(Result<IEnumerable<string>>), 400)]
    [ProducesResponseType(typeof(Result<IEnumerable<string>>), 401)]
    [ProducesResponseType(typeof(Result<IEnumerable<string>>), 403)]
    public async Task<IActionResult> GetAsync([FromQuery] GetFunctionId request, CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.SendAsync(request, cancellationToken);

        return result.ToActionResult();
    }
}
