using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="JornadaTrabalho"/>.</summary>
public interface IJornadaTrabalhoRepository
{
    /// <summary>Marca uma nova jornada para insercao.</summary>
    /// <param name="jornada">Jornada a adicionar.</param>
    void Adicionar(JornadaTrabalho jornada);

    /// <summary>Obtem a jornada ATIVA vigente de um servidor numa data (a mais recente ate a data).</summary>
    /// <param name="servidorId">Servidor.</param>
    /// <param name="data">Data de referencia.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Jornada vigente, ou <c>null</c> se inexistente.</returns>
    Task<JornadaTrabalho?> ObterVigenteAsync(Guid servidorId, DateOnly data, CancellationToken cancellationToken);
}
