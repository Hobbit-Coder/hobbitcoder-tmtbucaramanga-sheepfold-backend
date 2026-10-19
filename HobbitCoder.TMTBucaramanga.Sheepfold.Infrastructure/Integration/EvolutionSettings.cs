namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Integration;

/// <summary>Evolution API autoalojado (v2).</summary>
public class EvolutionSettings
{
    /// <summary>URL base de Evolution API, por ejemplo <c>https://evolution.hobbitcoder.com</c>.</summary>
    public string? Url { get; set; }

    /// <summary>Token de la instancia (o la clave global) para la cabecera <c>apikey</c>. Solo por secretos.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Nombre de la instancia vinculada al número.</summary>
    public string? Instance { get; set; }

    /// <summary>Pausa entre notificaciones para no enviar en ráfaga (reduce el riesgo de bloqueo del número).</summary>
    public int SecondsBetweenMessages { get; set; } = 3;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Instance);
}
