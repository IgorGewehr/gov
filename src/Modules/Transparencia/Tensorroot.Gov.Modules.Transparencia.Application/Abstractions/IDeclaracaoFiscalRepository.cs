using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Repositorio do agregado <see cref="DeclaracaoFiscal"/>.</summary>
public interface IDeclaracaoFiscalRepository
{
    /// <summary>Marca uma nova declaracao fiscal para insercao.</summary>
    /// <param name="declaracaoFiscal">Declaracao a adicionar.</param>
    void Adicionar(DeclaracaoFiscal declaracaoFiscal);

    /// <summary>Obtem uma declaracao por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A declaracao, ou <c>null</c> se inexistente no tenant.</returns>
    Task<DeclaracaoFiscal?> ObterPorIdAsync(DeclaracaoFiscalId id, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se ja existe uma declaracao vigente (nao rejeitada) para o tipo/periodo informados,
    /// apoiando a idempotencia do consumo da MSC (I-13).
    /// </summary>
    /// <param name="tipo">Tipo da declaracao.</param>
    /// <param name="exercicio">Ano de exercicio.</param>
    /// <param name="mes">Mes da competencia (MSC), quando aplicavel.</param>
    /// <param name="numeroBimestre">Numero do bimestre (RREO), quando aplicavel.</param>
    /// <param name="numeroQuadrimestre">Numero do quadrimestre (RGF), quando aplicavel.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existe declaracao vigente para o periodo.</returns>
    Task<bool> ExisteVigenteAsync(
        TipoDeclaracaoFiscal tipo,
        int exercicio,
        int? mes,
        int? numeroBimestre,
        int? numeroQuadrimestre,
        CancellationToken cancellationToken);

    /// <summary>Lista as declaracoes do exercicio (com filtros opcionais de tipo e situacao), tenant-scoped.</summary>
    /// <param name="exercicio">Ano de exercicio.</param>
    /// <param name="tipo">Filtro opcional por tipo de declaracao.</param>
    /// <param name="situacao">Filtro opcional por situacao.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Declaracoes do exercicio.</returns>
    Task<IReadOnlyList<DeclaracaoFiscal>> ListarPorExercicioAsync(
        int exercicio,
        TipoDeclaracaoFiscal? tipo,
        SituacaoDeclaracaoFiscal? situacao,
        CancellationToken cancellationToken);
}
