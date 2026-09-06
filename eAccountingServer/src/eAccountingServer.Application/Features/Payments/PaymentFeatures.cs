using eAccountingServer.Application.Features.Accounting;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Payments;

/// <summary>
/// Tahsilat ya da ödeme.
///
/// Tek bir kayıt iki deftere birden yazıyor: carinin bakiyesi ve kasa/banka
/// bakiyesi. <see cref="Direction"/> hangisinin hangi yöne gittiğini belirliyor;
/// gerisi ikisinde de aynı.
/// </summary>
/// <param name="Direction">0 tahsilat (para bize gelir), 1 ödeme (bizden çıkar).</param>
/// <param name="InvoiceId">
/// Belirli bir faturaya sayılacaksa. Boşsa carinin genel bakiyesine işlenir.
/// </param>
public sealed record CreatePaymentCommand(
    Guid ContactId,
    Guid AccountId,
    int Direction,
    DateOnly Date,
    decimal Amount,
    string? Description,
    Guid? InvoiceId = null) : IRequest<Result<string>>;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(p => p.ContactId).NotEmpty().WithMessage("Cari seçilmelidir.");
        RuleFor(p => p.AccountId).NotEmpty().WithMessage("Kasa ya da banka seçilmelidir.");

        RuleFor(p => p.Amount)
            .GreaterThan(0).WithMessage("Tutar sıfırdan büyük olmalıdır.");

        RuleFor(p => p.Direction)
            .InclusiveBetween(0, 1).WithMessage("İşlem tahsilat ya da ödeme olmalıdır.");
    }
}

internal sealed class CreatePaymentCommandHandler(
    IContactRepository contactRepository,
    IInvoiceRepository invoiceRepository,
    AccountingLedger ledger,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<CreatePaymentCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.ContactId, cancellationToken);

        if (contact is null)
            return Result<string>.Failure("Cari bulunamadı.");

        AccountInfo? account = await ledger.FindAccountAsync(request.AccountId, cancellationToken);

        if (account is null)
            return Result<string>.Failure("Kasa ya da banka bulunamadı.");

        // Kur çevirisi yapmıyoruz; farklı birimleri karıştırmak bakiyeyi bozar.
        if (account.CurrencyValue != contact.CurrencyType.Value)
            return Result<string>.Failure(
                $"Cari {contact.CurrencyType.Name}, seçilen hesap "
                + $"{CurrencyTypeEnum.FromValue(account.CurrencyValue).Name}. "
                + "Aynı para biriminde bir hesap seçin.");

        bool isCollection = request.Direction == 0;

        Invoice? invoice = null;

        if (request.InvoiceId is { } invoiceId)
        {
            invoice = await invoiceRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == invoiceId, cancellationToken);

            if (invoice is null)
                return Result<string>.Failure("Fatura bulunamadı.");

            if (invoice.ContactId != contact.Id)
                return Result<string>.Failure("Fatura bu cariye ait değil.");

            if (request.Amount > invoice.GrandTotal - invoice.PaidAmount)
                return Result<string>.Failure(
                    "Tutar faturanın kalan borcundan büyük olamaz.");
        }

        string description = string.IsNullOrWhiteSpace(request.Description)
            ? DefaultDescription(isCollection, contact.Name, invoice?.Number)
            : request.Description.Trim();

        Guid accountEntryId = await ledger.PostToAccountAsync(
            account, request.Date, description,
            deposit: isCollection ? request.Amount : 0,
            withdrawal: isCollection ? 0 : request.Amount,
            contactId: contact.Id,
            categoryId: null,
            cancellationToken);

        await ledger.PostToContactAsync(
            contact, request.Date, description,
            isCollection ? ContactTransactionKind.Collection : ContactTransactionKind.Payment,
            // Tahsilat carinin borcunu azaltır, ödeme artırır.
            debit: isCollection ? 0 : request.Amount,
            credit: isCollection ? request.Amount : 0,
            invoiceId: invoice?.Id,
            account: account,
            accountTransactionId: accountEntryId,
            cancellationToken);

        if (invoice is not null)
        {
            invoice.PaidAmount += request.Amount;
            invoice.Status = invoice.PaidAmount >= invoice.GrandTotal
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return isCollection ? "Tahsilat kaydedildi." : "Ödeme kaydedildi.";
    }

    private static string DefaultDescription(bool isCollection, string contactName, string? number)
    {
        string subject = isCollection ? "Tahsilat" : "Ödeme";
        string who = isCollection ? $"{contactName} tahsilatı" : $"{contactName} ödemesi";

        return number is null ? who : $"{subject} - {number}";
    }
}

// --- silme -------------------------------------------------------------------

/// <summary>
/// Bir tahsilatı ya da ödemeyi geri alır. Cari hareketi, kasa/banka hareketi ve
/// varsa faturanın ödenen tutarı birlikte düzeltilir.
/// </summary>
public sealed record DeletePaymentByIdCommand(Guid ContactTransactionId) : IRequest<Result<string>>;

internal sealed class DeletePaymentByIdCommandHandler(
    IContactTransactionRepository contactTransactionRepository,
    IInvoiceRepository invoiceRepository,
    AccountingLedger ledger,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<DeletePaymentByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        DeletePaymentByIdCommand request, CancellationToken cancellationToken)
    {
        ContactTransaction? transaction = await contactTransactionRepository
            .GetByExpressionWithTrackingAsync(
                p => p.Id == request.ContactTransactionId, cancellationToken);

        if (transaction is null)
            return Result<string>.Failure("Hareket bulunamadı.");

        if (transaction.Kind is not (ContactTransactionKind.Collection
            or ContactTransactionKind.Payment or ContactTransactionKind.Opening))
            return Result<string>.Failure(
                "Faturadan gelen hareket buradan silinemez; faturayı silin.");

        decimal amount = transaction.DebitAmount + transaction.CreditAmount;

        if (transaction.InvoiceId is { } invoiceId)
        {
            Invoice? invoice = await invoiceRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == invoiceId, cancellationToken);

            if (invoice is not null)
            {
                invoice.PaidAmount = Math.Max(0, invoice.PaidAmount - amount);
                invoice.Status = invoice.PaidAmount <= 0
                    ? InvoiceStatus.Approved
                    : invoice.PaidAmount >= invoice.GrandTotal
                        ? InvoiceStatus.Paid
                        : InvoiceStatus.PartiallyPaid;
            }
        }

        await ledger.RemoveContactEntryAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Hareket silindi.";
    }
}
