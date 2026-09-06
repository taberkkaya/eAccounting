using eAccountingServer.Application.Features.Dashboard;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

/// <summary>Ana sayfanın özeti; tek çağrıda bütün tablo.</summary>
public class DashboardController : ApiController
{
    public DashboardController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Get(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
