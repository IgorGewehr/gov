using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Contrato"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface IContratoRepository
{
    /// <summary>Marca um novo contrato para insercao.</summary>
    /// <param name="contrato">Contrato a adicionar.</param>
    void Adicionar(Contrato contrato);

    /// <summary>Obtem um contrato por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O contrato, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Contrato?> ObterPorIdAsync(ContratoId id, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os contratos vigentes do tenant em uma data de referencia
    /// (situacao Eficaz/EmExecucao e vigencia ainda nao expirada).
    /// </summary>
    /// <param name="referencia">Data de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contratos vigentes do tenant.</returns>
    Task<IReadOnlyList<Contrato>> ListarVigentesAsync(DateOnly referencia, CancellationToken cancellationToken);

    /// <summary>Lista os contratos de um fornecedor no tenant.</summary>
    /// <param name="fornecedorId">Identificador do fornecedor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contratos do fornecedor no tenant.</returns>
    Task<IReadOnlyList<Contrato>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os contratos do tenant PENDENTES de divulgacao no PNCP (ainda nao publicados, nao extintos e
    /// com prazo PNCP definido) — base do varredor de prazos a vencer/vencido (W9.1.d).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contratos aguardando divulgacao no PNCP.</returns>
    Task<IReadOnlyList<Contrato>> ListarPendentesPublicacaoPncpAsync(CancellationToken cancellationToken);
}
