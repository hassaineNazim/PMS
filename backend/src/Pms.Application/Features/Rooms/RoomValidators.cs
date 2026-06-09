using FluentValidation;

namespace Pms.Application.Features.Rooms;

public class CreateRoomValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 20);
        RuleFor(x => x.PricePerNight).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Floor).GreaterThanOrEqualTo(-5).When(x => x.Floor.HasValue);
    }
}

public class UpdateRoomValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 20);
        RuleFor(x => x.PricePerNight).GreaterThanOrEqualTo(0);
    }
}
