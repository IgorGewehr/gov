using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Empenho"/>.</summary>
public interface IEmpenhoRepository
{
    /// <summary>Marca um novo empenho para inserção.</summary>
    /// <param name="empenho">Empenho a adicionar.</param>
    void Adicionar(Empenho empenho);

    /// <summary>Obtém um empenho por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O empenho, ou <c>null</c>.</returns>
    Task<Empenho?> ObterPorIdAsync(EmpenhoId id, CancellationToken cancellationToken);

    /// <summary>Lista os empenhos que oneram uma dotação.</summary>
    /// <param name="dotacaoId">Dotação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Empenhos da dotação.</returns>
    Task<IReadOnlyList<Empenho>> ListarPorDotacaoAsync(DotacaoOrcamentariaId dotacaoId, CancellationToken cancellationToken);

    /// <summary>Lista empenhos de um exercício com saldo aberto (para inscrição em Restos a Pagar).</summary>
    /// <param name="exercicio">Exercício orçamentário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Empenhos com saldo aberto.</returns>
    Task<IReadOnlyList<Empenho>> ListarComSaldoAbertoPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}
