using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings;

/// <summary>
/// Parámetros de indicadores, alertas y avisos (§27, HU-30, decisión P04). Un valor nulo es "No configurado":
/// el indicador muestra conteos pero no señala personas, y el aviso programado no se genera.
/// </summary>
public class OperationalSettingsDto
{
    public int? LowAttendanceThresholdPercent { get; set; }
    public int? LowAttendanceWindowDays { get; set; }
    public int? FrequentLatenessThreshold { get; set; }
    public int? FrequentLatenessWindowDays { get; set; }
    public int? UpcomingBirthdaysDays { get; set; }
    public int? UpcomingEventNoticeHours { get; set; }
    public int? PendingFollowUpReminderDays { get; set; }
}

public sealed record NotificationOptionsDto(
    IReadOnlyDictionary<NotificationChannel, bool> Channels,
    IReadOnlyDictionary<NotificationKind, bool> Kinds);

/// <summary>La consultan el administrador y el director ("solo consulta" para el director, §27).</summary>
public class GetOperationalSettingsQuery : IRequest<OperationalSettingsDto>
{
}

public class UpdateOperationalSettingsCommand : OperationalSettingsDto, IRequest
{
}

public class GetNotificationOptionsQuery : IRequest<NotificationOptionsDto>
{
}

/// <summary>
/// El administrador habilita canales y novedades. WhatsApp solo transporta notificaciones definidas; esta
/// configuración no habilita mensajes libres ni destinatarios elegidos a mano.
/// </summary>
public class UpdateNotificationOptionsCommand : IRequest
{
    public Dictionary<NotificationChannel, bool> Channels { get; set; } = [];
    public Dictionary<NotificationKind, bool> Kinds { get; set; } = [];
}

public class SettingsHandlers :
    IRequestHandler<GetOperationalSettingsQuery, OperationalSettingsDto>,
    IRequestHandler<UpdateOperationalSettingsCommand, Unit>,
    IRequestHandler<GetNotificationOptionsQuery, NotificationOptionsDto>,
    IRequestHandler<UpdateNotificationOptionsCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public SettingsHandlers(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<OperationalSettingsDto> Handle(GetOperationalSettingsQuery request, CancellationToken cancellationToken = default)
    {
        EnsureAdminOrDirector();

        return ToDto(await LoadSettingsAsync(cancellationToken));
    }

    public async Task<Unit> Handle(UpdateOperationalSettingsCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();

        var settings = await LoadSettingsAsync(cancellationToken);
        var before = ToDto(settings);

        settings.LowAttendanceThresholdPercent = request.LowAttendanceThresholdPercent;
        settings.LowAttendanceWindowDays = request.LowAttendanceWindowDays;
        settings.FrequentLatenessThreshold = request.FrequentLatenessThreshold;
        settings.FrequentLatenessWindowDays = request.FrequentLatenessWindowDays;
        settings.UpcomingBirthdaysDays = request.UpcomingBirthdaysDays;
        settings.UpcomingEventNoticeHours = request.UpcomingEventNoticeHours;
        settings.PendingFollowUpReminderDays = request.PendingFollowUpReminderDays;

        _audit.Record(AuditModule.Configuration, AuditActions.Updated, settings.Id, "Indicadores y alertas", before, ToDto(settings));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    public async Task<NotificationOptionsDto> Handle(GetNotificationOptionsQuery request, CancellationToken cancellationToken = default)
    {
        EnsureAdminOrDirector();

        return await LoadOptionsAsync(cancellationToken);
    }

    public async Task<Unit> Handle(UpdateNotificationOptionsCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();

        var before = await LoadOptionsAsync(cancellationToken);
        var channels = await _dbContext.NotificationChannelSettings.ToListAsync(cancellationToken);
        var kinds = await _dbContext.NotificationKindSettings.ToListAsync(cancellationToken);

        foreach (var channel in channels.Where(channel => request.Channels.ContainsKey(channel.Channel)))
        {
            channel.IsEnabled = request.Channels[channel.Channel];
        }

        foreach (var kind in kinds.Where(kind => request.Kinds.ContainsKey(kind.Kind)))
        {
            kind.IsEnabled = request.Kinds[kind.Kind];
        }

        _audit.Record(AuditModule.Configuration, AuditActions.Updated, null, "Opciones de notificación", before,
            new NotificationOptionsDto(channels.ToDictionary(item => item.Channel, item => item.IsEnabled), kinds.ToDictionary(item => item.Kind, item => item.IsEnabled)));
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private void EnsureAdminOrDirector()
    {
        if (!_access.IsAdmin && !_access.IsDirector)
        {
            throw new ForbiddenAccessException();
        }
    }

    private async Task<OperationalSettings> LoadSettingsAsync(CancellationToken cancellationToken) =>
        await _dbContext.OperationalSettings.FirstOrDefaultAsync(settings => settings.Id == OperationalSettings.SingletonId, cancellationToken)
        ?? throw new NotFoundException(nameof(OperationalSettings), OperationalSettings.SingletonId);

    private async Task<NotificationOptionsDto> LoadOptionsAsync(CancellationToken cancellationToken) =>
        new(await _dbContext.NotificationChannelSettings.ToDictionaryAsync(item => item.Channel, item => item.IsEnabled, cancellationToken),
            await _dbContext.NotificationKindSettings.ToDictionaryAsync(item => item.Kind, item => item.IsEnabled, cancellationToken));

    private static OperationalSettingsDto ToDto(OperationalSettings settings) => new()
    {
        LowAttendanceThresholdPercent = settings.LowAttendanceThresholdPercent,
        LowAttendanceWindowDays = settings.LowAttendanceWindowDays,
        FrequentLatenessThreshold = settings.FrequentLatenessThreshold,
        FrequentLatenessWindowDays = settings.FrequentLatenessWindowDays,
        UpcomingBirthdaysDays = settings.UpcomingBirthdaysDays,
        UpcomingEventNoticeHours = settings.UpcomingEventNoticeHours,
        PendingFollowUpReminderDays = settings.PendingFollowUpReminderDays
    };
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
