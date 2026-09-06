using eAccountingServer.Application.Features.Accounting;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Invoices;

public sealed record InvoiceLineDto(
    Guid Id,
    Guid? ProductId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    int VatRate,
    decimal LineTotal,
    decimal VatAmount);

public sealed record InvoiceDto(
    Guid Id,
    int Type,
    string TypeName,
    string Number,
    DateOnly Date,
    DateOnly DueDate,
    Guid ContactId,
    string ContactName,
    string CurrencyName,
    int CurrencyTypeValue,
    int Status,
    string StatusName,
    decimal SubTotal,
    decimal DiscountTotal,
    decimal VatTotal,
    decimal GrandTotal,
    decimal PaidAmount,
    decimal RemainingAmount,
    /// <summary>Vadesi geçmiş ve hâlâ kapanmamış.</summary>
    bool IsOverdue,
    string? Note,
    List<InvoiceLineDto> Lines);

/// <summary>İstemciye gönderilen satır; kimlik sunucuda üretiliyor.</summary>
public sealed record InvoiceLineInput(
    Guid? ProductId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate,
    int VatRate);

internal static class InvoiceMapping
{
    public static string TypeName(InvoiceType type) =>
        type == InvoiceType.Sales ? "Satış Faturası" : "Alış Faturası";

    public static string StatusName(InvoiceStatus status) => status switch
    {
        InvoiceStatus.PartiallyPaid => "Kısmi Ödendi",
        InvoiceStatus.Paid => "Ödendi",
        _ => "Açık"
    };

    public static InvoiceDto Map(Invoice invoice, string contactName)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        return new InvoiceDto(
            invoice.Id, (int)invoice.Type, TypeName(invoice.Type), invoice.Number,
            invoice.Date, invoice.DueDate, invoice.ContactId, contactName,
            invoice.CurrencyType.Name, invoice.CurrencyType.Value,
            (int)invoice.Status, StatusName(invoice.Status),
            invoice.SubTotal, invoice.DiscountTotal, invoice.VatTotal, invoice.GrandTotal,
            invoice.PaidAmount, invoice.GrandTotal - invoice.PaidAmount,
            invoice.DueDate < today && invoice.Status != InvoiceStatus.Paid,
            invoice.Note,
            (invoice.Lines ?? [])
                .Select(l => new InvoiceLineDto(
                    l.Id, l.ProductId, l.Description, l.Unit, l.Quantity, l.UnitPrice,
                    l.DiscountRate, l.VatRate, l.LineTotal, l.VatAmount))
                .ToList());
    }
}

/// <summary>
/// Satır ve fatura toplamlarını hesaplar.
///
/// İstemci de aynı hesabı ekranda yapıyor ama gönderdiği toplamlara güvenmiyoruz:
/// para tutarı, tarayıcıdan gelen bir sayı değil sunucunun kendi hesabı olmalı.
/// </summary>
internal static class InvoiceCalculator
{
    public static (decimal LineTotal, decimal VatAmount, decimal Discount) Line(
        decimal quantity, decimal unitPrice, decimal discountRate, int vatRate)
    {
        decimal gross = Round(quantity * unitPrice);
        decimal discount = Round(gross * discountRate / 100m);
        decimal lineTotal = gross - discount;
        decimal vat = Round(lineTotal * vatRate / 100m);

        return (lineTotal, vat, discount);
    }

