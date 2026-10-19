using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

public class FollowUpContentValidator : AbstractValidator<FollowUpContent>
{
    public FollowUpContentValidator(TimeProvider timeProvider)
    {
        RuleFor(content => content.MeetingDate)
            .LessThanOrEqualTo(_ => LocalCalendar.Today(timeProvider)).WithMessage("The meeting date cannot be in the future.");
        RuleFor(content => content.Location).NotEmpty().WithMessage("Location is required").MaximumLength(150);
        RuleFor(content => content.TopicId).NotEmpty().WithMessage("Select a topic.");
        RuleFor(content => content.Description).NotEmpty().WithMessage("Description is required").MaximumLength(4000);
        RuleFor(content => content.Commitments).NotEmpty().WithMessage("Commitments are required").MaximumLength(4000);
        RuleFor(content => content.Observations).MaximumLength(4000);
    }
}
