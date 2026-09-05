using eAccountingServer.Application.Features.BankDetails;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

public class BankDetailsController : ApiController
{
    public BankDetailsController(IMediator mediator) : base(mediator)
    {
    }


    [HttpPost]
    public async Task<IActionResult> GetAll(GetAllBankDetailsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBankDetailCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> Update(UpdateBankDetailCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteById(DeleteBankDetailByIdCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Seçili aralığın ekstresini Excel veya PDF olarak indirir. Hata durumunda
    /// diğer uçlarla aynı gövdeyi döner ki istemci mesajı gösterebilsin.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Export(ExportBankDetailsQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);

        if (!response.IsSuccessful || response.Data is null)
            return StatusCode(response.StatusCode, response);

        return File(response.Data.Content, response.Data.ContentType, response.Data.FileName);
    }
}
