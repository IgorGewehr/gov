using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Pca;

/// <summary>
/// Vinculo de um item do PCA com a contratacao que o concretizou (licitacao/ata/dispensa/inexigibilidade).
/// Materializa a rastreabilidade planejamento -> execucao exigida pela governanca das contratacoes
/// (Lei 14.133/2021, art. 12, VII; Dec. 11.246/2022): permite aferir o grau de cumprimento do PCA e a
/// vinculacao formal entre a necessidade planejada e o instrumento gerado. Value Object.
/// </summary>
public sealed class ContratacaoVinculada : ValueObject
{
    private ContratacaoVinculada(FonteContratacaoPca fonte, Guid referenciaId, string? identificacao)
    {
        Fonte = fonte;
        ReferenciaId = referenciaId;
        Identificacao = identificacao;
    }

    /// <summary>Natureza do instrumento gerado (licitacao, ata, dispensa, inexigibilidade).</summary>
    public FonteContratacaoPca Fonte { get; }

    /// <summary>Identificador do agregado gerado (LicitacaoId/AtaId/DispensaId/CredenciamentoId).</summary>
    public Guid ReferenciaId { get; }

    /// <summary>Identificacao legivel do instrumento (ex.: numero do edital/ata), quando disponivel.</summary>
    public string? Identificacao { get; }

    /// <summary>Cria o vinculo de uma contratacao gerada a partir do item do PCA.</summary>
    /// <param name="fonte">Natureza do instrumento.</param>
    /// <param name="referenciaId">Identificador do agregado gerado.</param>
    /// <param name="identificacao">Identificacao legivel (opcional).</param>
    /// <returns>Novo <see cref="ContratacaoVinculada"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Fonte fora do enum.</exception>
    /// <exception cref="ArgumentException">Referencia vazia.</exception>
    public static ContratacaoVinculada Criar(FonteContratacaoPca fonte, Guid referenciaId, string? identificacao)
    {
        if (!Enum.IsDefined(fonte))
        {
            throw new ArgumentOutOfRangeException(nameof(fonte), "Fonte de contratacao do PCA invalida.");
        }

        if (referenciaId == Guid.Empty)
        {
            throw new ArgumentException("Referencia da contratacao vinculada obrigatoria.", nameof(referenciaId));
        }

        return new ContratacaoVinculada(fonte, referenciaId, string.IsNullOrWhiteSpace(identificacao) ? null : identificacao.Trim());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Fonte;
        yield return ReferenciaId;
    }
}
