using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using eAccountingServer.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Products;

public sealed record ProductDto(
    Guid Id,
    string? Code,
    string Name,
    string Unit,
    bool IsService,
    decimal PurchasePrice,
    decimal SalePrice,
    int VatRate,
    string CurrencyName,
    int CurrencyTypeValue,
    decimal StockQuantity,
    decimal CriticalStock,
    string? Description,
    /// <summary>Kritik seviye verilmiş ve altına düşülmüşse.</summary>
    bool IsBelowCritical);

// --- listeleme ---------------------------------------------------------------

/// <param name="OnlyLowStock">Kritik seviyenin altındakiler; sipariş listesi için.</param>
public sealed record GetAllProductsQuery(
    string? Search = null,
    bool? IsService = null,
    bool OnlyLowStock = false) : IRequest<Result<List<ProductDto>>>;

internal sealed class GetAllProductsQueryHandler(
    IProductRepository productRepository
    ) : IRequestHandler<GetAllProductsQuery, Result<List<ProductDto>>>
{
    public async Task<Result<List<ProductDto>>> Handle(
        GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        List<Product> products = await productRepository
            .GetAll().OrderBy(p => p.Name).ToListAsync(cancellationToken);

        if (request.IsService is { } isService)
            products = products.Where(p => p.IsService == isService).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim();

            products = products.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Code ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (request.OnlyLowStock)
            products = products
                .Where(p => !p.IsService && p.CriticalStock > 0 && p.StockQuantity <= p.CriticalStock)
                .ToList();

        return products.Select(Map).ToList();
    }

    internal static ProductDto Map(Product p) => new(
        p.Id, p.Code, p.Name, p.Unit, p.IsService,
        p.PurchasePrice, p.SalePrice, p.VatRate,
        p.CurrencyType.Name, p.CurrencyType.Value,
        p.StockQuantity, p.CriticalStock, p.Description,
        !p.IsService && p.CriticalStock > 0 && p.StockQuantity <= p.CriticalStock);
}

// --- oluşturma ---------------------------------------------------------------

public sealed record CreateProductCommand(
    string Name,
    string? Code,
    string Unit,
    bool IsService,
    decimal PurchasePrice,
    decimal SalePrice,
    int VatRate,
    int CurrencyTypeValue,
    decimal OpeningStock,
    decimal CriticalStock,
    string? Description) : IRequest<Result<string>>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(p => p.Unit)
            .NotEmpty().WithMessage("Birim zorunludur.")
            .MaximumLength(20).WithMessage("Birim en fazla 20 karakter olabilir.");

        RuleFor(p => p.VatRate)
            .InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.");

        RuleFor(p => p.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Alış fiyatı eksi olamaz.");

        RuleFor(p => p.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Satış fiyatı eksi olamaz.");
    }
}

internal sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IStockTransactionRepository stockTransactionRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<CreateProductCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        CreateProductCommand request, CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();
        string? code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        if (await productRepository.AnyAsync(p => p.Name == name, cancellationToken))
            return Result<string>.Failure("Bu isimde bir kayıt zaten var.");

        // Kod barkodla okutulacağı için benzersiz olmalı, ama boş bırakılabilir.
        if (code is not null
            && await productRepository.AnyAsync(p => p.Code == code, cancellationToken))
            return Result<string>.Failure($"'{code}' kodu başka bir kayıtta kullanılıyor.");

        Product product = new()
        {
            Name = name,
            Code = code,
            Unit = request.Unit.Trim(),
            IsService = request.IsService,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            VatRate = request.VatRate,
            CurrencyType = CurrencyTypeEnum.FromValue(request.CurrencyTypeValue),
            CriticalStock = request.IsService ? 0 : request.CriticalStock,
            StockQuantity = 0,
            Description = request.Description?.Trim()
        };

        await productRepository.AddAsync(product, cancellationToken);

        // Açılış stoğu da bir hareket: miktar bir yerden gelmiş olmalı.
        if (!request.IsService && request.OpeningStock > 0)
        {
            product.StockQuantity = request.OpeningStock;

            await stockTransactionRepository.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                Date = DateOnly.FromDateTime(DateTime.Today),
                Direction = StockDirection.In,
                Quantity = request.OpeningStock,
                UnitPrice = request.PurchasePrice,
                Description = "Açılış stoğu"
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return request.IsService ? "Hizmet eklendi." : "Ürün eklendi.";
    }
}

// --- güncelleme --------------------------------------------------------------

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Code,
    string Unit,
    bool IsService,
    decimal PurchasePrice,
    decimal SalePrice,
    int VatRate,
    int CurrencyTypeValue,
    decimal CriticalStock,
    string? Description) : IRequest<Result<string>>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(p => p.Unit).NotEmpty().WithMessage("Birim zorunludur.");

        RuleFor(p => p.VatRate)
            .InclusiveBetween(0, 100).WithMessage("KDV oranı 0 ile 100 arasında olmalıdır.");
    }
}

