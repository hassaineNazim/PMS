using FluentValidation;

namespace Pms.Application.Features.Guests;

public class CreateGuestValidator : AbstractValidator<CreateGuestRequest>
{
    public CreateGuestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Language).NotEmpty().MaximumLength(5);
    }
}

public class UpdateGuestValidator : AbstractValidator<UpdateGuestRequest>
{
    public UpdateGuestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Language).NotEmpty().MaximumLength(5);
    }
}
