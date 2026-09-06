using eAccountingServer.Application.Features.Accounting;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Contacts;

public sealed record ContactDto(
    Guid Id,
    string Name,
    int Type,
    string TypeName,
    string? TaxNumber,
    string? TaxOffice,
    string? Phone,
    string? Email,
    string? Address,
    string? Note,
    string CurrencyName,
    int CurrencyTypeValue,
    decimal DebitAmount,
    decimal CreditAmount,
    /// <summary>Artı ise cari bize borçlu, eksi ise biz ona borçluyuz.</summary>
    decimal Balance,
    /// <summary>Vadesi geçmiş ve hâlâ kapanmamış fatura tutarı.</summary>
    decimal OverdueAmount);

internal static class ContactMapping
{
    public static string NameOf(ContactType type) => type switch
    {
        ContactType.Customer => "Müşteri",
        ContactType.Supplier => "Tedarikçi",
        _ => "Müşteri / Tedarikçi"
    };
}

// --- listeleme ---------------------------------------------------------------

/// <param name="Type">1 müşteri, 2 tedarikçi, null hepsi.</param>
/// <param name="OnlyWithBalance">Bakiyesi sıfır olmayanlar; mutabakat için.</param>
public sealed record GetAllContactsQuery(
    int? Type = null,
    string? Search = null,
    bool OnlyWithBalance = false) : IRequest<Result<List<ContactDto>>>;

internal sealed class GetAllContactsQueryHandler(
    IContactRepository contactRepository,
    IInvoiceRepository invoiceRepository
    ) : IRequestHandler<GetAllContactsQuery, Result<List<ContactDto>>>
{
    public async Task<Result<List<ContactDto>>> Handle(
        GetAllContactsQuery request, CancellationToken cancellationToken)
    {
        List<Contact> contacts = await contactRepository
            .GetAll().OrderBy(p => p.Name).ToListAsync(cancellationToken);

        if (request.Type is { } type)
            // "Her ikisi" carisi hem müşteri hem tedarikçi listesinde görünmeli.
            contacts = contacts
                .Where(p => p.Type == (ContactType)type || p.Type == ContactType.Both)
                .ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim();

            contacts = contacts.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.TaxNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Phone ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Email ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (request.OnlyWithBalance)
            contacts = contacts.Where(p => p.DebitAmount != p.CreditAmount).ToList();

        Dictionary<Guid, decimal> overdue = await OverdueByContactAsync(
            invoiceRepository, cancellationToken);

        return contacts
            .Select(p => new ContactDto(
                p.Id, p.Name, (int)p.Type, ContactMapping.NameOf(p.Type),
                p.TaxNumber, p.TaxOffice, p.Phone, p.Email, p.Address, p.Note,
                p.CurrencyType.Name, p.CurrencyType.Value,
                p.DebitAmount, p.CreditAmount, p.DebitAmount - p.CreditAmount,
                overdue.TryGetValue(p.Id, out decimal amount) ? amount : 0))
            .ToList();
    }

    /// <summary>
    /// Cari başına vadesi geçmiş açık tutar. Bakiye "ne kadar", bu "ne kadarı
    /// gecikti" sorusunu cevaplıyor; listede ikisi yan yana duruyor.
    /// </summary>
    internal static async Task<Dictionary<Guid, decimal>> OverdueByContactAsync(
        IInvoiceRepository invoiceRepository, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        return await invoiceRepository
            .Where(p => p.DueDate < today
                && p.Status != InvoiceStatus.Paid
                && p.Status != InvoiceStatus.Cancelled
                && p.Status != InvoiceStatus.Draft)
            .GroupBy(p => p.ContactId)
            .Select(g => new { g.Key, Amount = g.Sum(p => p.GrandTotal - p.PaidAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Amount, cancellationToken);
    }
}

// --- tek kayıt ---------------------------------------------------------------

public sealed record GetContactByIdQuery(Guid Id) : IRequest<Result<ContactDto>>;

internal sealed class GetContactByIdQueryHandler(
    IContactRepository contactRepository,
    IInvoiceRepository invoiceRepository
    ) : IRequestHandler<GetContactByIdQuery, Result<ContactDto>>
{
    public async Task<Result<ContactDto>> Handle(
        GetContactByIdQuery request, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .Where(p => p.Id == request.Id).FirstOrDefaultAsync(cancellationToken);

        if (contact is null)
            return Result<ContactDto>.Failure("Cari bulunamadı.");

        Dictionary<Guid, decimal> overdue = await GetAllContactsQueryHandler
            .OverdueByContactAsync(invoiceRepository, cancellationToken);

        return new ContactDto(
            contact.Id, contact.Name, (int)contact.Type, ContactMapping.NameOf(contact.Type),
            contact.TaxNumber, contact.TaxOffice, contact.Phone, contact.Email,
            contact.Address, contact.Note,
            contact.CurrencyType.Name, contact.CurrencyType.Value,
            contact.DebitAmount, contact.CreditAmount, contact.DebitAmount - contact.CreditAmount,
            overdue.TryGetValue(contact.Id, out decimal amount) ? amount : 0);
    }
}

// --- oluşturma ---------------------------------------------------------------

/// <param name="OpeningBalance">
/// Artı: cari bize borçlu başlıyor. Eksi: biz ona borçluyuz. Programa geçerken
/// eski defterdeki bakiyeyi taşımanın yolu bu.
/// </param>
public sealed record CreateContactCommand(
    string Name,
    int Type,
    int CurrencyTypeValue,
    string? TaxNumber,
    string? TaxOffice,
    string? Phone,
    string? Email,
    string? Address,
    string? Note,
    decimal OpeningBalance = 0) : IRequest<Result<string>>;

public sealed class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Cari adı zorunludur.")
            .MaximumLength(160).WithMessage("Cari adı en fazla 160 karakter olabilir.");

        RuleFor(p => p.Type)
            .InclusiveBetween(1, 3).WithMessage("Cari türü geçersiz.");

        RuleFor(p => p.Email)
            .EmailAddress().WithMessage("E-posta adresi geçersiz.")
            .When(p => !string.IsNullOrWhiteSpace(p.Email));

        RuleFor(p => p.TaxNumber)
            .MaximumLength(20).WithMessage("Vergi/TC numarası en fazla 20 karakter olabilir.");
    }
}

