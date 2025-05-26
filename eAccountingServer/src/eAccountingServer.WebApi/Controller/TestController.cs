using eAccountingServer.WebApi.Abstractions;
using FluentEmail.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAccountingServer.WebApi.Controller;

[AllowAnonymous]
public class TestController : ApiController
{

    private readonly IFluentEmail _fluentEmail;
    public TestController(IMediator mediator, IFluentEmail fluentEmail) : base(mediator)
    {
        _fluentEmail = fluentEmail;
    }

    [HttpGet]
    public async Task<IActionResult> SendTestEmail()
    {
        await _fluentEmail
            .To("taberkkaya@gmail.com")
            .Subject("Test")
            .Body("<h1>Test</h1>", true)
            .SendAsync();

        return NoContent();
    }
}
