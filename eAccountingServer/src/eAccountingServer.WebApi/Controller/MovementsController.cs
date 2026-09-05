using eAccountingServer.Application.Features.Movements;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

/// <summary>Kasa ve banka hareketlerine hesap ayrımı yapmadan bakmak için.</summary>
public class MovementsController : ApiController
{
    public MovementsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> GetRecent(GetRecentMovementsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
