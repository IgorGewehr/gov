using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>Identificador forte de uma <see cref="PensaoAlimenticia"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PensaoAlimenticiaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PensaoAlimenticiaId"/>.</returns>
    public static PensaoAlimenticiaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Forma de apuracao do valor da pensao alimenticia definida na decisao judicial: percentual sobre uma
/// base (em regra a remuneracao liquida apos descontos legais) ou um valor fixo mensal. Parametrizavel —
/// a decisao manda; o sistema nunca presume (CLAUDE.md S7).
/// </summary>
public enum ModalidadePensao
{
    /// <summary>Percentual incidente sobre uma base de calculo (ex.: 30% da remuneracao liquida).</summary>
    PercentualSobreBase = 1,

    /// <summary>Valor fixo mensal (ex.: R$ 800,00 por mes), independente da remuneracao.</summary>
    ValorFixo = 2,
}

/// <summary>
/// Base sobre a qual incide o percentual da pensao quando a modalidade e
/// <see cref="ModalidadePensao.PercentualSobreBase"/>. A decisao judicial define a base; o motor de folha
/// resolve cada uma deterministicamente a partir dos proventos/descontos legais ja apurados.
/// </summary>
public enum BasePensao
{
    /// <summary>Total bruto de proventos do servidor na competencia.</summary>
    ProventosBrutos = 1,

    /// <summary>
    /// Proventos menos a contribuicao previdenciaria (INSS/RPPS) — base "liquida" usual nas sentencas.
    /// Nao inclui o IRRF: a pensao e o que DEDUZ o IRRF, logo nao pode depender dele (sem circularidade).
    /// </summary>
    LiquidoAposDescontosLegais = 2,
}

/// <summary>
/// Pensao alimenticia judicial de um servidor (entidade-filha do agregado <see cref="Servidor"/>, exposta
/// somente pela raiz). Modela uma obrigacao de pagar — fixada por decisao judicial — que (a) e DEDUTIVEL da
/// base do IRRF (Lei 7.713/88 art. 4 II; RIR/2018 art. 75 e art. 101) e (b) gera o desconto do servidor com
/// o REPASSE ao beneficiario. Parametrizavel: percentual sobre uma base ou valor fixo; um servidor pode ter
/// MAIS DE UMA pensao (beneficiarios distintos). Nada hardcoded — a decisao manda (CLAUDE.md S7).
/// </summary>
public sealed class PensaoAlimenticia : Entity<PensaoAlimenticiaId>
{
    private PensaoAlimenticia()
    {
    }

    private PensaoAlimenticia(
        PensaoAlimenticiaId id,
        string beneficiario,
        ModalidadePensao modalidade,
        decimal percentual,
        BasePensao baseIncidencia,
        decimal valorFixo,
        string processoJudicial,
        bool ativa)
        : base(id)
    {
        Beneficiario = beneficiario;
        Modalidade = modalidade;
        Percentual = percentual;
        BaseIncidencia = baseIncidencia;
        ValorFixo = valorFixo;
        ProcessoJudicial = processoJudicial;
        Ativa = ativa;
    }

    /// <summary>Nome do beneficiario (alimentando) para o repasse.</summary>
    public string Beneficiario { get; private set; } = default!;

    /// <summary>Modalidade de apuracao (percentual sobre base ou valor fixo).</summary>
    public ModalidadePensao Modalidade { get; private set; }

    /// <summary>Percentual (fracao 0..1) quando <see cref="ModalidadePensao.PercentualSobreBase"/>; zero no valor fixo.</summary>
    public decimal Percentual { get; private set; }

    /// <summary>Base de incidencia do percentual (quando aplicavel).</summary>
    public BasePensao BaseIncidencia { get; private set; }

    /// <summary>Valor fixo mensal quando <see cref="ModalidadePensao.ValorFixo"/>; zero no percentual.</summary>
    public decimal ValorFixo { get; private set; }

    /// <summary>Identificacao do processo/decisao judicial (auditoria/cumprimento de ordem).</summary>
    public string ProcessoJudicial { get; private set; } = default!;

