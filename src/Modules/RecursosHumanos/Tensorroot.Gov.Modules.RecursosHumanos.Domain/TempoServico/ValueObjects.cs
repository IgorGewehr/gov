using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

/// <summary>Identificador forte do agregado <see cref="CertidaoTempoServico"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CertidaoTempoServicoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CertidaoTempoServicoId"/>.</returns>
    public static CertidaoTempoServicoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Objeto de Valor da numeracao oficial de uma certidao de tempo: sequencial reiniciado a cada exercicio
/// (ano), unico por <c>(TenantId, Exercicio, Sequencial)</c>. Formatado como <c>NNNN/AAAA</c>
/// (ex.: <c>0042/2026</c>), padrao de identificacao de atos/documentos do ente publico.
/// </summary>
public sealed class NumeroCertidao : ValueObject
{
    /// <summary>Menor sequencial valido (a numeracao comeca em 1 a cada exercicio).</summary>
    public const int SequencialMinimo = 1;

    private NumeroCertidao(int exercicio, int sequencial)
    {
        Exercicio = exercicio;
        Sequencial = sequencial;
    }

    /// <summary>Exercicio (ano civil) da numeracao.</summary>
    public int Exercicio { get; }

    /// <summary>Sequencial dentro do exercicio (>= 1).</summary>
    public int Sequencial { get; }

    /// <summary>Cria uma numeracao de certidao valida.</summary>
    /// <param name="exercicio">Exercicio (ano civil) da numeracao.</param>
    /// <param name="sequencial">Sequencial dentro do exercicio (>= 1).</param>
    /// <returns>Instancia valida de <see cref="NumeroCertidao"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o sequencial for menor que 1 ou o exercicio nao for positivo.</exception>
    public static NumeroCertidao De(int exercicio, int sequencial)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exercicio);
        if (sequencial < SequencialMinimo)
        {
            throw new ArgumentOutOfRangeException(nameof(sequencial), $"Sequencial da certidao deve ser ao menos {SequencialMinimo}.");
        }

        return new NumeroCertidao(exercicio, sequencial);
    }

    /// <summary>Formatacao oficial <c>NNNN/AAAA</c> com o sequencial preenchido a 4 digitos.</summary>
    public string Formatado => $"{Sequencial.ToString("D4", CultureInfo.InvariantCulture)}/{Exercicio.ToString("D4", CultureInfo.InvariantCulture)}";

    /// <inheritdoc />
    public override string ToString() => Formatado;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Exercicio;
        yield return Sequencial;
    }
}

/// <summary>
/// Objeto de Valor do codigo de autenticacao da certidao: hash hexadecimal derivado dos campos estaveis do
/// documento (servidor, numero, finalidade, totais), permitindo a VALIDACAO publica de autenticidade
/// (servico online tipico do balcao). O codigo NAO carrega dado sensivel — e' um digest opaco. O calculo do
/// digest vive na borda (Application) e e' PASSADO ao dominio (este nao computa hash/IO).
/// </summary>
public sealed class CodigoAutenticacao : ValueObject
{
    /// <summary>Comprimento fixo do codigo (16 hex = 64 bits do digest, suficiente para verificacao publica).</summary>
    public const int Comprimento = 16;

    private CodigoAutenticacao(string valor) => Valor = valor;

    /// <summary>Texto hexadecimal maiusculo do codigo (comprimento fixo).</summary>
    public string Valor { get; }

    /// <summary>Cria um codigo de autenticacao a partir do digest hexadecimal ja calculado na borda.</summary>
    /// <param name="hexDigest">Digest hexadecimal (>= 16 caracteres; sera truncado/normalizado a 16 hex maiusculos).</param>
    /// <returns>Instancia valida de <see cref="CodigoAutenticacao"/>.</returns>
    /// <exception cref="ArgumentException">Se o digest for vazio, curto demais ou contiver caracteres nao-hexadecimais.</exception>
    public static CodigoAutenticacao De(string hexDigest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hexDigest);
        var normalizado = hexDigest.Trim().ToUpperInvariant();
        if (normalizado.Length < Comprimento)
        {
            throw new ArgumentException($"Digest deve ter ao menos {Comprimento} caracteres hexadecimais.", nameof(hexDigest));
        }

        var recortado = normalizado[..Comprimento];
        if (!recortado.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Digest deve ser hexadecimal.", nameof(hexDigest));
        }

