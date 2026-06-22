using MediatR;
using Microsoft.Extensions.Logging;

namespace Tensorroot.Gov.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Behavior do pipeline que registra (log estruturado) o início, o término e os erros
/// de cada request processado pelo MediatR.
/// </summary>
/// <typeparam name="TRequest">Tipo do request.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
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

        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Processando {RequestName}", requestName);

        try
        {
            var response = await next().ConfigureAwait(false);
            logger.LogInformation("Processado com sucesso {RequestName}", requestName);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao processar {RequestName}", requestName);
            throw;
        }
    }
}
