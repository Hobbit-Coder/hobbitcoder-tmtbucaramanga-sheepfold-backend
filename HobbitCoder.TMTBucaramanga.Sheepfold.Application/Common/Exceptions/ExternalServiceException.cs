namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;

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