        return new CodigoAutenticacao(recortado);
    }

    /// <summary>
    /// Tenta criar um codigo de autenticacao a partir de um texto possivelmente invalido (entrada PUBLICA
    /// do balcao de validacao). Diferente de <see cref="De"/>, NAO lanca para codigo malformado: devolve
    /// <c>false</c> e <paramref name="codigo"/> nulo — o servico de validacao trata como "nao confere".
    /// </summary>
    /// <param name="hexDigest">Texto informado pelo cidadao.</param>
    /// <param name="codigo">Codigo normalizado, quando valido.</param>
    /// <returns><c>true</c> se o texto e' um codigo bem formado; caso contrario <c>false</c>.</returns>
    public static bool TryDe(string? hexDigest, out CodigoAutenticacao? codigo)
    {
        codigo = null;
        if (string.IsNullOrWhiteSpace(hexDigest))
        {
            return false;
        }

        var normalizado = hexDigest.Trim().ToUpperInvariant();
        if (normalizado.Length < Comprimento)
        {
            return false;
        }

        var recortado = normalizado[..Comprimento];
        if (!recortado.All(Uri.IsHexDigit))
        {
            return false;
        }

        codigo = new CodigoAutenticacao(recortado);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Valor;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}

/// <summary>
/// Objeto de Valor de um PERIODO computado na certidao de tempo: intervalo fechado <c>[Inicio, Fim]</c> com
/// a contagem de dias liquidos (descontando ja os periodos nao-computaveis sobrepostos), a natureza
/// (efetivo exercicio proprio ou averbado) e o FATOR de conversao aplicavel ao tempo. O fator e' o
/// multiplicador legal (ex.: 1,0 para tempo comum; conversao de tempo especial — atividade insalubre/penosa —
/// majora o tempo comum equivalente, conforme regra do regime de destino). Imutavel e auto-validado.
/// // TODO(validar-oficial): fatores de conversao especial->comum conforme regra vigente do regime de destino
/// (EC 103/2019 vedou conversao para periodos POSTERIORES a 13/11/2019, ressalvado direito adquirido).
/// </summary>
public sealed class PeriodoTempo : ValueObject
{
    /// <summary>Fator neutro (tempo comum, sem conversao): multiplicador 1,0.</summary>
    public const decimal FatorComum = 1.0m;

    private PeriodoTempo(
        DateOnly inicio,
        DateOnly fim,
        int diasNaoComputaveis,
        NaturezaPeriodo natureza,
        decimal fator,
        RegimeOrigemPeriodo? regimeOrigem,
        string? origem,
        string? observacao)
    {
        Inicio = inicio;
        Fim = fim;
        DiasNaoComputaveis = diasNaoComputaveis;
        Natureza = natureza;
        Fator = fator;
        RegimeOrigem = regimeOrigem;
        Origem = origem;
        Observacao = observacao;
    }

    /// <summary>Data inicial do periodo (inclusiva).</summary>
    public DateOnly Inicio { get; }

    /// <summary>Data final do periodo (inclusiva).</summary>
    public DateOnly Fim { get; }

    /// <summary>Dias NAO-COMPUTAVEIS sobrepostos ao periodo (licencas sem contagem de tempo) ja abatidos da contagem.</summary>
    public int DiasNaoComputaveis { get; }

    /// <summary>Natureza do periodo (efetivo exercicio proprio ou tempo averbado).</summary>
    public NaturezaPeriodo Natureza { get; }

    /// <summary>Fator de conversao aplicado ao tempo bruto do periodo (1,0 = comum).</summary>
    public decimal Fator { get; }

    /// <summary>Regime de origem (quando averbado); nulo para efetivo exercicio proprio.</summary>
    public RegimeOrigemPeriodo? RegimeOrigem { get; }

    /// <summary>Orgao/certidao de origem (quando averbado); nulo para efetivo exercicio proprio.</summary>
    public string? Origem { get; }

    /// <summary>Observacao do periodo (texto livre); nula quando ausente.</summary>
    public string? Observacao { get; }

    /// <summary>
    /// Dias BRUTOS do intervalo fechado <c>[Inicio, Fim]</c> (ambos inclusivos — contagem civil de tempo
    /// de servico inclui o dia inicial e o final).
    /// </summary>
    public int DiasBrutos => Fim.DayNumber - Inicio.DayNumber + 1;

    /// <summary>Dias LIQUIDOS computaveis (brutos menos nao-computaveis), nunca negativo.</summary>
    public int DiasLiquidos => Math.Max(0, DiasBrutos - DiasNaoComputaveis);

    /// <summary>
    /// Dias EQUIVALENTES apos a aplicacao do fator de conversao (arredondado ao dia inteiro mais proximo).
    /// E' este o tempo que entra no total da certidao.
    /// </summary>
    public int DiasEquivalentes => (int)Math.Round(DiasLiquidos * Fator, MidpointRounding.AwayFromZero);