    /// <summary>Indica se a pensao esta vigente (deve ser descontada na folha).</summary>
    public bool Ativa { get; private set; }

    /// <summary>
    /// Cria uma pensao por PERCENTUAL sobre uma base (a decisao define o percentual e a base).
    /// </summary>
    /// <param name="beneficiario">Nome do alimentando (nao vazio).</param>
    /// <param name="percentual">Fracao da base (0 &lt; p &lt;= 1).</param>
    /// <param name="baseIncidencia">Base sobre a qual incide o percentual.</param>
    /// <param name="processoJudicial">Identificacao do processo judicial (nao vazio).</param>
    /// <returns>Nova <see cref="PensaoAlimenticia"/> percentual e ativa.</returns>
    /// <exception cref="ArgumentException">Se beneficiario/processo for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o percentual estiver fora de (0,1].</exception>
    public static PensaoAlimenticia PorPercentual(
        string beneficiario,
        decimal percentual,
        BasePensao baseIncidencia,
        string processoJudicial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(beneficiario);
        ArgumentException.ThrowIfNullOrWhiteSpace(processoJudicial);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(percentual);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentual, 1m);

        return new PensaoAlimenticia(
            PensaoAlimenticiaId.New(),
            beneficiario.Trim(),
            ModalidadePensao.PercentualSobreBase,
            percentual,
            baseIncidencia,
            valorFixo: 0m,
            processoJudicial.Trim(),
            ativa: true);
    }

    /// <summary>Cria uma pensao por VALOR FIXO mensal.</summary>
    /// <param name="beneficiario">Nome do alimentando (nao vazio).</param>
    /// <param name="valorFixo">Valor fixo mensal (maior que zero).</param>
    /// <param name="processoJudicial">Identificacao do processo judicial (nao vazio).</param>
    /// <returns>Nova <see cref="PensaoAlimenticia"/> de valor fixo e ativa.</returns>
    /// <exception cref="ArgumentException">Se beneficiario/processo for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor fixo nao for positivo.</exception>
    public static PensaoAlimenticia PorValorFixo(string beneficiario, decimal valorFixo, string processoJudicial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(beneficiario);
        ArgumentException.ThrowIfNullOrWhiteSpace(processoJudicial);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valorFixo);

        return new PensaoAlimenticia(
            PensaoAlimenticiaId.New(),
            beneficiario.Trim(),
            ModalidadePensao.ValorFixo,
            percentual: 0m,
            BasePensao.LiquidoAposDescontosLegais,
            valorFixo,
            processoJudicial.Trim(),
            ativa: true);
    }

    /// <summary>Encerra a pensao (cessa o desconto a partir das proximas competencias).</summary>
    public void Encerrar() => Ativa = false;

    /// <summary>
    /// Apura o valor mensal da pensao deterministicamente a partir das bases ja calculadas da folha.
    /// Valor fixo retorna o proprio valor; percentual incide sobre a base escolhida na decisao. Nunca
    /// negativo (uma base negativa por descontos resultaria em zero).
    /// </summary>
    /// <param name="proventosBrutos">Total bruto de proventos do servidor na competencia.</param>
    /// <param name="descontoPrevidenciario">INSS/RPPS apurado na competencia (base liquida nao inclui IRRF).</param>
    /// <returns>Valor mensal da pensao (2 casas), nao-negativo.</returns>
    public decimal Apurar(decimal proventosBrutos, decimal descontoPrevidenciario)
    {
        if (!Ativa)
        {
            return 0m;
        }

        if (Modalidade == ModalidadePensao.ValorFixo)
        {
            return decimal.Round(ValorFixo, 2, MidpointRounding.AwayFromZero);
        }

        var baseCalculo = BaseIncidencia switch
        {
            BasePensao.ProventosBrutos => proventosBrutos,
            BasePensao.LiquidoAposDescontosLegais => proventosBrutos - descontoPrevidenciario,
            _ => proventosBrutos - descontoPrevidenciario,
        };

        if (baseCalculo <= 0m)
        {
            return 0m;
        }

        return decimal.Round(baseCalculo * Percentual, 2, MidpointRounding.AwayFromZero);
    }
}
