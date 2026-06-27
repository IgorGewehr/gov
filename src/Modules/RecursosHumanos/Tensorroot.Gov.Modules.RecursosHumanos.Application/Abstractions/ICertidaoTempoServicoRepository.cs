using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="CertidaoTempoServico"/> (CTC — tenant-scoped via Global Query Filter).
/// </summary>
public interface ICertidaoTempoServicoRepository
{
    /// <summary>Marca uma nova certidao para insercao.</summary>
    /// <param name="certidao">Certidao a adicionar.</param>
    void Adicionar(CertidaoTempoServico certidao);

    /// <summary>Obtem a certidao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A certidao, ou <c>null</c>.</returns>
    Task<CertidaoTempoServico?> ObterPorIdAsync(CertidaoTempoServicoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtem a certidao VIGENTE pelo codigo de autenticacao (validacao publica de autenticidade). So retorna
    /// certidoes emitidas (nao anuladas) — a validacao publica nunca confirma documento sem efeito.
    /// </summary>
    /// <param name="codigo">Codigo de autenticacao (hex, 16 caracteres).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A certidao vigente, ou <c>null</c>.</returns>
    Task<CertidaoTempoServico?> ObterVigentePorCodigoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>Lista as certidoes de um servidor, da mais recente para a mais antiga.</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Certidoes do servidor.</returns>
    Task<IReadOnlyList<CertidaoTempoServico>> ListarPorServidorAsync(ServidorId servidorId, CancellationToken cancellationToken);

    /// <summary>Proximo sequencial de certidao do exercicio no tenant (numeracao reiniciada por ano).</summary>
    /// <param name="exercicio">Exercicio (ano civil).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O proximo sequencial (>= 1).</returns>
    Task<int> ProximoSequencialAsync(int exercicio, CancellationToken cancellationToken);
}
