using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Notifications.Common;

/// <summary>
/// Genera las notificaciones de una novedad para destinatarios ya autorizados: avisos dentro de la
/// aplicación y, si el canal y la novedad están habilitados en la configuración, el envío automático por
/// WhatsApp al teléfono registrado en Identity. Se llama después de guardar el cambio que la origina; un fallo
/// al notificar se registra y no deshace ese cambio.
/// </summary>
public sealed class NotificationDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserDirectory _userDirectory;
    private readonly IWhatsAppNotificationSender _whatsAppSender;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IApplicationDbContext dbContext,
        IUserDirectory userDirectory,
        IWhatsAppNotificationSender whatsAppSender,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<NotificationDispatcher> logger)
    {
        _dbContext = dbContext;
        _userDirectory = userDirectory;
        _whatsAppSender = whatsAppSender;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Notifica a <paramref name="recipientUserIds"/> excepto a quien hizo el cambio y a cuentas inactivas o sin
    /// rol ministerial (el administrador no recibe avisos ministeriales).
    /// </summary>
    /// <returns>Los usuarios que recibieron el aviso dentro de la aplicación.</returns>
    public async Task<IReadOnlyCollection<Guid>> NotifyAsync(
        IEnumerable<Guid> recipientUserIds,
        NotificationKind kind,
        string title,
        string message,
        Guid? eventId = null,
        Guid? attendeeId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var kindEnabled = await _dbContext.NotificationKindSettings
                .Where(setting => setting.Kind == kind)
                .Select(setting => setting.IsEnabled)
                .FirstOrDefaultAsync(cancellationToken);

            if (!kindEnabled)
            {
                return [];
            }

            var channels = await _dbContext.NotificationChannelSettings
                .ToDictionaryAsync(setting => setting.Channel, setting => setting.IsEnabled, cancellationToken);
            var directory = await _userDirectory.GetUsersAsync(cancellationToken);
            var actor = _currentUserService.UserId;

            var recipients = recipientUserIds
                .Distinct()
                .Where(userId => userId.ToString() != actor)
                .Select(userId => directory.GetValueOrDefault(userId))
                .OfType<DirectoryUser>()
                .Where(user => user.IsActive && user.IsMinistry)
                .ToList();

            if (recipients.Count == 0)
            {
                return [];
            }

            if (channels.GetValueOrDefault(NotificationChannel.InApp))
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;

                _dbContext.Notifications.AddRange(recipients.Select(user => new Notification
                {
                    RecipientUserId = user.Id,
                    Kind = kind,
                    Title = Truncate(title, 200),
                    Message = Truncate(message, 500),
                    CreatedAt = now,
                    EventId = eventId,
                    AttendeeId = attendeeId
                }));

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            if (channels.GetValueOrDefault(NotificationChannel.WhatsApp))
            {
                foreach (var user in recipients.Where(user => !string.IsNullOrWhiteSpace(user.PhoneNumber)))
                {
                    _whatsAppSender.Enqueue(new WhatsAppNotification { PhoneNumber = user.PhoneNumber!, Title = title, Message = message });
                }
            }

            return recipients.Select(user => user.Id).ToList();
        }
        catch (Exception exception) when (exception is ExternalServiceException or DbUpdateException)
        {
            _logger.LogError(exception, "No se pudo generar la notificación {Kind}.", kind);
            return [];
        }
    }

    private static string Truncate(string value, int maxLength) => value.Length > maxLength ? value[..maxLength] : value;
}
