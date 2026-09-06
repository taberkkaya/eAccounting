using eAccountingServer.Application.Features.Search;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

/// <summary>Cari, ürün, fatura ve hesaplarda tek arama.</summary>
public class SearchController : ApiController
{
    public SearchController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Global(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
