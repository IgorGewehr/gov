using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Credenciamento"/> (sempre tenant-scoped via Global Query Filter).</summary>
public interface ICredenciamentoRepository
{
    /// <summary>Marca um novo edital de credenciamento para insercao.</summary>
    /// <param name="credenciamento">Credenciamento a adicionar.</param>
    void Adicionar(Credenciamento credenciamento);

    /// <summary>Obtem um credenciamento por identificador (com itens e credenciados carregados).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O credenciamento, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Credenciamento?> ObterPorIdAsync(CredenciamentoId id, CancellationToken cancellationToken);

    /// <summary>Lista credenciamentos por situacao (ou todos, se nao informada).</summary>
    /// <param name="situacao">Filtro de situacao (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Credenciamentos do tenant.</returns>
    Task<IReadOnlyList<Credenciamento>> ListarAsync(SituacaoCredenciamento? situacao, CancellationToken cancellationToken);
}