    public static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

// --- listeleme ---------------------------------------------------------------

/// <param name="Type">1 satış, 2 alış, null ikisi de.</param>
/// <param name="Status">Fatura durumu; null hepsi.</param>
/// <param name="OnlyOverdue">Vadesi geçmiş açık faturalar.</param>
public sealed record GetAllInvoicesQuery(
    int? Type = null,
    Guid? ContactId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? Status = null,
    bool OnlyOverdue = false,
    string? Search = null,
    int Take = 500) : IRequest<Result<List<InvoiceDto>>>;

internal sealed class GetAllInvoicesQueryHandler(
    IInvoiceRepository invoiceRepository,
    IContactRepository contactRepository
    ) : IRequestHandler<GetAllInvoicesQuery, Result<List<InvoiceDto>>>
{
    public async Task<Result<List<InvoiceDto>>> Handle(
        GetAllInvoicesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Invoice> query = invoiceRepository.GetAll();

        if (request.Type is { } type) query = query.Where(p => p.Type == (InvoiceType)type);
        if (request.ContactId is { } contactId) query = query.Where(p => p.ContactId == contactId);
        if (request.StartDate is { } start) query = query.Where(p => p.Date >= start);
        if (request.EndDate is { } end) query = query.Where(p => p.Date <= end);
        if (request.Status is { } status) query = query.Where(p => p.Status == (InvoiceStatus)status);

        if (request.OnlyOverdue)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            query = query.Where(p => p.DueDate < today && p.Status != InvoiceStatus.Paid);
        }

        List<Invoice> invoices = await query
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt)
            .Take(Math.Clamp(request.Take, 1, 2000))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> contacts = await contactRepository
            .GetAll().ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        IEnumerable<InvoiceDto> result = invoices.Select(p => InvoiceMapping.Map(
            p, contacts.TryGetValue(p.ContactId, out string? name) ? name : "—"));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim();

            result = result.Where(p =>
                p.Number.Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.ContactName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Note ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return result.ToList();
    }
}

// --- tek fatura --------------------------------------------------------------

public sealed record GetInvoiceByIdQuery(Guid Id) : IRequest<Result<InvoiceDto>>;

internal sealed class GetInvoiceByIdQueryHandler(
    IInvoiceRepository invoiceRepository,
    IContactRepository contactRepository
    ) : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(
        GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        Invoice? invoice = await invoiceRepository
            .Where(p => p.Id == request.Id)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return Result<InvoiceDto>.Failure("Fatura bulunamadı.");

        Contact? contact = await contactRepository
            .Where(p => p.Id == invoice.ContactId).FirstOrDefaultAsync(cancellationToken);

        invoice.Lines = (invoice.Lines ?? []).OrderBy(p => p.CreatedAt).ToList();

        return InvoiceMapping.Map(invoice, contact?.Name ?? "—");
    }
}

// --- sıradaki numara ---------------------------------------------------------

/// <summary>
/// Yeni faturanın önerilen numarası. Kullanıcı değiştirebilir; kendi
/// numaralarını kullananlar için dayatma olmasın.
/// </summary>
public sealed record GetNextInvoiceNumberQuery(int Type) : IRequest<Result<string>>;

internal sealed class GetNextInvoiceNumberQueryHandler(
    IInvoiceRepository invoiceRepository
    ) : IRequestHandler<GetNextInvoiceNumberQuery, Result<string>>
{
    public async Task<Result<string>> Handle(
        GetNextInvoiceNumberQuery request, CancellationToken cancellationToken) =>
        await NextNumberAsync(invoiceRepository, (InvoiceType)request.Type, cancellationToken);

    internal static async Task<string> NextNumberAsync(
        IInvoiceRepository invoiceRepository, InvoiceType type, CancellationToken cancellationToken)
    {
        string prefix = $"{(type == InvoiceType.Sales ? "SF" : "AF")}{DateTime.Today.Year}";

        // Yıl içindeki en büyük sıra numarasının bir fazlası. Silinmiş faturalar
        // da sayılıyor (IgnoreQueryFilters yok ama silinen numara geri gelmesin
        // diye sıra en büyükten devam ediyor).
        List<string> numbers = await invoiceRepository
            .Where(p => p.Type == type && p.Number.StartsWith(prefix))
            .Select(p => p.Number)
            .ToListAsync(cancellationToken);

        int next = numbers
            .Select(n => int.TryParse(n[prefix.Length..], out int value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{next:D6}";
    }
}

// --- oluşturma ---------------------------------------------------------------

/// <param name="PaidWithAccountId">
/// Peşin satışlar için: verilirse fatura tutarı kadar tahsilat/ödeme de yazılır.
/// Kasadan geçen bir satışta iki ayrı ekran doldurmak zorunda kalmamak için.
/// </param>
public sealed record CreateInvoiceCommand(
    int Type,
    Guid ContactId,
    DateOnly Date,
    DateOnly DueDate,
    List<InvoiceLineInput> Lines,
    string? Number = null,
    string? Note = null,
    Guid? PaidWithAccountId = null) : IRequest<Result<string>>;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(p => p.Type)
            .InclusiveBetween(1, 2).WithMessage("Fatura türü satış ya da alış olmalıdır.");

        RuleFor(p => p.ContactId).NotEmpty().WithMessage("Cari seçilmelidir.");

        RuleFor(p => p.Lines)
            .NotEmpty().WithMessage("Faturada en az bir satır olmalıdır.");

        RuleFor(p => p.DueDate)
            .GreaterThanOrEqualTo(p => p.Date)
            .WithMessage("Vade tarihi fatura tarihinden önce olamaz.");

        RuleForEach(p => p.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description)
                .NotEmpty().WithMessage("Satır açıklaması zorunludur.")
                .MaximumLength(400).WithMessage("Satır açıklaması en fazla 400 karakter olabilir.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.");

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Birim fiyat eksi olamaz.");

            line.RuleFor(l => l.DiscountRate)
                .InclusiveBetween(0, 100).WithMessage("İndirim 0 ile 100 arasında olmalıdır.");

            line.RuleFor(l => l.VatRate)
                .InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.");
        });
    }
}

