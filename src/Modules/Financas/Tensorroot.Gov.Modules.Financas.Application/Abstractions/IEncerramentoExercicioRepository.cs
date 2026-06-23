using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Encerramento;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado de controle <see cref="EncerramentoExercicio"/> (1 por tenant+exercício).</summary>
public interface IEncerramentoExercicioRepository
{
    /// <summary>Marca um novo agregado para inserção.</summary>
    /// <param name="encerramento">Agregado a adicionar.</param>
    void Adicionar(EncerramentoExercicio encerramento);

    /// <summary>Obtém o agregado de encerramento de um exercício, ou <c>null</c> se ainda não iniciado.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O agregado, ou <c>null</c>.</returns>
    Task<EncerramentoExercicio?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}
