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
    public async Task<IActionResult> GetAll(GetMovementsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> GetRecent(GetRecentMovementsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Seçili filtrelerin sonucunu Excel veya PDF olarak indirir. Hata durumunda
    /// diğer uçlarla aynı gövdeyi döner ki istemci mesajı gösterebilsin.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Export(ExportMovementsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);

        if (!response.IsSuccessful || response.Data is null)
            return StatusCode(response.StatusCode, response);

        return File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
    }
}
