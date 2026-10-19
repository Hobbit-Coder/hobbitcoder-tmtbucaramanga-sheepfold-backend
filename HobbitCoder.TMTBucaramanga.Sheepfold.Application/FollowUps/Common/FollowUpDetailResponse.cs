namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.FollowUps.Common;

public sealed class FollowUpDetailResponse
{
    public required FollowUpListItem FollowUp { get; set; }

    /// <summary>Contenido de la reunión; <c>null</c> si está pendiente o si es confidencial y el usuario no es su autor ni director.</summary>
    public FollowUpContentResponse? Content { get; set; }

    /// <summary>El contenido existe pero no se muestra (confidencial).</summary>
    public required bool ContentProtected { get; set; }
}
