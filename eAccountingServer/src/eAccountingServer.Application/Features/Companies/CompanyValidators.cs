using FluentValidation;

namespace eAccountingServer.Application.Features.Companies;

/// <summary>
/// Firma adı tek zorunlu alan. Sunucuda da denetlenmesi gerekiyor: arayüzdeki
/// "required" yalnızca tarayıcıyı kullananları bağlar, adı boş bir firma ise
/// listede tıklanamaz bir satır olarak kalıyordu.
/// </summary>
public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Firma adı zorunludur.")
            .MinimumLength(3).WithMessage("Firma adı en az 3 karakter olmalıdır.")
            .MaximumLength(200).WithMessage("Firma adı en fazla 200 karakter olabilir.");
    }
}

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Firma adı zorunludur.")
            .MinimumLength(3).WithMessage("Firma adı en az 3 karakter olmalıdır.")
            .MaximumLength(200).WithMessage("Firma adı en fazla 200 karakter olabilir.");
    }
}
