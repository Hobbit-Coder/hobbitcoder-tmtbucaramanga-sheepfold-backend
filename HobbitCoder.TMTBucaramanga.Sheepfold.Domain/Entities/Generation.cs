using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

/// <summary>Generación. Su perfil determina el contacto exigido y la etapa educativa esperada.</summary>
public class Generation : CatalogItem
{
    public required GenerationProfile Profile { get; set; }

    /// <summary>Orden de presentación en listas y filtros.</summary>
    public int SortOrder { get; set; }

    /// <summary>PULSO exige tutor legal; ECO, LEGADO y las demás, contacto de emergencia.</summary>
    public ContactKind RequiredContactKind =>
        Profile == GenerationProfile.Pulso ? ContactKind.LegalGuardian : ContactKind.EmergencyContact;

    /// <summary>
    /// Indica si la etapa educativa corresponde al perfil de la generación. Sin perfil de la
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
