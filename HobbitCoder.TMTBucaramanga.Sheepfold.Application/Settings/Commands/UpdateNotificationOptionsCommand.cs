using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Security;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Commands;

/// <summary>
/// El administrador habilita canales y novedades. WhatsApp solo transporta notificaciones definidas; esta
/// configuración no habilita mensajes libres ni destinatarios elegidos a mano.
/// </summary>
public class UpdateNotificationOptionsCommand : IRequest
{
    public Dictionary<NotificationChannel, bool> Channels { get; set; } = [];
    public Dictionary<NotificationKind, bool> Kinds { get; set; } = [];
}

public class UpdateNotificationOptionsCommandHandler : IRequestHandler<UpdateNotificationOptionsCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly MinistryAccess _access;
    private readonly AuditTrail _audit;

    public UpdateNotificationOptionsCommandHandler(IApplicationDbContext dbContext, MinistryAccess access, AuditTrail audit)
    {
        _dbContext = dbContext;
        _access = access;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateNotificationOptionsCommand request, CancellationToken cancellationToken = default)
    {
        _access.EnsureAdmin();

        var before = await SettingsHelpers.LoadOptionsAsync(_dbContext, cancellationToken);
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
            new NotificationOptionsDto { Channels = channels.ToDictionary(item => item.Channel, item => item.IsEnabled), Kinds = kinds.ToDictionary(item => item.Kind, item => item.IsEnabled) });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
