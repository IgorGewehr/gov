using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
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

    /// <summary>
    /// Busca paginada de servidores por nome/matricula (navegabilidade — Onda 0), com filtros opcionais
    /// por situacao, regime e cargo. Tenant-scoped via Global Query Filter. Ordena por nome.
    /// </summary>
    /// <param name="termo">Termo livre (nome ou matricula; case-insensivel); nulo lista tudo.</param>
    /// <param name="situacao">Filtro opcional por situacao no ciclo de vida do vinculo.</param>
    /// <param name="regime">Filtro opcional por regime previdenciario (RPPS/RGPS).</param>
    /// <param name="cargoId">Filtro opcional por cargo provido.</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de servidores e o total que atende ao filtro.</returns>
    Task<(IReadOnlyList<Servidor> Itens, int Total)> BuscarAsync(
        string? termo,
        SituacaoServidor? situacao,
        RegimePrevidenciario? regime,
        CargoId? cargoId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
