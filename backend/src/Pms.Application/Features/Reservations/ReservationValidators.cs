using FluentValidation;

namespace Pms.Application.Features.Reservations;

public class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.GuestId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.Adults).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
    }
}

public class UpdateReservationValidator : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationValidator()
    {
        RuleFor(x => x.GuestId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.Adults).GreaterThanOrEqualTo(1);
    }
}

public class AvailabilityValidator : AbstractValidator<AvailabilityRequest>
{
    public AvailabilityValidator()
    {
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out must be after check-in.");
    }
}
