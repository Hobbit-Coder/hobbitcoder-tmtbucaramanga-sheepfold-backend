namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Audit;

/// <summary>Acciones de la auditoría ministerial. Son códigos estables; la interfaz los traduce.</summary>
public static class AuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Retired = "Retired";
    public const string Restored = "Restored";
    public const string PermanentlyDeleted = "PermanentlyDeleted";
    public const string Activated = "Activated";
    public const string Deactivated = "Deactivated";
    public const string Transferred = "Transferred";
    public const string TeamChanged = "TeamChanged";
    public const string StatusChanged = "StatusChanged";
    public const string AttendanceMarked = "AttendanceMarked";
    public const string AttendanceCleared = "AttendanceCleared";
    public const string Completed = "Completed";
    public const string ConfidentialityChanged = "ConfidentialityChanged";
    public const string SensitiveRead = "SensitiveRead";
    public const string ContactAdded = "ContactAdded";
    public const string ContactUpdated = "ContactUpdated";
    public const string ContactRemoved = "ContactRemoved";
    public const string CourseAdded = "CourseAdded";
    public const string CourseUpdated = "CourseUpdated";
    public const string CourseRemoved = "CourseRemoved";
    public const string AttachmentAdded = "AttachmentAdded";
    public const string AttachmentRemoved = "AttachmentRemoved";

    /// <summary>Acción de un cambio que puede activar o desactivar el registro.</summary>
    public static string ForActivation(bool wasActive, bool isActive, string activated = Activated, string deactivated = Deactivated)
    {
        if (wasActive == isActive)
        {
            return Updated;
        }

        return isActive ? activated : deactivated;
    }
}
