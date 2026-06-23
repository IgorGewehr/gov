using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>
/// Exceção de domínio: tentativa de emitir CDA sem um requisito legal obrigatório (LEF art. 2º §5º,
/// I–VI / CTN art. 202). A ausência de qualquer requisito gera NULIDADE da CDA e da execução, por isso
/// o domínio RECUSA a emissão na origem.
/// </summary>
public sealed class CdaRequisitoAusenteException : InvalidOperationException
{
    /// <summary>Cria a exceção indicando o requisito ausente.</summary>
    /// <param name="requisito">Nome do requisito legal ausente.</param>
    public CdaRequisitoAusenteException(string requisito)
        : base($"CDA inválida (nula): requisito legal obrigatório ausente — {requisito} (LEF art. 2º §5º / CTN art. 202).")
        => Requisito = requisito;

    /// <summary>Requisito legal ausente.</summary>
    public string Requisito { get; }
}

/// <summary>
/// Certidão de Dívida Ativa — documento extraído do Termo de Inscrição (TIDA), com os requisitos legais
/// OBRIGATÓRIOS da LEF (Lei 6.830/80) art. 2º §5º, incisos I–VI, e CTN art. 202. Modelada como Value
/// Object imutável: nasce válida ou não nasce (a factory recusa se faltar requisito → nulidade evitada
/// na origem). // TODO(validar-oficial): texto literal do art. 2º §5º direto do Planalto.
/// </summary>
public sealed class CertidaoDividaAtiva : ValueObject
{
    private CertidaoDividaAtiva(
        string numero,
        string nomeDevedor,
        string? domicilioDevedor,
        string coResponsaveis,
        ValorMonetario valorOriginario,
        DateOnly termoInicialEncargos,
        string formaCalculoEncargos,
        string origemNatureza,
        string fundamentoLegal,
        string fundamentoCorrecaoMonetaria,
        DateOnly dataInscricao,
        long numeroInscricao,
        string? processoAdministrativo)
    {
        Numero = numero;
        NomeDevedor = nomeDevedor;
        DomicilioDevedor = domicilioDevedor;
        CoResponsaveis = coResponsaveis;
        ValorOriginario = valorOriginario;
        TermoInicialEncargos = termoInicialEncargos;
        FormaCalculoEncargos = formaCalculoEncargos;
        OrigemNatureza = origemNatureza;
        FundamentoLegal = fundamentoLegal;
        FundamentoCorrecaoMonetaria = fundamentoCorrecaoMonetaria;
        DataInscricao = dataInscricao;
        NumeroInscricao = numeroInscricao;
        ProcessoAdministrativo = processoAdministrativo;
    }

    /// <summary>Número da CDA.</summary>
    public string Numero { get; } = default!;

    /// <summary>Inc. I — nome do devedor.</summary>
    public string NomeDevedor { get; } = default!;

    /// <summary>Inc. I — domicílio/residência do devedor, se conhecido (opcional).</summary>
    public string? DomicilioDevedor { get; }

    /// <summary>Inc. I — co-responsáveis, quando houver (texto livre; vazio se não houver).</summary>
    public string CoResponsaveis { get; } = default!;

    /// <summary>Inc. II — valor originário da dívida (R$).</summary>
    public ValorMonetario ValorOriginario { get; } = default!;

    /// <summary>Inc. II — termo inicial dos encargos (juros/mora).</summary>
    public DateOnly TermoInicialEncargos { get; }

    /// <summary>Inc. II — forma de cálculo dos juros de mora e demais encargos (lei/contrato).</summary>
    public string FormaCalculoEncargos { get; } = default!;

    /// <summary>Inc. III — origem e natureza da dívida (espécie tributária / fato).</summary>
    public string OrigemNatureza { get; } = default!;

    /// <summary>Inc. III — fundamento legal da dívida (lei municipal).</summary>
    public string FundamentoLegal { get; } = default!;

    /// <summary>Inc. IV — fundamento legal da correção monetária + termo inicial (texto).</summary>
    public string FundamentoCorrecaoMonetaria { get; } = default!;

    /// <summary>Inc. V — data da inscrição no Registro de Dívida Ativa.</summary>
    public DateOnly DataInscricao { get; }

    /// <summary>Inc. V — número (sequencial) da inscrição no Registro de Dívida Ativa.</summary>
    public long NumeroInscricao { get; }

    /// <summary>Inc. VI — nº do processo administrativo / auto de infração, se neles apurado (opcional).</summary>
    public string? ProcessoAdministrativo { get; }