internal sealed class CreateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceLineRepository invoiceLineRepository,
    IContactRepository contactRepository,
    IProductRepository productRepository,
    IStockTransactionRepository stockTransactionRepository,
    AccountingLedger ledger,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<CreateInvoiceCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        InvoiceType type = (InvoiceType)request.Type;

        Contact? contact = await contactRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.ContactId, cancellationToken);

        if (contact is null)
            return Result<string>.Failure("Cari bulunamadı.");

        string number = string.IsNullOrWhiteSpace(request.Number)
            ? await GetNextInvoiceNumberQueryHandler
                .NextNumberAsync(invoiceRepository, type, cancellationToken)
            : request.Number.Trim();

        if (await invoiceRepository.AnyAsync(
            p => p.Type == type && p.Number == number, cancellationToken))
            return Result<string>.Failure($"'{number}' numaralı fatura zaten var.");

        AccountInfo? paidWith = null;

        if (request.PaidWithAccountId is { } accountId)
        {
            paidWith = await ledger.FindAccountAsync(accountId, cancellationToken);

            if (paidWith is null)
                return Result<string>.Failure("Ödemenin yapılacağı hesap bulunamadı.");

            if (paidWith.CurrencyValue != contact.CurrencyType.Value)
                return Result<string>.Failure(
                    "Peşin ödeme için carinin para birimiyle aynı bir hesap seçin.");
        }

        Invoice invoice = new()
        {
            Type = type,
            Number = number,
            Date = request.Date,
            DueDate = request.DueDate,
            ContactId = contact.Id,
            CurrencyType = contact.CurrencyType,
            Status = InvoiceStatus.Approved,
            Note = request.Note?.Trim()
        };

        await invoiceRepository.AddAsync(invoice, cancellationToken);

        Result<string>? failure = await WriteLinesAsync(
            invoice, request.Lines, productRepository, invoiceLineRepository,
            stockTransactionRepository, cancellationToken);

        if (failure is not null) return failure;

        // Satış carinin borcunu artırır, alış bizim borcumuzu.
        await ledger.PostToContactAsync(
            contact, invoice.Date,
            $"{InvoiceMapping.TypeName(type)} {invoice.Number}",
            ContactTransactionKind.Invoice,
            debit: type == InvoiceType.Sales ? invoice.GrandTotal : 0,
            credit: type == InvoiceType.Sales ? 0 : invoice.GrandTotal,
            invoiceId: invoice.Id,
            account: null, accountTransactionId: null,
            cancellationToken);

        if (paidWith is not null)
        {
            bool isCollection = type == InvoiceType.Sales;

            Guid entryId = await ledger.PostToAccountAsync(
                paidWith, invoice.Date,
                $"{invoice.Number} - {contact.Name}",
                deposit: isCollection ? invoice.GrandTotal : 0,
                withdrawal: isCollection ? 0 : invoice.GrandTotal,
                contactId: contact.Id, categoryId: null, cancellationToken);

            await ledger.PostToContactAsync(
                contact, invoice.Date,
                $"{(isCollection ? "Tahsilat" : "Ödeme")} - {invoice.Number}",
                isCollection ? ContactTransactionKind.Collection : ContactTransactionKind.Payment,
                debit: isCollection ? 0 : invoice.GrandTotal,
                credit: isCollection ? invoice.GrandTotal : 0,
                invoiceId: invoice.Id, account: paidWith, accountTransactionId: entryId,
                cancellationToken);

            invoice.PaidAmount = invoice.GrandTotal;
            invoice.Status = InvoiceStatus.Paid;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"{invoice.Number} numaralı fatura oluşturuldu.";
    }

    /// <summary>
    /// Satırları yazar, toplamları hesaplar ve stoğu hareketlendirir. Oluşturma
    /// ve güncelleme aynı yolu kullanıyor ki iki yerde farklı davranmasın.
    /// </summary>
    internal static async Task<Result<string>?> WriteLinesAsync(
        Invoice invoice,
        List<InvoiceLineInput> inputs,
        IProductRepository productRepository,
        IInvoiceLineRepository invoiceLineRepository,
        IStockTransactionRepository stockTransactionRepository,
        CancellationToken cancellationToken)
    {
        decimal subTotal = 0, vatTotal = 0, discountTotal = 0;

        foreach (InvoiceLineInput input in inputs)
        {
            Product? product = input.ProductId is { } productId
                ? await productRepository
                    .GetByExpressionWithTrackingAsync(p => p.Id == productId, cancellationToken)
                : null;

            if (input.ProductId is not null && product is null)
                return Result<string>.Failure("Satırdaki ürün bulunamadı.");

            (decimal lineTotal, decimal vat, decimal discount) = InvoiceCalculator.Line(
                input.Quantity, input.UnitPrice, input.DiscountRate, input.VatRate);

            subTotal += lineTotal;
            vatTotal += vat;
            discountTotal += discount;

            await invoiceLineRepository.AddAsync(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                ProductId = input.ProductId,
                Description = input.Description.Trim(),
                Unit = string.IsNullOrWhiteSpace(input.Unit) ? "Adet" : input.Unit.Trim(),
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                DiscountRate = input.DiscountRate,
                VatRate = input.VatRate,
                LineTotal = lineTotal,
                VatAmount = vat
            }, cancellationToken);

            // Hizmetin stoğu yok; ürünse satış çıkarır, alış girer.
            if (product is null || product.IsService) continue;

            bool isOut = invoice.Type == InvoiceType.Sales;

            product.StockQuantity += isOut ? -input.Quantity : input.Quantity;

            await stockTransactionRepository.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                Date = invoice.Date,
                Direction = isOut ? StockDirection.Out : StockDirection.In,
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                Description = $"{InvoiceMapping.TypeName(invoice.Type)} {invoice.Number}",
                InvoiceId = invoice.Id
            }, cancellationToken);
        }

        invoice.SubTotal = InvoiceCalculator.Round(subTotal);
        invoice.DiscountTotal = InvoiceCalculator.Round(discountTotal);
        invoice.VatTotal = InvoiceCalculator.Round(vatTotal);
        invoice.GrandTotal = InvoiceCalculator.Round(subTotal + vatTotal);

        return null;
    }
}

