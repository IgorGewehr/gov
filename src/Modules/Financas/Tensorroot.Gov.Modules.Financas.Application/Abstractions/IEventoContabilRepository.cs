using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="EventoContabil"/> (roteiros parametrizáveis).</summary>
public interface IEventoContabilRepository
{
    /// <summary>Marca um novo roteiro para inserção.</summary>
    /// <param name="evento">Roteiro a adicionar.</param>
    void Adicionar(EventoContabil evento);

    /// <summary>Resolve o roteiro vigente para um fato no exercício informado.</summary>
    /// <param name="fato">Fato contábil.</param>
    /// <param name="exercicio">Exercício de competência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O roteiro vigente, ou <c>null</c>.</returns>
    Task<EventoContabil?> ObterVigenteAsync(FatoContabil fato, int exercicio, CancellationToken cancellationToken);

    /// <summary>Indica se já há roteiro para o fato (idempotência do seed).</summary>
    /// <param name="fato">Fato contábil.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se já existir.</returns>
    Task<bool> ExisteParaFatoAsync(FatoContabil fato, CancellationToken cancellationToken);
}
