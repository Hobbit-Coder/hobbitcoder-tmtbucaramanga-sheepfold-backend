namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;

/// <summary>
/// El usuario está autenticado pero su rol o alcance no le permite la acción. La API la traduce a un 403 con
/// un mensaje genérico: nunca revela datos del recurso que no puede consultar.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("You do not have permission to perform this action.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Un servicio externo necesario (Identity, almacenamiento S3, pasarela de mensajería) no está configurado o no
/// respondió. La API la traduce a un 503 sin detalle técnico.
/// </summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException()
        : base("An external service is not available.")
    {
    }

    public ExternalServiceException(string message)
        : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
