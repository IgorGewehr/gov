using MediatR;
using Microsoft.Extensions.Logging;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;

namespace Tensorroot.Gov.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Behavior transversal (LG-2) que gera a TRILHA DE ACESSO de toda query marcada
/// <see cref="ISensivelLgpd"/>: apos o handler concluir com sucesso, sela
/// <c>{Tenant, UserId, Ip, Entidade, EntityId, BaseLegal, Ts}</c> numa trilha append-only via
/// <see cref="IRegistroAcessoSensivel"/>. Elimina a leitura de dado sensivel SEM RASTRO
/// (Saude/Assistencia), exigencia do art. 37 LGPD e da CLAUDE.md §6.
/// <para>
/// So registra quando a leitura efetivamente OCORRE (apos <c>next()</c>): leituras que falham na
/// validacao/autorizacao nao geram acesso. Requests que NAO implementam <see cref="ISensivelLgpd"/>
/// passam direto (custo zero).
/// </para>
/// </summary>
/// <typeparam name="TRequest">Tipo do request.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta.</typeparam>
public sealed class TrilhaAcessoSensivelBehavior<TRequest, TResponse>(
    IRegistroAcessoSensivel registro,
    ILogger<TrilhaAcessoSensivelBehavior<TRequest, TResponse>> logger)
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

        if (request is not ISensivelLgpd sensivel)
        {
            // Requests que NAO sao leitura sensivel passam direto (custo zero).
            return await next().ConfigureAwait(false);
        }

        // LG-A2 (deny-by-default da base legal): a hipotese legal aplicada DEVE pertencer ao
        // conjunto fechado de bases legais aplicaveis ao recurso. Verifica-se ANTES de projetar
        // qualquer dado — sem base legal aplicavel, o dado sensivel nunca chega a ser lido. A
        // tentativa e selada na trilha (negativa) para accountability perante ANPD/TCE.
        if (!sensivel.BasesLegaisAplicaveis.Contains(sensivel.BaseLegal))
        {
            logger.LogWarning(
                "Acesso sensivel NEGADO (LG-A2): base legal {BaseLegal} nao aplicavel ao recurso {Entidade}.",
                sensivel.BaseLegal,
                sensivel.EntidadeSensivel);

            await registro.RegistrarNegacaoAsync(
                sensivel.EntidadeSensivel,
                sensivel.EntidadeId,
                sensivel.BaseLegal,
                cancellationToken).ConfigureAwait(false);

            throw new BaseLegalLgpdNaoAplicavelException(sensivel.EntidadeSensivel, sensivel.BaseLegal);
        }

        var response = await next().ConfigureAwait(false);

        // A trilha de acesso e PARTE da operacao de leitura sensivel: se nao for possivel
        // selar o acesso, a leitura nao pode ser considerada concluida (deny-by-default da
        // accountability). Por isso NAO engolimos a excecao — ela aborta a resposta.
        logger.LogInformation(
            "Trilha de acesso LGPD: leitura sensivel de {Entidade} (base legal {BaseLegal}).",
            sensivel.EntidadeSensivel,
            sensivel.BaseLegal);

        await registro.RegistrarAsync(
            sensivel.EntidadeSensivel,
            sensivel.EntidadeId,
            sensivel.BaseLegal,
            cancellationToken).ConfigureAwait(false);

        return response;
    }
}
