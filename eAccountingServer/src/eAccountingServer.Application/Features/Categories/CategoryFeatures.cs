using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Repositories;
using FluentValidation;
using GenericRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResultKit;

namespace eAccountingServer.Application.Features.Categories;

public sealed record CategoryDto(Guid Id, string Name, int Direction);

// --- listeleme ---------------------------------------------------------------

public sealed record GetAllCategoriesQuery() : IRequest<Result<List<CategoryDto>>>;

internal sealed class GetAllCategoriesQueryHandler(
    ICategoryRepository categoryRepository
    ) : IRequestHandler<GetAllCategoriesQuery, Result<List<CategoryDto>>>
{
    public async Task<Result<List<CategoryDto>>> Handle(
        GetAllCategoriesQuery request, CancellationToken cancellationToken) =>
        await categoryRepository
            .GetAll()
            // Önce gelir sonra gider, her biri kendi içinde alfabetik: listeler
            // her ekranda aynı sırada çıksın.
            .OrderBy(p => p.Direction)
            .ThenBy(p => p.Name)
            .Select(p => new CategoryDto(p.Id, p.Name, p.Direction))
            .ToListAsync(cancellationToken);
}

// --- oluşturma ---------------------------------------------------------------

public sealed record CreateCategoryCommand(string Name, int Direction) : IRequest<Result<string>>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Kalem adı zorunludur.")
            .MaximumLength(80).WithMessage("Kalem adı en fazla 80 karakter olabilir.");

        RuleFor(p => p.Direction)
            .InclusiveBetween(0, 1).WithMessage("Kalem gelir ya da gider olmalıdır.");
    }
}

internal sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<CreateCategoryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();

        bool exists = await categoryRepository
            .AnyAsync(p => p.Name == name && p.Direction == request.Direction, cancellationToken);

        if (exists)
            return Result<string>.Failure("Bu kalem zaten var.");

        await categoryRepository.AddAsync(
            new Category { Name = name, Direction = request.Direction }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Kalem eklendi.";
    }
}

// --- güncelleme --------------------------------------------------------------

public sealed record UpdateCategoryCommand(Guid Id, string Name, int Direction) : IRequest<Result<string>>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Kalem adı zorunludur.")
            .MaximumLength(80).WithMessage("Kalem adı en fazla 80 karakter olabilir.");
    }
}

internal sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<UpdateCategoryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (category is null)
            return Result<string>.Failure("Kalem bulunamadı.");

        category.Name = request.Name.Trim();
        category.Direction = request.Direction;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Kalem güncellendi.";
    }
}

// --- silme -------------------------------------------------------------------

public sealed record DeleteCategoryByIdCommand(Guid Id) : IRequest<Result<string>>;

internal sealed class DeleteCategoryByIdCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWorkCompany unitOfWork
    ) : IRequestHandler<DeleteCategoryByIdCommand, Result<string>>
{
    public async Task<Result<string>> Handle(DeleteCategoryByIdCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository
            .GetByExpressionWithTrackingAsync(p => p.Id == request.Id, cancellationToken);

        if (category is null)
            return Result<string>.Failure("Kalem bulunamadı.");

        // Yumuşak silme: geçmiş hareketler kalemi göstermeye devam etsin, yalnızca
        // yeni kayıtlarda seçilemesin.
        categoryRepository.Delete(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return "Kalem silindi.";
    }
}
