using eAccountingServer.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using ResultKit;

namespace eAccountingServer.WebApi.Modules
{
    public static class AuthModule
    {
        public static void RegisterAuthRoutes(this IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup("api/auth").WithTags("auth");

            group.MapPost("login",
                async (ISender sender, LoginCommand request, CancellationToken cancellationToken) =>
                {
                    var response = await sender.Send(request, cancellationToken);
                    return response.IsSuccessful ? Results.Ok(response) : Results.InternalServerError(response);
                }).Produces<Result<LoginCommandResponse>>();

            group.MapPost("confirmEmail",
                async (ISender sender, ConfirmEmailCommand request, CancellationToken cancellationToken) =>
                {
                    var response = await sender.Send(request, cancellationToken);
                    return response.IsSuccessful ? Results.Ok(response) : Results.InternalServerError(response);
                }).Produces<Result<string>>();

            group.MapPost("sendConfirmEmail",
                async (ISender sender, SendConfirmEmailCommand request, CancellationToken cancellationToken) =>
                {
                    var response = await sender.Send(request, cancellationToken);
                    return response.IsSuccessful ? Results.Ok(response) : Results.InternalServerError(response);
                }).Produces<Result<string>>();

            group.MapPost("changeCompany",
                async (ISender sender, ChangeCompanyCommand request, CancellationToken cancellationToken) =>
                {
                    var response = await sender.Send(request, cancellationToken);
                    return response.IsSuccessful ? Results.Ok(response) : Results.InternalServerError(response);
                }).Produces<Result<string>>().RequireAuthorization();
        }

    }
}
