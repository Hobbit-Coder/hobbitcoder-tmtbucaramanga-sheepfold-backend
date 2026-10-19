using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Commands;

/// <summary>
/// Avisos programados: eventos próximos, cumpleaños próximos y recordatorio de seguimientos pendientes. Solo
/// se generan los que tienen su parámetro configurado y cada aviso se genera una sola vez.
/// Lo ejecuta una tarea en segundo plano.
/// </summary>
public class GenerateScheduledNotificationsCommand : IRequest
{
}

public class GenerateScheduledNotificationsCommandHandler : IRequestHandler<GenerateScheduledNotificationsCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly NotificationDispatcher _dispatcher;
    private readonly EventAudienceResolver _audience;
    private readonly IUserDirectory _userDirectory;
    private readonly TimeProvider _timeProvider;

    public GenerateScheduledNotificationsCommandHandler(
        IApplicationDbContext dbContext,
        NotificationDispatcher dispatcher,
        EventAudienceResolver audience,
        IUserDirectory userDirectory,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
        _audience = audience;
        _userDirectory = userDirectory;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> Handle(GenerateScheduledNotificationsCommand request, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.OperationalSettings.AsNoTracking().FirstAsync(cancellationToken);

        if (settings.UpcomingEventNoticeHours is { } hours)
        {
            await UpcomingEventsAsync(hours, cancellationToken);
        }

        if (settings.UpcomingBirthdaysDays is { } days)
        {
            await BirthdaysAsync(days, cancellationToken);
        }

        if (settings.PendingFollowUpReminderDays is { } reminderDays)
        {
            await PendingFollowUpsAsync(reminderDays, cancellationToken);
        }

        return Unit.Value;
    }

    private const int BirthdayRepeatGuardDays = 300;

    private async Task UpcomingEventsAsync(int hours, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var until = now.AddHours(hours);
        var events = await _dbContext.Events
            .Include(@event => @event.Managers).Include(@event => @event.Generations).Include(@event => @event.Groups)
            .Where(@event => @event.Status == EventStatus.Published && @event.StartsAt > now && @event.StartsAt <= until)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
        {
            return;
        }

        var directory = await _userDirectory.GetUsersAsync(cancellationToken);

        foreach (var @event in events)
        {
            var alreadyNotified = await _dbContext.Notifications
                .Where(notification => notification.Kind == NotificationKind.UpcomingEvent && notification.EventId == @event.Id)
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken);

            var viewers = await _audience.ViewerUserIdsAsync(@event, directory, cancellationToken);
            var start = LocalCalendar.ToLocal(@event.StartsAt);

            await _dispatcher.NotifyAsync(viewers.Except(alreadyNotified), NotificationKind.UpcomingEvent, $"Próximo evento: {@event.Title}",
                $"{@event.Title} empieza el {start:dd/MM/yyyy} a las {start:HH:mm} en {@event.Location}.", eventId: @event.Id, cancellationToken: cancellationToken);
        }
    }

    /// <summary>Avisa al equipo del grupo actual de cada asistente que cumple años en los próximos días.</summary>
    private async Task BirthdaysAsync(int days, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(_timeProvider);
        var guardSince = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-BirthdayRepeatGuardDays);

        var attendees = await _dbContext.Attendees
            .Where(attendee => attendee.IsActive && attendee.BirthDate != null)
            .Select(attendee => new
            {
                attendee.Id,
                attendee.FirstName,
                attendee.LastName,
                attendee.BirthDate,
                GroupId = attendee.Memberships.Where(membership => membership.EndedAt == null).Select(membership => (Guid?)membership.GroupId).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        foreach (var attendee in attendees.Where(item => item.GroupId is not null))
        {
            var birthday = LocalCalendar.NextBirthday(attendee.BirthDate!.Value, today);

            if (birthday.DayNumber - today.DayNumber > days)
            {
                continue;
            }

            var alreadyNotified = await _dbContext.Notifications
                .Where(notification => notification.Kind == NotificationKind.Birthday && notification.AttendeeId == attendee.Id && notification.CreatedAt >= guardSince)
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken);

            var team = await _dbContext.GroupAssignments.Where(assignment => assignment.GroupId == attendee.GroupId)
                .Select(assignment => assignment.UserId).ToListAsync(cancellationToken);

            await _dispatcher.NotifyAsync(team.Except(alreadyNotified), NotificationKind.Birthday, "Cumpleaños próximo",
                $"{attendee.FirstName} {attendee.LastName} cumple años el {birthday:dd/MM}.", attendeeId: attendee.Id, cancellationToken: cancellationToken);
        }
    }

    /// <summary>Recuerda a cada autor cuántos seguimientos tiene pendientes, cada <paramref name="days"/> días. Sin nombres.</summary>
    private async Task PendingFollowUpsAsync(int days, CancellationToken cancellationToken)
    {
        var since = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-days);
        var pendingByAuthor = await _dbContext.FollowUps
            .Where(followUp => followUp.Status == FollowUpStatus.Pending)
            .GroupBy(followUp => followUp.CreatedBy)
            .Select(group => new { Author = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        foreach (var pending in pendingByAuthor)
        {
            if (!Guid.TryParse(pending.Author, out var authorId)
                || await _dbContext.Notifications.AnyAsync(notification => notification.Kind == NotificationKind.PendingFollowUp
                    && notification.RecipientUserId == authorId && notification.CreatedAt >= since, cancellationToken))
            {
                continue;
            }

            await _dispatcher.NotifyAsync([authorId], NotificationKind.PendingFollowUp, "Seguimientos pendientes",
                pending.Count == 1 ? "Tienes 1 seguimiento pendiente." : $"Tienes {pending.Count} seguimientos pendientes.", cancellationToken: cancellationToken);
        }
    }
}
