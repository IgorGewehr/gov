using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Pbf;

/// <summary>Repositorio do agregado <see cref="AcompanhamentoCondicionalidade"/> (tenant-scoped).</summary>
public interface IAcompanhamentoCondicionalidadeRepository
{
    /// <summary>Adiciona um novo acompanhamento.</summary>
    /// <param name="acompanhamento">Acompanhamento a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AdicionarAsync(AcompanhamentoCondicionalidade acompanhamento, CancellationToken cancellationToken);

    /// <summary>Obtem o acompanhamento por identificador (com seus registros), tenant-scoped.</summary>
    /// <param name="id">Identificador do acompanhamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O acompanhamento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<AcompanhamentoCondicionalidade?> ObterPorIdAsync(AcompanhamentoCondicionalidadeId id, CancellationToken cancellationToken);

    /// <summary>Obtem o acompanhamento de uma familia numa competencia (chave de negocio), tenant-scoped.</summary>
    /// <param name="familiaId">Familia beneficiaria.</param>
    /// <param name="competencia">Competencia (ano/mes).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O acompanhamento, ou <c>null</c> se ainda nao aberto no tenant.</returns>
    Task<AcompanhamentoCondicionalidade?> ObterPorFamiliaCompetenciaAsync(FamiliaId familiaId, Competencia competencia, CancellationToken cancellationToken);

    /// <summary>Lista os acompanhamentos numa competencia, opcionalmente filtrados por efeito gradativo.</summary>
    /// <param name="competencia">Competencia (ano/mes).</param>
    /// <param name="efeitoMinimo">Efeito minimo a incluir (ex.: somente em descumprimento). Nulo = todos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Acompanhamentos do periodo (tenant-scoped).</returns>
    Task<IReadOnlyList<AcompanhamentoCondicionalidade>> ListarPorCompetenciaAsync(
        Competencia competencia,
        EfeitoDescumprimento? efeitoMinimo,
        CancellationToken cancellationToken);
}
