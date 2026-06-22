using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>
/// Persistência dos registros de controle de geração da MSC (<see cref="MscGeradaRegistro"/>), usados
/// para garantir a idempotência por competência. Implementada na Infrastructure sobre o schema
/// <c>financas</c>.
/// </summary>
public interface IMscGeradaStore
{
    /// <summary>Indica se a MSC da competência (e tipo) já foi gerada para o tenant atual.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="mes">Mês da competência.</param>
    /// <param name="tipoMatriz">Tipo da matriz (1 = Agregada, 2 = Encerramento).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O registro existente, ou <c>null</c> se ainda não gerada.</returns>
    Task<MscGeradaRegistro?> ObterAsync(
        int exercicio,
        int mes,
        int tipoMatriz,
        CancellationToken cancellationToken);

    /// <summary>Adiciona um registro de geração (não confirma a transação).</summary>
    /// <param name="registro">Registro a persistir.</param>
    void Adicionar(MscGeradaRegistro registro);
}
