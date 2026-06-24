using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

namespace Tensorroot.Gov.Modules.Convenios.Application.Abstractions;

/// <summary>
/// Repositorio do agregado <see cref="ConvenioRecebido"/> (fluxo A). Tenant-scoped pelo Global Query Filter
/// do DbContext; as escritas sao confirmadas pelo <c>IUnitOfWork</c> do pipeline.
/// </summary>
public interface IConvenioRecebidoRepository
{
    /// <summary>Adiciona um novo convenio ao contexto.</summary>
    /// <param name="convenio">Convenio a adicionar.</param>
    void Adicionar(ConvenioRecebido convenio);

    /// <summary>Carrega um convenio pelo identificador (com filhos), ou nulo se nao existir no tenant.</summary>
    /// <param name="id">Identificador do convenio.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O convenio, ou nulo.</returns>
    Task<ConvenioRecebido?> ObterPorIdAsync(ConvenioRecebidoId id, CancellationToken cancellationToken);

    /// <summary>Lista os convenios do tenant (read-side), opcionalmente filtrando por situacao.</summary>
    /// <param name="situacao">Situacao (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Convenios do tenant.</returns>
    Task<IReadOnlyList<ConvenioRecebido>> ListarAsync(
        Domain.Recebidos.SituacaoConvenioRecebido? situacao, CancellationToken cancellationToken);
}
