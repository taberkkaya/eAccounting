using eAccountingServer.Domain.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ResultKit;

namespace eAccountingServer.WebApi
{
    public class ExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            Result<string> errorResult;

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = 500;

            if (exception.GetType() == typeof(ValidationException))
            {
                httpContext.Response.StatusCode = 403;

                errorResult = Result<string>.Failure(403, ((ValidationException)exception).Errors.Select(s => s.PropertyName).ToList());

                await httpContext.Response.WriteAsJsonAsync(errorResult);

                return true;
            }

            if (exception is CompanyNotSelectedException)
            {
                // Sunucu arızası değil, eksik bir kurulum: istemci bunu ayırt
                // edebilsin diye 400 dönüyor.
                httpContext.Response.StatusCode = 400;

                errorResult = Result<string>.Failure(400, [exception.Message]);

                await httpContext.Response.WriteAsJsonAsync(errorResult);

                return true;
            }

            errorResult = Result<string>.Failure(exception.Message);

            await httpContext.Response.WriteAsJsonAsync(errorResult);

            return true;
        }
    }
}