internal sealed class CreateContactCommandHandler(
    IContactRepository contactRepository,
    AccountingLedger ledger,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<CreateContactCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        CreateContactCommand request, CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();

        if (await contactRepository.AnyAsync(p => p.Name == name, cancellationToken))
            return Result<string>.Failure("Bu isimde bir cari zaten var.");

        Contact contact = new()
        {
            Name = name,
            Type = (ContactType)request.Type,
            CurrencyType = CurrencyTypeEnum.FromValue(request.CurrencyTypeValue),
            TaxNumber = request.TaxNumber?.Trim(),
            TaxOffice = request.TaxOffice?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Note = request.Note?.Trim()
        };

        await contactRepository.AddAsync(contact, cancellationToken);

        // Açılış bakiyesi de ekstrede bir satır: nereden geldiği görünsün.
        if (request.OpeningBalance != 0)
            await ledger.PostToContactAsync(
                contact,
                DateOnly.FromDateTime(DateTime.Today),
                "Açılış bakiyesi",
                ContactTransactionKind.Opening,
                request.OpeningBalance > 0 ? request.OpeningBalance : 0,
                request.OpeningBalance < 0 ? -request.OpeningBalance : 0,
                null, null, null, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Cari eklendi.";
    }
}

// --- güncelleme --------------------------------------------------------------

public sealed record UpdateContactCommand(
    Guid Id,
    string Name,
    int Type,
    int CurrencyTypeValue,
    string? TaxNumber,
    string? TaxOffice,
    string? Phone,
    string? Email,
    string? Address,
    string? Note) : IRequest<Result<string>>;

public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Cari adı zorunludur.")
            .MaximumLength(160).WithMessage("Cari adı en fazla 160 karakter olabilir.");

        RuleFor(p => p.Email)
            .EmailAddress().WithMessage("E-posta adresi geçersiz.")
            .When(p => !string.IsNullOrWhiteSpace(p.Email));
    }
}

internal sealed class UpdateContactCommandHandler(
    IContactRepository contactRepository,
    IContactTransactionRepository contactTransactionRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<UpdateContactCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        UpdateContactCommand request, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (contact is null)
            return Result<string>.Failure("Cari bulunamadı.");

        string name = request.Name.Trim();

        if (await contactRepository.AnyAsync(
            p => p.Name == name && p.Id != request.Id, cancellationToken))
            return Result<string>.Failure("Bu isimde başka bir cari var.");

        // Para birimini değiştirmek geçmiş hareketleri yanlış birime taşırdı;
        // hareket varsa eski birim kalıyor.
        if (contact.CurrencyType.Value != request.CurrencyTypeValue
            && await contactTransactionRepository.AnyAsync(
                p => p.ContactId == contact.Id, cancellationToken))
            return Result<string>.Failure(
                "Hareketi olan bir carinin para birimi değiştirilemez.");

        contact.Name = name;
        contact.Type = (ContactType)request.Type;
        contact.CurrencyType = CurrencyTypeEnum.FromValue(request.CurrencyTypeValue);
        contact.TaxNumber = request.TaxNumber?.Trim();
        contact.TaxOffice = request.TaxOffice?.Trim();
        contact.Phone = request.Phone?.Trim();
        contact.Email = request.Email?.Trim();
        contact.Address = request.Address?.Trim();
        contact.Note = request.Note?.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Cari güncellendi.";
    }
}

// --- silme -------------------------------------------------------------------

public sealed record DeleteContactByIdCommand(Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteContactByIdCommandHandler(
    IContactRepository contactRepository,
    IInvoiceRepository invoiceRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<DeleteContactByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        DeleteContactByIdCommand request, CancellationToken cancellationToken)
    {
        Contact? contact = await contactRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (contact is null)
            return Result<string>.Failure("Cari bulunamadı.");

        // Faturası olan cari silinirse fatura sahipsiz kalır ve listede boş bir
        // isim görünür; önce faturayı silmek gerekiyor.
        if (await invoiceRepository.AnyAsync(p => p.ContactId == contact.Id, cancellationToken))
            return Result<string>.Failure(
                "Bu carinin faturaları var. Önce faturaları silmelisiniz.");

        contactRepository.Delete(contact);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Cari silindi.";
    }
}
