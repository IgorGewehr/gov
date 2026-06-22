using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Abstractions;

/// <summary>
/// Porta de leitura da cota/limite de vagas aplicavel a um procedimento (intra-modulo Saude). A
/// fonte de verdade da cota pode ser tabela propria; o agregado de regulacao apenas guarda o
/// snapshot no momento da solicitacao.
/// </summary>
public interface ICotaRepository
{
    /// <summary>Obtem a cota aplicavel a um procedimento SIGTAP na unidade solicitante.</summary>
    /// <param name="codigoSigtap">Codigo SIGTAP do procedimento.</param>
    /// <param name="estabelecimentoSolicitanteId">Unidade solicitante (CNES).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A cota aplicavel (snapshot de disponibilidade).</returns>
    Task<Cota> ObterCotaAsync(
        string codigoSigtap,
        EstabelecimentoId estabelecimentoSolicitanteId,
        CancellationToken cancellationToken);
}