    /// <summary>
    /// Emite a CDA validando TODOS os requisitos legais. RECUSA (lança <see cref="CdaRequisitoAusenteException"/>)
    /// se faltar qualquer requisito obrigatório — evita a nulidade prevista na LEF.
    /// </summary>
    /// <param name="numero">Número da CDA.</param>
    /// <param name="nomeDevedor">Inc. I — nome do devedor (obrigatório).</param>
    /// <param name="domicilioDevedor">Inc. I — domicílio do devedor (opcional, "se conhecido").</param>
    /// <param name="coResponsaveis">Inc. I — co-responsáveis (opcional).</param>
    /// <param name="valorOriginario">Inc. II — valor originário (obrigatório, &gt; 0).</param>
    /// <param name="termoInicialEncargos">Inc. II — termo inicial dos encargos (obrigatório).</param>
    /// <param name="formaCalculoEncargos">Inc. II — forma de cálculo de juros/encargos (obrigatório).</param>
    /// <param name="origemNatureza">Inc. III — origem e natureza (obrigatório).</param>
    /// <param name="fundamentoLegal">Inc. III — fundamento legal (obrigatório).</param>
    /// <param name="fundamentoCorrecaoMonetaria">Inc. IV — fundamento da correção monetária (obrigatório).</param>
    /// <param name="dataInscricao">Inc. V — data da inscrição (obrigatório).</param>
    /// <param name="numeroInscricao">Inc. V — número da inscrição (obrigatório, &gt; 0).</param>
    /// <param name="processoAdministrativo">Inc. VI — nº do processo administrativo (opcional).</param>
    /// <returns>A CDA válida.</returns>
    /// <exception cref="CdaRequisitoAusenteException">Se faltar requisito obrigatório.</exception>
    public static CertidaoDividaAtiva Emitir(
        string numero,
        string nomeDevedor,
        string? domicilioDevedor,
        string? coResponsaveis,
        ValorMonetario valorOriginario,
        DateOnly termoInicialEncargos,
        string formaCalculoEncargos,
        string origemNatureza,
        string fundamentoLegal,
        string fundamentoCorrecaoMonetaria,
        DateOnly dataInscricao,
        long numeroInscricao,
        string? processoAdministrativo)
    {
        ArgumentNullException.ThrowIfNull(valorOriginario);

        ExigirPreenchido(numero, "Número da CDA");
        ExigirPreenchido(nomeDevedor, "Nome do devedor (inc. I)");
        if (valorOriginario.Valor <= 0m)
        {
            throw new CdaRequisitoAusenteException("Valor originário (inc. II)");
        }

        ExigirPreenchido(formaCalculoEncargos, "Forma de cálculo de juros/encargos (inc. II)");
        ExigirPreenchido(origemNatureza, "Origem e natureza da dívida (inc. III)");
        ExigirPreenchido(fundamentoLegal, "Fundamento legal (inc. III)");
        ExigirPreenchido(fundamentoCorrecaoMonetaria, "Fundamento da correção monetária (inc. IV)");
        if (numeroInscricao <= 0)
        {
            throw new CdaRequisitoAusenteException("Número da inscrição (inc. V)");
        }

        return new CertidaoDividaAtiva(
            numero.Trim(),
            nomeDevedor.Trim(),
            string.IsNullOrWhiteSpace(domicilioDevedor) ? null : domicilioDevedor.Trim(),
            string.IsNullOrWhiteSpace(coResponsaveis) ? string.Empty : coResponsaveis.Trim(),
            valorOriginario,
            termoInicialEncargos,
            formaCalculoEncargos.Trim(),
            origemNatureza.Trim(),
            fundamentoLegal.Trim(),
            fundamentoCorrecaoMonetaria.Trim(),
            dataInscricao,
            numeroInscricao,
            string.IsNullOrWhiteSpace(processoAdministrativo) ? null : processoAdministrativo.Trim());
    }

    private static void ExigirPreenchido(string? valor, string requisito)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new CdaRequisitoAusenteException(requisito);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Numero;
        yield return NomeDevedor;
        yield return DomicilioDevedor;
        yield return CoResponsaveis;
        yield return ValorOriginario;
        yield return TermoInicialEncargos;
        yield return FormaCalculoEncargos;
        yield return OrigemNatureza;
        yield return FundamentoLegal;
        yield return FundamentoCorrecaoMonetaria;
        yield return DataInscricao;
        yield return NumeroInscricao;
        yield return ProcessoAdministrativo;
    }
}
