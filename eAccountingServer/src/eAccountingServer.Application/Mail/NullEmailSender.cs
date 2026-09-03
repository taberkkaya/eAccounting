using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;

namespace eAccountingServer.Application.Mail;

/// <summary>
/// Stands in for a real SMTP sender when no mail host is configured. Account creation
/// and email confirmation publish notifications unconditionally, and a demo deployment
/// with no mail server must not fail those requests because of it.
/// </summary>
internal sealed class NullEmailSender(ILogger<NullEmailSender> logger) : ISender
{
    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        logger.LogInformation(
            "SMTP is not configured; dropping mail to {Recipients} with subject {Subject}.",
            string.Join(", ", email.Data.ToAddresses.Select(a => a.EmailAddress)),
            email.Data.Subject);

        return new SendResponse();
    }

    public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null) =>
        Task.FromResult(Send(email, token));
}
