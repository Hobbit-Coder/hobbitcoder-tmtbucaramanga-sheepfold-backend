using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public class EventDataValidator<T> : AbstractValidator<T> where T : EventData
{
    public EventDataValidator()
    {
        RuleFor(command => command.Title).NotEmpty().WithMessage("Title is required").MaximumLength(150);
        RuleFor(command => command.EventTypeId).NotEmpty().WithMessage("Select an event type.");
        RuleFor(command => command.Location).NotEmpty().WithMessage("Location is required").MaximumLength(150);
        RuleFor(command => command.Description).NotEmpty().WithMessage("Description is required").MaximumLength(4000);
        RuleFor(command => command.Audience).IsInEnum();
        RuleFor(command => command.EndsAt).GreaterThanOrEqualTo(command => command.StartsAt).WithMessage("The event must end after it starts.");
        RuleFor(command => command.ManagerIds).NotEmpty().WithMessage("Assign at least one manager.");
        RuleFor(command => command.GenerationIds).NotEmpty().When(command => command.Audience == EventAudience.Generations)
            .WithMessage("Select at least one generation.");
        RuleFor(command => command.GroupIds).NotEmpty().When(command => command.Audience == EventAudience.Groups)
            .WithMessage("Select at least one group.");
    }
}
