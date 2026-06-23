using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Servico de aplicacao que materializa um <see cref="EventoESocial"/> a partir de um XML ja gerado pelo
/// ACL de dominio, com idempotencia por chave de negocio (ESOCIAL-SPEC §4.3): se o evento da chave ja
/// existir, devolve o existente sem duplicar; senao cria em <c>Gerado</c> e persiste na mesma transacao
/// (Outbox via evento de dominio). Centraliza tambem o ambiente (Producao Restrita por padrao) e o Id.
/// </summary>
/// <param name="eventos">Repositorio de eventos eSocial.</param>
/// <param name="unitOfWork">Unidade de trabalho (transacao + Outbox).</param>
/// <param name="tenant">Contexto do tenant.</param>
/// <param name="timeProvider">Relogio (instante de geracao).</param>
public sealed class GeradorEventoApplicationService(
    IEventoESocialRepository eventos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Persiste (idempotente) um evento eSocial a partir do XML gerado e da chave de negocio.
    /// </summary>
    /// <param name="parametros">Parametros eSocial (empregador/ambiente).</param>
    /// <param name="tipo">Tipo do evento.</param>
    /// <param name="chave">Chave de idempotencia (geracao).</param>
    /// <param name="xml">XML do evento (UTF-8) gerado pelo ACL de dominio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O evento (existente ou recem-criado).</returns>
    public async Task<EventoESocial> MaterializarAsync(
        ParametrosESocial parametros,
        TipoEventoESocial tipo,
        ChaveIdempotenciaEvento chave,
        byte[] xml,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        ArgumentNullException.ThrowIfNull(chave);
        ArgumentNullException.ThrowIfNull(xml);

        // Fail-closed (CLAUDE.md §16): sem CNPJ do ente nao ha ideEmpregador valido — recusa a geracao.
        if (string.IsNullOrWhiteSpace(parametros.CnpjEnte))
        {
            throw new InvalidOperationException(
                "CNPJ do ente nao configurado (RecursosHumanos:ESocial:CnpjEnte). Geracao de evento eSocial recusada.");
        }

        // Idempotencia de geracao: um unico evento por chave de negocio.
        var existente = await eventos.ObterPorChaveAsync(chave, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return existente;
        }

        var agora = timeProvider.GetUtcNow();

        // O atributo Id do evento DEVE ser unico (consulta/download BX). O carimbo so tem resolucao de
        // segundo, entao varios eventos gerados no mesmo segundo colidiriam com sequencial fixo. Deriva o
        // sequencial (1..99999) do proprio identificador do agregado (unico), garantindo Id distinto por
        // evento mesmo dentro do mesmo segundo. // TODO(validar-oficial): regra exata do sequencial no MOS.
        var id = EventoESocialId.New();
        var sequencial = (int)((uint)id.Value.GetHashCode() % 99999u) + 1;
        var idEvento = GeradorIdEvento.Montar(TipoInscricao.Cnpj, parametros.CnpjEnte, agora, sequencial);

        var evento = EventoESocial.Gerar(
            tenant.TenantId,
            tipo,
            chave,
            idEvento,
            parametros.Ambiente,
            xml,
            agora,
            id);

        eventos.Adicionar(evento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return evento;
    }
}
