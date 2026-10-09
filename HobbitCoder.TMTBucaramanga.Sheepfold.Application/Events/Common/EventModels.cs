using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Events.Common;

public sealed record NamedRef(Guid Id, string Name);

public sealed record AudienceGroupRef(Guid Id, string Name, int Year, Guid GenerationId, string GenerationName);

public sealed record EventSummaryResponse(
    Guid Id,
    string Title,
    NamedRef EventType,
    DateTime StartsAt,
    DateTime EndsAt,
    string Location,
    EventAudience Audience,
    IReadOnlyList<NamedRef> Generations,
    IReadOnlyList<AudienceGroupRef> Groups,
    EventStatus Status,
    bool RequiresAttendance);

public sealed record PersonRef(Guid Id, string FullName, UserRole? Role);

public sealed record FileResponse(Guid Id, string FileName, string ContentType, long SizeBytes, DateTime UploadedAt, string UploadedBy);

/// <param name="Edit">Administra el evento: creador, encargado o director (§16, §4.1).</param>
/// <param name="Attendance">Hay registro de asistencia disponible para el usuario (§19).</param>
public sealed record EventCapabilities(bool Edit, bool ChangeStatus, bool Retire, bool Attendance, bool Comment);

public sealed record EventDetailResponse(
    EventSummaryResponse Event,
    string Description,
    PersonRef Creator,
    IReadOnlyList<PersonRef> Managers,
    IReadOnlyList<FileResponse> Attachments,
    IReadOnlyList<EventStatus> AllowedTransitions,
    EventCapabilities Capabilities);

/// <summary>Públicos y opciones que el usuario puede elegir al crear o editar un evento (§16, §17).</summary>
public sealed record AudienceOptionsResponse(
    IReadOnlyList<EventAudience> Audiences,
    IReadOnlyList<NamedRef> Generations,
    IReadOnlyList<AudienceGroupRef> Groups,
    IReadOnlyList<PersonRef> Managers);

public sealed record CommentResponse(
    Guid Id,
    Guid? ParentCommentId,
    PersonRef Author,
    string Body,
    DateTime CreatedAt,
    IReadOnlyList<FileResponse> Attachments);

/// <summary>Archivo recibido en una petición, antes de guardarlo en el almacenamiento.</summary>
public sealed record UploadedFile(string FileName, string ContentType, long SizeBytes, Func<Stream> OpenReadStream);

/// <summary>Contenido de un archivo para descargar.</summary>
public sealed record FileContent(Stream Content, string FileName, string ContentType);

public static class EventTransitions
{
    /// <summary>
    /// Cambios de estado admitidos (§15). Finalizar no cierra la asistencia (§20); cancelar conserva datos y
    /// conversación. Un cancelado o finalizado puede volver a publicarse.
    /// </summary>
    public static IReadOnlyList<EventStatus> From(EventStatus status) => status switch
    {
        EventStatus.Draft => [EventStatus.Published, EventStatus.Cancelled],
        EventStatus.Published => [EventStatus.Cancelled, EventStatus.Finished],
        EventStatus.Cancelled => [EventStatus.Published],
        _ => [EventStatus.Published]
    };
}