    /// <summary>Cria um periodo de efetivo exercicio (tempo proprio apurado do vinculo), com fator comum por padrao.</summary>
    /// <param name="inicio">Inicio do periodo (inclusivo).</param>
    /// <param name="fim">Fim do periodo (inclusivo, >= inicio).</param>
    /// <param name="diasNaoComputaveis">Dias nao-computaveis sobrepostos (>= 0).</param>
    /// <param name="fator">Fator de conversao (>= 1,0; default comum).</param>
    /// <param name="observacao">Observacao (opcional).</param>
    /// <returns>Periodo de efetivo exercicio valido.</returns>
    public static PeriodoTempo EfetivoExercicio(
        DateOnly inicio,
        DateOnly fim,
        int diasNaoComputaveis = 0,
        decimal fator = FatorComum,
        string? observacao = null)
        => Criar(inicio, fim, diasNaoComputaveis, NaturezaPeriodo.EfetivoExercicio, fator, regimeOrigem: null, origem: null, observacao);

    /// <summary>Cria um periodo AVERBADO de outro orgao/regime (importado por certidao de origem).</summary>
    /// <param name="inicio">Inicio do periodo (inclusivo).</param>
    /// <param name="fim">Fim do periodo (inclusivo, >= inicio).</param>
    /// <param name="regimeOrigem">Regime previdenciario de origem.</param>
    /// <param name="origem">Orgao/certidao de origem (nao vazio).</param>
    /// <param name="diasNaoComputaveis">Dias nao-computaveis sobrepostos (>= 0).</param>
    /// <param name="fator">Fator de conversao (>= 1,0; default comum).</param>
    /// <param name="observacao">Observacao (opcional).</param>
    /// <returns>Periodo averbado valido.</returns>
    /// <exception cref="ArgumentException">Se a origem for vazia.</exception>
    public static PeriodoTempo Averbado(
        DateOnly inicio,
        DateOnly fim,
        RegimeOrigemPeriodo regimeOrigem,
        string origem,
        int diasNaoComputaveis = 0,
        decimal fator = FatorComum,
        string? observacao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origem);
        return Criar(inicio, fim, diasNaoComputaveis, NaturezaPeriodo.Averbado, fator, regimeOrigem, origem.Trim(), observacao);
    }

    private static PeriodoTempo Criar(
        DateOnly inicio,
        DateOnly fim,
        int diasNaoComputaveis,
        NaturezaPeriodo natureza,
        decimal fator,
        RegimeOrigemPeriodo? regimeOrigem,
        string? origem,
        string? observacao)
    {
        if (fim < inicio)
        {
            throw new ArgumentException("Fim do periodo nao pode ser anterior ao inicio.", nameof(fim));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(diasNaoComputaveis);
        if (fator < FatorComum)
        {
            throw new ArgumentOutOfRangeException(nameof(fator), $"Fator de conversao nao pode ser inferior a {FatorComum} (conversao apenas MAJORA o tempo comum).");
        }

        var brutos = fim.DayNumber - inicio.DayNumber + 1;
        if (diasNaoComputaveis > brutos)
        {
            throw new ArgumentException("Dias nao-computaveis nao podem exceder os dias brutos do periodo.", nameof(diasNaoComputaveis));
        }

        var observacaoNormalizada = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        return new PeriodoTempo(inicio, fim, diasNaoComputaveis, natureza, fator, regimeOrigem, origem, observacaoNormalizada);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Inicio;
        yield return Fim;
        yield return DiasNaoComputaveis;
        yield return Natureza;
        yield return Fator;
        yield return RegimeOrigem;
        yield return Origem;
    }
}

/// <summary>
/// Objeto de Valor que decompoe um total de DIAS em anos/meses/dias pelo padrao civil de contagem de tempo
/// de servico (ano = 365 dias; mes = 30 dias — convencao consagrada em certidoes de tempo). Imutavel.
/// // TODO(validar-oficial): a convencao 365/30 e' a usual; confirmar com o regulamento do regime de destino.
/// </summary>
public sealed record TempoDecomposto(int Anos, int Meses, int Dias, int TotalDias)
{
    /// <summary>Dias por ano na convencao civil de tempo de servico.</summary>
    public const int DiasPorAno = 365;

    /// <summary>Dias por mes na convencao civil de tempo de servico.</summary>
    public const int DiasPorMes = 30;

    /// <summary>Decompoe um total de dias em anos/meses/dias (convencao 365/30).</summary>
    /// <param name="totalDias">Total de dias (>= 0).</param>
    /// <returns>Tempo decomposto.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o total for negativo.</exception>
    public static TempoDecomposto De(int totalDias)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalDias);
        var anos = totalDias / DiasPorAno;
        var resto = totalDias % DiasPorAno;
        var meses = resto / DiasPorMes;
        var dias = resto % DiasPorMes;
        return new TempoDecomposto(anos, meses, dias, totalDias);
    }

    /// <summary>Formatacao legivel <c>"AA ano(s), MM mes(es) e DD dia(s)"</c>.</summary>
    public string Formatado => $"{Anos} ano(s), {Meses} mes(es) e {Dias} dia(s)";
}
