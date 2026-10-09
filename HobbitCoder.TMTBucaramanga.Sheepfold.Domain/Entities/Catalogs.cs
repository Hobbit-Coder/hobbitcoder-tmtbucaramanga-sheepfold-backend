using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>
/// Valor de una lista administrable (§27). El nombre es único dentro de su lista. Retirarlo lo desactiva: deja
/// de ofrecerse en los formularios, pero los registros históricos que lo usan lo siguen mostrando.
/// </summary>
public abstract class CatalogItem : AuditableEntity
{
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Tipo de evento (§15).</summary>
public class EventType : CatalogItem
{
}

/// <summary>Tema de seguimiento pastoral (§21).</summary>
public class FollowUpTopic : CatalogItem
{
}

/// <summary>Curso ofrecido por la organización (§10).</summary>
public class Course : CatalogItem
{
}

/// <summary>Generación (§8). Su perfil determina el contacto exigido y la etapa educativa esperada.</summary>
public class Generation : CatalogItem
{
    public required GenerationProfile Profile { get; set; }

    /// <summary>Orden de presentación en listas y filtros.</summary>
    public int SortOrder { get; set; }

    /// <summary>§9: PULSO exige tutor legal; ECO, LEGADO y las demás, contacto de emergencia.</summary>
    public ContactKind RequiredContactKind =>
        Profile == GenerationProfile.Pulso ? ContactKind.LegalGuardian : ContactKind.EmergencyContact;

    /// <summary>
    /// Decisión P03: indica si la etapa educativa corresponde al perfil de la generación (§8). Sin perfil de la
    /// fuente no hay etapa esperada y no se advierte.
    /// </summary>
    public bool MatchesStage(EducationalStage stage) => Profile switch
    {
        GenerationProfile.Pulso => stage is >= EducationalStage.Grade6 and <= EducationalStage.Grade8,
        GenerationProfile.Eco => stage is >= EducationalStage.Grade9 and <= EducationalStage.Grade12,
        GenerationProfile.Legado1To5 => stage is >= EducationalStage.Semester1 and <= EducationalStage.Semester5,
        GenerationProfile.Legado6To10 => stage is >= EducationalStage.Semester6 and <= EducationalStage.Semester10 or EducationalStage.FirstJob,
        _ => true
    };
}
