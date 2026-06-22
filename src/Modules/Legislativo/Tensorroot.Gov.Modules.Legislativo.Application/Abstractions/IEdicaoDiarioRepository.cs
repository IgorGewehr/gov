using Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="EdicaoDiario"/>.</summary>
public interface IEdicaoDiarioRepository
{
    /// <summary>Marca uma nova edicao para insercao.</summary>
    /// <param name="edicao">Edicao a adicionar.</param>
    void Adicionar(EdicaoDiario edicao);

    /// <summary>Obtem uma edicao por identificador (com materias), respeitando o tenant.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A edicao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<EdicaoDiario?> ObterPorIdAsync(EdicaoDiarioId id, CancellationToken cancellationToken);

    /// <summary>Calcula o proximo numero sequencial de edicao no (tenant, ano) — D-5.</summary>
    /// <param name="ano">Ano da edicao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Proximo numero (1 quando nao ha edicoes no ano).</returns>
    Task<int> ProximoNumeroAsync(int ano, CancellationToken cancellationToken);

    /// <summary>Lista edicoes do tenant (filtros opcionais por ano/situacao), paginadas.</summary>
    /// <param name="ano">Ano (opcional).</param>
    /// <param name="situacao">Situacao (opcional).</param>
    /// <param name="apenasPublicadas">Quando verdadeiro, retorna apenas edicoes publicadas (consulta cidada/LAI).</param>
    /// <param name="pagina">Pagina (base 1).</param>
    /// <param name="tamanho">Tamanho da pagina.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pagina de edicoes e total.</returns>
    Task<(IReadOnlyList<EdicaoDiario> Itens, int Total)> ListarAsync(
        int? ano,
        SituacaoEdicao? situacao,
        bool apenasPublicadas,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);
}
