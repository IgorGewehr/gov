using BancoDeHorasAgregado = Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto.BancoDeHoras;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="BancoDeHorasAgregado"/> (saldo vivo por servidor).</summary>
public interface IBancoDeHorasRepository
{
    /// <summary>Marca um novo banco de horas para insercao.</summary>
    /// <param name="banco">Banco de horas a adicionar.</param>
    void Adicionar(BancoDeHorasAgregado banco);

    /// <summary>Obtem o banco de horas de um servidor (unico por tenant; carrega os lancamentos).</summary>
    /// <param name="servidorId">Servidor titular.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O banco de horas, ou <c>null</c> quando ainda nao aberto.</returns>
    Task<BancoDeHorasAgregado?> ObterPorServidorAsync(Guid servidorId, CancellationToken cancellationToken);

    /// <summary>Lista todos os bancos de horas do tenant (rotina de prescricao em lote).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bancos de horas do tenant.</returns>
    Task<IReadOnlyList<BancoDeHorasAgregado>> ListarTodosAsync(CancellationToken cancellationToken);
}
