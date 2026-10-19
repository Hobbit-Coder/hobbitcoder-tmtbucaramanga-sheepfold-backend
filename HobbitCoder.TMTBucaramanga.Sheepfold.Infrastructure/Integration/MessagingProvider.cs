namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>Proveedor de WhatsApp para las notificaciones automáticas.</summary>
public enum MessagingProvider
{
    /// <summary>Evolution API autoalojado: texto libre desde un número vinculado por QR.</summary>
    Evolution = 1,

    /// <summary>WhatsApp Cloud API de Meta: plantilla de utilidad aprobada.</summary>
    Meta = 2
}
