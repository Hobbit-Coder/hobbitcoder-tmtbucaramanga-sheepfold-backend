using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Commands;

public class UpdateOperationalSettingsCommand : OperationalSettingsDto, IRequest
{
}

public class UpdateOperationalSettingsCommandHandler : IRequestHandler<UpdateOperationalSettingsCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateOperationalSettingsCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateOperationalSettingsCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();

        var settings = await SettingsHelpers.LoadSettingsAsync(_dbContext, cancellationToken);
        var before = SettingsHelpers.ToDto(settings);

        settings.LowAttendanceThresholdPercent = request.LowAttendanceThresholdPercent;
        settings.LowAttendanceWindowDays = request.LowAttendanceWindowDays;
        settings.FrequentLatenessThreshold = request.FrequentLatenessThreshold;
        settings.FrequentLatenessWindowDays = request.FrequentLatenessWindowDays;
        settings.UpcomingBirthdaysDays = request.UpcomingBirthdaysDays;
        settings.UpcomingEventNoticeHours = request.UpcomingEventNoticeHours;
        settings.PendingFollowUpReminderDays = request.PendingFollowUpReminderDays;

        _audit.Record(AuditModule.Configuration, AuditActions.Updated, settings.Id, "Indicadores y alertas", before, SettingsHelpers.ToDto(settings));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

/// <summary>Rangos de cordura para que un parámetro no quede inutilizable; no son umbrales de negocio.</summary>
public class UpdateOperationalSettingsCommandValidator : AbstractValidator<UpdateOperationalSettingsCommand>
{
    public UpdateOperationalSettingsCommandValidator()
    {
        RuleFor(command => command.LowAttendanceThresholdPercent).InclusiveBetween(1, 100);
        RuleFor(command => command.LowAttendanceWindowDays).InclusiveBetween(1, 366);
        RuleFor(command => command.FrequentLatenessThreshold).InclusiveBetween(1, 366);
        RuleFor(command => command.FrequentLatenessWindowDays).InclusiveBetween(1, 366);
        RuleFor(command => command.UpcomingBirthdaysDays).InclusiveBetween(0, 366);
        RuleFor(command => command.UpcomingEventNoticeHours).InclusiveBetween(1, 24 * 30);
        RuleFor(command => command.PendingFollowUpReminderDays).InclusiveBetween(1, 366);

        RuleFor(command => command.LowAttendanceWindowDays).NotNull().When(command => command.LowAttendanceThresholdPercent is not null)
            .WithMessage("LowAttendanceWindowDays is required with a threshold");
        RuleFor(command => command.FrequentLatenessWindowDays).NotNull().When(command => command.FrequentLatenessThreshold is not null)
            .WithMessage("FrequentLatenessWindowDays is required with a threshold");
    }
}
