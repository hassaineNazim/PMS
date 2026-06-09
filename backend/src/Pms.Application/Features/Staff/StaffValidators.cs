using FluentValidation;

namespace Pms.Application.Features.Staff;

public class CreateStaffValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CreateScheduleValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleValidator()
    {
        RuleFor(x => x.StaffId).NotEmpty();
        RuleFor(x => x.ShiftEnd).NotEqual(x => x.ShiftStart)
            .WithMessage("Shift end must differ from shift start.");
    }
}
