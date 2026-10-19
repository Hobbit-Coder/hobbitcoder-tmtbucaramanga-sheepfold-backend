namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Settings.Common;

/// <summary>Nombre del catálogo en la auditoría, en español como el resto de la interfaz.</summary>
public static class CatalogKindLabels
{
    public static string Of(CatalogKind kind) => kind switch
    {
        CatalogKind.Generations => "Generación",
        CatalogKind.EventTypes => "Tipo de evento",
        CatalogKind.FollowUpTopics => "Tema de seguimiento",
        CatalogKind.Courses => "Curso",
        _ => kind.ToString()
    };
}
