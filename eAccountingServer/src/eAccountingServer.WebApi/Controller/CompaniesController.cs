using eAccountingServer.Application.Features.Companies;
using eAccountingServer.Domain.Users;
using eAccountingServer.WebApi.Abstractions;
using MediatR;
using ResultKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

[Authorize(Policy = AuthorizationPolicies.Admin)]
public class CompaniesController : ApiController
{
    public CompaniesController(IMediator mediator) : base(mediator)
    {
    }


    [HttpPost]
    public async Task<IActionResult> GetAll(GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> Update(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteById(DeleteCompanyByIdCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> MigrateAll(MigrateAllCompaniesCommand request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Oturumdaki firmanın belgelerde görünen bilgileri. Kimlik jetondan
    /// okunuyor: istemcinin başka bir firmanın künyesini istemesine gerek yok,
    /// izni de yok.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        string? companyId = User.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(companyId, out Guid id))
            return StatusCode(400, Result<CompanyProfileDto>.Failure("Firma seçili değil."));

        var response = await _mediator.Send(new GetCompanyProfileQuery(id), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
