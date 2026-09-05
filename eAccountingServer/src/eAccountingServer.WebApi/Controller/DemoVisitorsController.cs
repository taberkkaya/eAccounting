using eAccountingServer.Application.Features.DemoVisitors;
using eAccountingServer.Domain.Users;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

[Authorize(Policy = AuthorizationPolicies.Admin)]
public class DemoVisitorsController : ApiController
{
    public DemoVisitorsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> GetAll(GetAllDemoVisitorsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
