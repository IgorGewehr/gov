using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="Servidor"/>.</summary>
public interface IServidorRepository
{
    /// <summary>Marca um novo servidor para insercao.</summary>
    /// <param name="servidor">Servidor a adicionar.</param>
    void Adicionar(Servidor servidor);

    /// <summary>Obtem um servidor por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O servidor, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Servidor?> ObterPorIdAsync(ServidorId id, CancellationToken cancellationToken);

    /// <summary>Obtem um servidor pela matricula (unica por tenant).</summary>
    /// <param name="matricula">Matricula do vinculo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O servidor, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Servidor?> ObterPorMatriculaAsync(Matricula matricula, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe um servidor com a matricula informada no tenant (I-11).</summary>
    /// <param name="matricula">Matricula a verificar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se a matricula ja estiver em uso no tenant.</returns>
    Task<bool> MatriculaExisteAsync(Matricula matricula, CancellationToken cancellationToken);

    /// <summary>Lista os servidores ativos do tenant (situacao diferente de <c>Desligado</c>).</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Servidores ativos do tenant.</returns>
    Task<IReadOnlyList<Servidor>> ListarAtivosAsync(CancellationToken cancellationToken);
}