internal sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<UpdateProductCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return Result<string>.Failure("Kayıt bulunamadı.");

        string name = request.Name.Trim();
        string? code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();

        if (await productRepository.AnyAsync(
            p => p.Name == name && p.Id != request.Id, cancellationToken))
            return Result<string>.Failure("Bu isimde başka bir kayıt var.");

        if (code is not null && await productRepository.AnyAsync(
            p => p.Code == code && p.Id != request.Id, cancellationToken))
            return Result<string>.Failure($"'{code}' kodu başka bir kayıtta kullanılıyor.");

        product.Name = name;
        product.Code = code;
        product.Unit = request.Unit.Trim();
        product.IsService = request.IsService;
        product.PurchasePrice = request.PurchasePrice;
        product.SalePrice = request.SalePrice;
        product.VatRate = request.VatRate;
        product.CurrencyType = CurrencyTypeEnum.FromValue(request.CurrencyTypeValue);
        product.CriticalStock = request.IsService ? 0 : request.CriticalStock;
        product.Description = request.Description?.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Kayıt güncellendi.";
    }
}

// --- stok düzeltme -----------------------------------------------------------

/// <summary>
/// Sayım sonrası elle düzeltme. Faturaya bağlı olmayan tek stok hareketi bu;
/// sayımda çıkan farkın kayda geçmesi lazım.
/// </summary>
/// <param name="Direction">0 giriş, 1 çıkış.</param>
public sealed record AdjustStockCommand(
    Guid ProductId,
    int Direction,
    decimal Quantity,
    DateOnly Date,
    string? Description) : IRequest<Result<string>>;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(p => p.Quantity)
            .GreaterThan(0).WithMessage("Miktar sıfırdan büyük olmalıdır.");

        RuleFor(p => p.Direction)
            .InclusiveBetween(0, 1).WithMessage("Hareket giriş ya da çıkış olmalıdır.");
    }
}

internal sealed class AdjustStockCommandHandler(
    IProductRepository productRepository,
    IStockTransactionRepository stockTransactionRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<AdjustStockCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        AdjustStockCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            return Result<string>.Failure("Ürün bulunamadı.");

        if (product.IsService)
            return Result<string>.Failure("Hizmetlerin stoğu tutulmuyor.");

        bool isIn = request.Direction == 0;

        product.StockQuantity += isIn ? request.Quantity : -request.Quantity;

        await stockTransactionRepository.AddAsync(new StockTransaction
        {
            ProductId = product.Id,
            Date = request.Date,
            Direction = isIn ? StockDirection.In : StockDirection.Out,
            Quantity = request.Quantity,
            UnitPrice = isIn ? product.PurchasePrice : product.SalePrice,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? "Stok düzeltme"
                : request.Description.Trim()
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Stok güncellendi.";
    }
}

// --- stok hareketleri --------------------------------------------------------

public sealed record StockTransactionDto(
    Guid Id,
    DateOnly Date,
    int Direction,
    string DirectionName,
    decimal Quantity,
    decimal UnitPrice,
    string Description,
    Guid? InvoiceId,
    decimal RunningQuantity);

public sealed record GetStockTransactionsQuery(Guid ProductId)
    : IRequest<Result<List<StockTransactionDto>>>;

internal sealed class GetStockTransactionsQueryHandler(
    IStockTransactionRepository stockTransactionRepository
    ) : IRequestHandler<GetStockTransactionsQuery, Result<List<StockTransactionDto>>>
{
    public async Task<Result<List<StockTransactionDto>>> Handle(
        GetStockTransactionsQuery request, CancellationToken cancellationToken)
    {
        List<StockTransaction> transactions = await stockTransactionRepository
            .Where(p => p.ProductId == request.ProductId)
            .OrderBy(p => p.Date).ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        decimal running = 0;
        List<StockTransactionDto> result = [];

        foreach (StockTransaction transaction in transactions)
        {
            running += transaction.Direction == StockDirection.In
                ? transaction.Quantity
                : -transaction.Quantity;

            result.Add(new StockTransactionDto(
                transaction.Id, transaction.Date,
                (int)transaction.Direction,
                transaction.Direction == StockDirection.In ? "Giriş" : "Çıkış",
                transaction.Quantity, transaction.UnitPrice, transaction.Description,
                transaction.InvoiceId, running));
        }

        // Ekranda en yeni üstte dursun; koşu miktarı eskiden yeniye hesaplandı.
        result.Reverse();
        return result;
    }
}

// --- silme -------------------------------------------------------------------

public sealed record DeleteProductByIdCommand(Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteProductByIdCommandHandler(
    IProductRepository productRepository,
    IInvoiceLineRepository invoiceLineRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<DeleteProductByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        DeleteProductByIdCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return Result<string>.Failure("Kayıt bulunamadı.");

        if (await invoiceLineRepository.AnyAsync(p => p.ProductId == product.Id, cancellationToken))
            return Result<string>.Failure(
                "Bu kayıt faturalarda kullanılmış. Silmek yerine pasife alın.");

        productRepository.Delete(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Kayıt silindi.";
    }
}
