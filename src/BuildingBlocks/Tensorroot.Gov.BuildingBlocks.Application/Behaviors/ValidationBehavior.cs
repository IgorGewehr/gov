using FluentValidation;
using MediatR;

namespace Tensorroot.Gov.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Behavior do pipeline que valida o request com todos os <see cref="IValidator{T}"/>
/// registrados antes do handler. Lança <see cref="ValidationException"/> ao encontrar falhas.
/// </summary>
/// <typeparam name="TRequest">Tipo do request.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var validatorsList = validators.ToList();
        if (validatorsList.Count > 0)
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                validatorsList.Select(validator => validator.ValidateAsync(context, cancellationToken)))
                .ConfigureAwait(false);

            var failures = results
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }
}
