using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Arquivistica;

/// <summary>
/// Metadados arquivisticos minimos do SIGAD (e-ARQ v2 / Res. CONARQ 50/2022 e 37/2012), embarcaveis no
/// processo/documento: produtor, tipo documental, data de producao, restricao de acesso, suporte
/// original e fundo/serie. O subconjunto OBRIGATORIO por tenant e decisao de produto (// TODO(M10)).
/// </summary>
public sealed class MetadadosArquivisticos : ValueObject
{
    /// <summary>Comprimento maximo do orgao produtor.</summary>
    public const int ComprimentoMaximoProdutor = 200;

    /// <summary>Comprimento maximo do tipo documental.</summary>
    public const int ComprimentoMaximoTipo = 120;

    /// <summary>Comprimento maximo do fundo/serie.</summary>
    public const int ComprimentoMaximoFundoSerie = 200;

    /// <summary>Comprimento maximo do suporte original.</summary>
    public const int ComprimentoMaximoSuporte = 60;

    private MetadadosArquivisticos(
        string produtorOrgao,
        string tipoDocumental,
        DateOnly dataProducao,
        NivelDeAcesso restricaoAcesso,
        string? suporteOriginal,
        string? fundoSerie)
    {
        ProdutorOrgao = produtorOrgao;
        TipoDocumental = tipoDocumental;
        DataProducao = dataProducao;
        RestricaoAcesso = restricaoAcesso;
        SuporteOriginal = suporteOriginal;
        FundoSerie = fundoSerie;
    }

    /// <summary>Orgao produtor do documento/processo.</summary>
    public string ProdutorOrgao { get; }

    /// <summary>Tipo documental (ex.: "Oficio", "Requerimento").</summary>
    public string TipoDocumental { get; }

    /// <summary>Data de producao.</summary>
    public DateOnly DataProducao { get; }

    /// <summary>Restricao de acesso (reusa o <see cref="NivelDeAcesso"/> do processo).</summary>
    public NivelDeAcesso RestricaoAcesso { get; }

    /// <summary>Suporte original (ex.: "nato-digital", "papel digitalizado").</summary>
    public string? SuporteOriginal { get; }

    /// <summary>Fundo/serie arquivistica.</summary>
    public string? FundoSerie { get; }

    /// <summary>Cria metadados arquivisticos validados.</summary>
    /// <param name="produtorOrgao">Orgao produtor.</param>
    /// <param name="tipoDocumental">Tipo documental.</param>
    /// <param name="dataProducao">Data de producao.</param>
    /// <param name="restricaoAcesso">Restricao de acesso.</param>
    /// <param name="suporteOriginal">Suporte original (opcional).</param>
    /// <param name="fundoSerie">Fundo/serie (opcional).</param>
    /// <returns>Instancia de <see cref="MetadadosArquivisticos"/>.</returns>
    /// <exception cref="ArgumentException">Se campos obrigatorios forem vazios ou excederem limites.</exception>
    public static MetadadosArquivisticos De(
        string produtorOrgao,
        string tipoDocumental,
        DateOnly dataProducao,
        NivelDeAcesso restricaoAcesso,
        string? suporteOriginal = null,
        string? fundoSerie = null)
    {
        var produtor = Normalizar(produtorOrgao, nameof(produtorOrgao), ComprimentoMaximoProdutor);
        var tipo = Normalizar(tipoDocumental, nameof(tipoDocumental), ComprimentoMaximoTipo);
        var suporte = NormalizarOpcional(suporteOriginal, nameof(suporteOriginal), ComprimentoMaximoSuporte);
        var fundo = NormalizarOpcional(fundoSerie, nameof(fundoSerie), ComprimentoMaximoFundoSerie);

        return new MetadadosArquivisticos(produtor, tipo, dataProducao, restricaoAcesso, suporte, fundo);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ProdutorOrgao;
        yield return TipoDocumental;
        yield return DataProducao;
        yield return RestricaoAcesso;
        yield return SuporteOriginal;
        yield return FundoSerie;
    }

    private static string Normalizar(string valor, string nome, int comprimentoMaximo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, nome);
        var normalizado = valor.Trim();
        if (normalizado.Length > comprimentoMaximo)
        {
            throw new ArgumentException($"'{nome}' excede {comprimentoMaximo} caracteres.", nome);
        }

        return normalizado;
    }

    private static string? NormalizarOpcional(string? valor, string nome, int comprimentoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();
        if (normalizado.Length > comprimentoMaximo)
        {
            throw new ArgumentException($"'{nome}' excede {comprimentoMaximo} caracteres.", nome);
        }

        return normalizado;
    }
}
