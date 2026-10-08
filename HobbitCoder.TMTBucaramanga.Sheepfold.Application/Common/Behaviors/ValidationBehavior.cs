using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using FluentValidation;
using ValidationException = HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions.ValidationException;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Behaviors;

/// <summary>
/// Ejecuta los validadores de FluentValidation registrados para el request antes de
/// llegar al handler. Sin este behavior los validadores del proyecto quedan registrados
/// pero nunca se invocan.
/// </summary>
/// <remarks>
/// Los validadores se ejecutan en paralelo y cada uno recibe su propio <see cref="ValidationContext{T}"/>:
/// si compartieran uno, cada resultado acumularía los fallos de los demás y los errores se duplicarían.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next);

        return _validators.Any()
            ? ValidateAndContinueAsync(request, next, cancellationToken)
            : next(cancellationToken);
    }

    private async Task<TResponse> ValidateAndContinueAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(_validators.Select(validator =>
            validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)));

        var failures = results
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
