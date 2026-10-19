using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>Proveedor que entrega una notificación por WhatsApp (Evolution o Meta, según <see cref="MessagingSettings.Provider"/>).</summary>
public interface IWhatsAppClient
{
    bool IsConfigured { get; }

    /// <summary>Pausa que el despachador espera después de cada envío.</summary>
    TimeSpan PauseBetweenMessages { get; }

    /// <summary>Envía la notificación; un rechazo o un fallo de red se registra y no se propaga.</summary>
    /// <returns><c>true</c> si el proveedor aceptó el mensaje.</returns>
    Task<bool> SendAsync(WhatsAppNotification notification, CancellationToken cancellationToken);
}