// --- güncelleme --------------------------------------------------------------

public sealed record UpdateInvoiceCommand(
    Guid Id,
    DateOnly Date,
    DateOnly DueDate,
    List<InvoiceLineInput> Lines,
    string? Note = null) : IRequest<Result<string>>;

public sealed class UpdateInvoiceCommandValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceCommandValidator()
    {
        RuleFor(p => p.Lines).NotEmpty().WithMessage("Faturada en az bir satır olmalıdır.");

        RuleFor(p => p.DueDate)
            .GreaterThanOrEqualTo(p => p.Date)
            .WithMessage("Vade tarihi fatura tarihinden önce olamaz.");

        RuleForEach(p => p.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().WithMessage("Satır açıklaması zorunludur.");
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.");
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Birim fiyat eksi olamaz.");
            line.RuleFor(l => l.VatRate).InclusiveBetween(0, 100).WithMessage("KDV oranı geçersiz.");
        });
    }
}

internal sealed class UpdateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceLineRepository invoiceLineRepository,
    IContactRepository contactRepository,
    IProductRepository productRepository,
    IStockTransactionRepository stockTransactionRepository,
    AccountingLedger ledger,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<UpdateInvoiceCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        Invoice? invoice = await invoiceRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (invoice is null)
            return Result<string>.Failure("Fatura bulunamadı.");

        // Tahsilatı yapılmış faturayı değiştirmek, tahsilatı da yeniden dağıtmak
        // demek. Önce tahsilatı silmek daha az sürprizli.
        if (invoice.PaidAmount > 0)
            return Result<string>.Failure(
                "Bu faturaya tahsilat/ödeme işlenmiş. Önce onu silin, sonra faturayı düzenleyin.");

        Contact? contact = await contactRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == invoice.ContactId, cancellationToken);

        if (contact is null)
            return Result<string>.Failure("Cari bulunamadı.");

        await UnpostAsync(invoice, invoiceLineRepository, productRepository,
            stockTransactionRepository, ledger, cancellationToken);

        invoice.Date = request.Date;
        invoice.DueDate = request.DueDate;
        invoice.Note = request.Note?.Trim();

        Result<string>? failure = await CreateInvoiceCommandHandler.WriteLinesAsync(
            invoice, request.Lines, productRepository, invoiceLineRepository,
            stockTransactionRepository, cancellationToken);

        if (failure is not null) return failure;

        await ledger.PostToContactAsync(
            contact, invoice.Date,
            $"{InvoiceMapping.TypeName(invoice.Type)} {invoice.Number}",
            ContactTransactionKind.Invoice,
            debit: invoice.Type == InvoiceType.Sales ? invoice.GrandTotal : 0,
            credit: invoice.Type == InvoiceType.Sales ? 0 : invoice.GrandTotal,
            invoiceId: invoice.Id, account: null, accountTransactionId: null,
            cancellationToken);

        invoice.Status = InvoiceStatus.Approved;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Fatura güncellendi.";
    }

    /// <summary>
    /// Faturanın bıraktığı bütün izleri siler: satırlar, stok hareketleri ve
    /// cari kaydı. Güncelleme "sil ve yeniden yaz" olarak çalışıyor; kısmi
    /// güncellemede satır eşleştirmesi hata yapmaya çok açık.
    /// </summary>
    internal static async Task UnpostAsync(
        Invoice invoice,
        IInvoiceLineRepository invoiceLineRepository,
        IProductRepository productRepository,
        IStockTransactionRepository stockTransactionRepository,
        AccountingLedger ledger,
        CancellationToken cancellationToken)
    {
        List<StockTransaction> stockEntries = await stockTransactionRepository
            .Where(p => p.InvoiceId == invoice.Id).ToListAsync(cancellationToken);

        foreach (StockTransaction entry in stockEntries)
        {
            Product? product = await productRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == entry.ProductId, cancellationToken);

            if (product is not null)
                product.StockQuantity += entry.Direction == StockDirection.In
                    ? -entry.Quantity
                    : entry.Quantity;

            StockTransaction? tracked = await stockTransactionRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == entry.Id, cancellationToken);

            if (tracked is not null) stockTransactionRepository.Delete(tracked);
        }

        List<InvoiceLine> lines = await invoiceLineRepository
            .Where(p => p.InvoiceId == invoice.Id).ToListAsync(cancellationToken);

        foreach (InvoiceLine line in lines)
        {
            InvoiceLine? tracked = await invoiceLineRepository
                .GetByExpressionWithTrackingAsync(p => p.Id == line.Id, cancellationToken);

            if (tracked is not null) invoiceLineRepository.Delete(tracked);
        }

        await ledger.RemoveInvoiceEntriesAsync(invoice.Id, cancellationToken);
    }
}

// --- silme -------------------------------------------------------------------

public sealed record DeleteInvoiceByIdCommand(Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteInvoiceByIdCommandHandler(
    IInvoiceRepository invoiceRepository,
    IInvoiceLineRepository invoiceLineRepository,
    IProductRepository productRepository,
    IStockTransactionRepository stockTransactionRepository,
    AccountingLedger ledger,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<DeleteInvoiceByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        DeleteInvoiceByIdCommand request, CancellationToken cancellationToken)
    {
        Invoice? invoice = await invoiceRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (invoice is null)
            return Result<string>.Failure("Fatura bulunamadı.");

        await UpdateInvoiceCommandHandler.UnpostAsync(
            invoice, invoiceLineRepository, productRepository,
            stockTransactionRepository, ledger, cancellationToken);

        invoiceRepository.Delete(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Fatura silindi.";
    }
}
