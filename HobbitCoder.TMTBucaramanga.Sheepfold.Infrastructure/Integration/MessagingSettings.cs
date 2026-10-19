namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>
/// WhatsApp para las notificaciones automáticas (sección <see cref="SectionName"/>). Mismo proveedor y número que los
/// códigos de seguridad de Identity. Sin las credenciales del <see cref="Provider"/> elegido solo hay avisos dentro
/// de la aplicación.
/// </summary>
public class MessagingSettings
{
    public const string SectionName = "Messaging";

    public MessagingProvider Provider { get; set; } = MessagingProvider.Evolution;

    public int TimeoutSeconds { get; set; } = 10;

    public EvolutionSettings Evolution { get; set; } = new();

    public MetaWhatsAppSettings Meta { get; set; } = new();

    public bool IsConfigured => Provider switch
    {
        MessagingProvider.Evolution => Evolution.IsConfigured,
        MessagingProvider.Meta => Meta.IsConfigured,
        _ => false
    };
}
