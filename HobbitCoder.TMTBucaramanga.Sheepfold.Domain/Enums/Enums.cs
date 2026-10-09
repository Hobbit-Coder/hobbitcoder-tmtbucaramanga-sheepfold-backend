namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;

/// <summary>Rol de la cuenta en Identity (claim <c>role</c>). Sheepfold no guarda roles: los lee del token o del directorio.</summary>
public enum UserRole
{
    Admin = 1,
    Director = 2,
    Coordinator = 3,
    Leader = 4,
    Apprentice = 5
}

public enum Gender
{
    Female = 1,
    Male = 2
}

/// <summary>
/// Generación de referencia del §8. Determina el contacto exigido (§9), la etapa educativa esperada (P03) y la
/// identidad visual. Una generación creada en configuración sin perfil de la fuente usa <see cref="Other"/>.
/// </summary>
public enum GenerationProfile
{
    Pulso = 1,
    Eco = 2,
    Legado1To5 = 3,
    Legado6To10 = 4,
    Other = 99
}

/// <summary>Responsabilidad de un usuario en un grupo (§11). Coordinar no convierte en integrante.</summary>
public enum GroupResponsibility
{
    Coordinator = 1,
    Leader = 2,
    Apprentice = 3
}

/// <summary>Tutor legal (PULSO) o contacto de emergencia (ECO y LEGADO), §9.</summary>
public enum ContactKind
{
    LegalGuardian = 1,
    EmergencyContact = 2
}

/// <summary>Estado de un curso realizado (§10).</summary>
public enum CourseStatus
{
    InProgress = 1,
    Finished = 2
}

/// <summary>
/// Etapa educativa opcional del asistente (decisión P03): grado escolar, semestre universitario o primer
/// trabajo. Solo sirve para advertir cuando la generación no corresponde; nunca bloquea.
/// </summary>
public enum EducationalStage
{
    Grade6 = 6,
    Grade7 = 7,
    Grade8 = 8,
    Grade9 = 9,
    Grade10 = 10,
    Grade11 = 11,
    Grade12 = 12,
    Semester1 = 101,
    Semester2 = 102,
    Semester3 = 103,
    Semester4 = 104,
    Semester5 = 105,
    Semester6 = 106,
    Semester7 = 107,
    Semester8 = 108,
    Semester9 = 109,
    Semester10 = 110,
    FirstJob = 200
}

/// <summary>Estados del evento (§15).</summary>
public enum EventStatus
{
    Draft = 1,
    Published = 2,
    Cancelled = 3,
    Finished = 4
}

/// <summary>Público del evento (§17). "Por grupos" es el público <see cref="Groups"/>; los demás son generales.</summary>
public enum EventAudience
{
    AllMinistry = 1,
    DirectorsAndCoordinators = 2,
    DirectorsCoordinatorsAndLeaders = 3,
    PrimaryGroup = 4,
    PrimaryGroupAndServers = 5,
    Generations = 6,
    Groups = 7
}

/// <summary>Las dos marcas de asistencia (§19). Sin marca se interpreta como no asistió.</summary>
public enum AttendanceMark
{
    Attended = 1,
    Late = 2
}

public enum FollowUpStatus
{
    Pending = 1,
    Done = 2
}

/// <summary>Novedades que generan notificación (§23).</summary>
public enum NotificationKind
{
    UpcomingEvent = 1,
    EventChanged = 2,
    EventCancelled = 3,
    Birthday = 4,
    AttendeeAssigned = 5,
    PendingFollowUp = 6,
    EventResponsibility = 7,
    DiscussionActivity = 8
}

public enum NotificationChannel
{
    InApp = 1,
    WhatsApp = 2
}

/// <summary>Módulos de la auditoría ministerial (§25).</summary>
public enum AuditModule
{
    Attendees = 1,
    Groups = 2,
    Assignments = 3,
    Events = 4,
    Attendance = 5,
    FollowUps = 6,
    Courses = 7,
    Configuration = 8
}
