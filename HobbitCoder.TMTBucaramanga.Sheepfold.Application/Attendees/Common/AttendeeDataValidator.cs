using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Attendees.Common;

public class AttendeeDataValidator : AbstractValidator<IAttendeeData>
{
    public AttendeeDataValidator(TimeProvider timeProvider)
    {
        RuleFor(data => data.FirstName).NotEmpty().WithMessage("FirstName is required").MaximumLength(100);
        RuleFor(data => data.LastName).NotEmpty().WithMessage("LastName is required").MaximumLength(100);
        RuleFor(data => data.Gender).IsInEnum();
        RuleFor(data => data.PhoneNumber).MaximumLength(30);
        RuleFor(data => data.Instagram).MaximumLength(60);
        RuleFor(data => data.EducationalStage).IsInEnum();
        RuleFor(data => data.BirthDate)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("BirthDate cannot be in the future");
    }
}
