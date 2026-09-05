using eAccountingServer.Application.Features.CashRegisterDetails;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

public class CashRegisterDetailsController : ApiController
{
    public CashRegisterDetailsController(IMediator mediator) : base(mediator)
    {
    }


    [HttpPost]
    public async Task<IActionResult> GetAll(GetAllCashRegisterDetailsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCashRegisterDetailCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> Update(UpdateCashRegisterDetailCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteById(DeleteCashRegisterDetailByIdCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Seçili aralığın ekstresini Excel veya PDF olarak indirir. Hata durumunda
    /// diğer uçlarla aynı gövdeyi döner ki istemci mesajı gösterebilsin.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Export(ExportCashRegisterDetailsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);

        if (!response.IsSuccessful || response.Data is null)
            return StatusCode(response.StatusCode, response);

        return File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
    }
}
