using FluentValidation.Results;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;

/// <summary>
/// Se lanza cuando un request no supera sus validadores de FluentValidation.
/// El manejador de excepciones de la API la traduce a un 400 con el detalle por campo.
/// </summary>
public class ValidationException : Exception
{
    private const string DefaultMessage = "One or more validation failures have occurred.";

    /// <summary>Errores agrupados por nombre de propiedad.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base(DefaultMessage)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base(DefaultMessage)
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    /// <summary>Error de validación de un solo campo.</summary>
    public static ValidationException For(string propertyName, string message) =>
        new([new ValidationFailure(propertyName, message)]);
}
