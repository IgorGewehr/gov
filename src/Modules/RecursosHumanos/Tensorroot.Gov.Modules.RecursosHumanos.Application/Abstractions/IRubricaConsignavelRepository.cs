using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio do catalogo de <see cref="RubricaConsignavel"/> (parametrizacao por tenant).</summary>
public interface IRubricaConsignavelRepository
{
    /// <summary>Marca uma nova rubrica consignavel para insercao.</summary>
    /// <param name="rubrica">Rubrica a adicionar.</param>
    void Adicionar(RubricaConsignavel rubrica);

    /// <summary>Obtem uma rubrica consignavel por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A rubrica, ou <c>null</c> se inexistente no tenant.</returns>
    Task<RubricaConsignavel?> ObterPorIdAsync(RubricaConsignavelId id, CancellationToken cancellationToken);

    /// <summary>Obtem a rubrica consignavel ATIVA pelo codigo (referencia S-1010) no tenant.</summary>
    /// <param name="codigo">Codigo da rubrica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A rubrica ativa, ou <c>null</c> se inexistente/inativa.</returns>
    Task<RubricaConsignavel?> ObterAtivaPorCodigoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>Indica se ja existe rubrica consignavel com o codigo no tenant.</summary>
    /// <param name="codigo">Codigo da rubrica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se ja existir.</returns>
    Task<bool> ExistePorCodigoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>Lista as rubricas consignaveis ATIVAS do tenant cujos codigos compoem a base da margem.</summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Codigos de rubrica (S-1010) que contam para a margem.</returns>
    Task<IReadOnlyList<string>> ListarCodigosQueContamParaMargemAsync(CancellationToken cancellationToken);
}
