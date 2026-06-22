using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ValueObjects;

/// <summary>
/// Lotacao/estabelecimento de exercicio do cargo. Identifica a unidade onde o servidor
/// presta servico, alinhada ao eSocial: <c>InscricaoEstabelecimento</c> mapeia o estabelecimento
/// declarado em S-1005 e <c>CodigoLotacaoTributaria</c> a lotacao tributaria em S-1020. A vigencia
/// da referencia em S-1005/S-1020 na competencia e verificada no momento do uso pela folha (I-10).
/// </summary>
public sealed class Lotacao : ValueObject
{
    private Lotacao(string inscricaoEstabelecimento, string denominacaoUnidade, string? codigoLotacaoTributaria)
    {
        InscricaoEstabelecimento = inscricaoEstabelecimento;
        DenominacaoUnidade = denominacaoUnidade;
        CodigoLotacaoTributaria = codigoLotacaoTributaria;
    }

    /// <summary>Inscricao do estabelecimento (S-1005).</summary>
    public string InscricaoEstabelecimento { get; }

    /// <summary>Denominacao da unidade/secretaria de exercicio.</summary>
    public string DenominacaoUnidade { get; }

    /// <summary>Codigo da lotacao tributaria (S-1020), quando aplicavel.</summary>
    public string? CodigoLotacaoTributaria { get; }

    /// <summary>Cria uma lotacao de exercicio.</summary>
    /// <param name="inscricaoEstabelecimento">Inscricao do estabelecimento (S-1005).</param>
    /// <param name="denominacaoUnidade">Denominacao da unidade de exercicio.</param>
    /// <param name="codigoLotacaoTributaria">Codigo da lotacao tributaria (S-1020), opcional.</param>
    /// <returns>Instancia de <see cref="Lotacao"/>.</returns>
    /// <exception cref="ArgumentException">Se a inscricao do estabelecimento ou a denominacao forem vazias.</exception>
    public static Lotacao Criar(string inscricaoEstabelecimento, string denominacaoUnidade, string? codigoLotacaoTributaria = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inscricaoEstabelecimento);
        ArgumentException.ThrowIfNullOrWhiteSpace(denominacaoUnidade);
        return new Lotacao(
            inscricaoEstabelecimento.Trim(),
            denominacaoUnidade.Trim(),
            string.IsNullOrWhiteSpace(codigoLotacaoTributaria) ? null : codigoLotacaoTributaria.Trim());
    }

    /// <inheritdoc />
    public override string ToString() => DenominacaoUnidade;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InscricaoEstabelecimento;
        yield return DenominacaoUnidade;
        yield return CodigoLotacaoTributaria;
    }
}
