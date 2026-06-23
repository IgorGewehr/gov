using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="MarcacaoPonto"/> (AFD append-only).</summary>
public interface IMarcacaoPontoRepository
{
    /// <summary>Marca uma nova marcacao para insercao (nunca atualiza/remove — AFD imutavel).</summary>
    /// <param name="marcacao">Marcacao a adicionar.</param>
    void Adicionar(MarcacaoPonto marcacao);

    /// <summary>
    /// Obtem o ultimo NSR ja persistido no tenant (REP) para calcular o proximo da sequencia sem
    /// lacunas; <c>null</c> quando ainda nao ha marcacoes.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Maior NSR do tenant, ou <c>null</c>.</returns>
    Task<long?> ObterUltimoNsrAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Dedup EM LOTE da ingestao de AFD: dentre os <paramref name="nsrsEquipamento"/> informados,
    /// devolve os que JA existem para o equipamento <paramref name="repId"/> no tenant (chave natural
    /// <c>(TenantId, RepId, NsrEquipamento)</c>). Permite reimportar um AFD sem duplicar marcacoes.
    /// </summary>
    /// <param name="repId">Equipamento (REP) de origem.</param>
    /// <param name="nsrsEquipamento">NSR de equipamento candidatos do lote.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Subconjunto de NSR de equipamento ja persistidos para o REP no tenant.</returns>
    Task<IReadOnlySet<long>> NsrsEquipamentoExistentesAsync(
        Guid repId,
        IReadOnlyCollection<long> nsrsEquipamento,
        CancellationToken cancellationToken);

    /// <summary>Lista as marcacoes de um periodo (datas inclusivas), ordenadas por NSR.</summary>
    /// <param name="inicio">Data inicial (inclusiva).</param>
    /// <param name="fim">Data final (inclusiva).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Marcacoes do periodo no tenant.</returns>
    Task<IReadOnlyList<MarcacaoPonto>> ListarPorPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken);

    /// <summary>Lista as marcacoes de um servidor numa competencia, ordenadas por data/hora.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Marcacoes do servidor na competencia.</returns>
    Task<IReadOnlyList<MarcacaoPonto>> ListarPorServidorCompetenciaAsync(Guid servidorId, Competencia competencia, CancellationToken cancellationToken);
}
