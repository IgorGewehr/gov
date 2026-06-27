using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;

namespace Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Msc;

/// <summary>
/// Matriz de Saldos Contabeis (MSC) de uma competencia: o conjunto de <see cref="LinhaMsc"/> derivado do
/// balancete, com o invariante de fechamento (partida dobrada): a soma dos saldos finais do lado devedor
/// iguala a do lado credor. Modela a MSC como objeto de dominio coeso (nao e agregado persistido — e
/// derivada/read-model gerada sob demanda e publicada via evento de integracao).
/// </summary>
public sealed class MatrizSaldosContabeis
{
    /// <summary>Tolerancia de arredondamento ao conferir o fechamento (centavos).</summary>
    public const decimal ToleranciaFechamento = 0.01m;

    /// <summary>Mes 12 — competencia em que a inscricao de Restos a Pagar deve constar na MSC agregada.</summary>
    public const int MesInscricaoRestosAPagar = 12;

    // Contas de controle/RP esperadas na MSC AGREGADA de dezembro (Regras Gerais MSC 2026, quadro "Restos
    // a Pagar", p.13): RPNP a liquidar/em liquidacao/liquidados (6.2.2.1.3.05/06/07), RPNP inscritos
    // (6.3.1.7.1 a liquidar / 6.3.1.7.2 em liquidacao) e RPP inscritos (6.3.2.7.0). Sao PREFIXOS — a conta
    // analitica real do ente desce abaixo deles. Normativo (nao magico): documentado contra a norma.
    private static readonly string[] PrefixosContasRestosAPagar =
    [
        "6.2.2.1.3.05",
        "6.2.2.1.3.06",
        "6.2.2.1.3.07",
        "6.3.1.7.1",
        "6.3.1.7.2",
        "6.3.2.7.0",
    ];

    private readonly List<LinhaMsc> _linhas;

    private MatrizSaldosContabeis(
        Guid tenantId,
        int exercicio,
        int mes,
        TipoMatrizMsc tipoMatriz,
        List<LinhaMsc> linhas)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Mes = mes;
        TipoMatriz = tipoMatriz;
        _linhas = linhas;
    }

    /// <summary>Tenant (ente publico) dono da matriz.</summary>
    public Guid TenantId { get; }

    /// <summary>Exercicio.</summary>
    public int Exercicio { get; }

    /// <summary>Mes da competencia (1-12; 13 = Encerramento).</summary>
    public int Mes { get; }

    /// <summary>Tipo da matriz (Agregada/Encerramento).</summary>
    public TipoMatrizMsc TipoMatriz { get; }

    /// <summary>Linhas (registros de valor) da matriz.</summary>
    public IReadOnlyList<LinhaMsc> Linhas => _linhas;

    /// <summary>Soma dos saldos finais do lado devedor.</summary>
    public decimal TotalSaldoFinalDevedor => SomarSaldoFinal(NaturezaSaldo.Devedora);

    /// <summary>Soma dos saldos finais do lado credor.</summary>
    public decimal TotalSaldoFinalCredor => SomarSaldoFinal(NaturezaSaldo.Credora);

    /// <summary>Indica se a matriz esta balanceada (Sigma SaldoFinal D = Sigma SaldoFinal C).</summary>
    public bool EstaBalanceada =>
        Math.Abs(TotalSaldoFinalDevedor - TotalSaldoFinalCredor) <= ToleranciaFechamento;

    /// <summary>
    /// Monta a matriz a partir das linhas ja derivadas, validando as DUAS regras duras do SICONFI (Regras
    /// Gerais MSC 2026, "Observacoes Importantes"): (1) balanco D=C POR CLASSE contabil (natureza de
    /// informacao: Patrimonial, Orcamentaria, Controle) — nao basta o total geral fechar; (2) consistencia
    /// <c>saldo_inicial + movimento = saldo_final</c> por conta. Bloqueia a criacao se qualquer regra falhar
    /// (uma MSC que viole isso seria rejeitada pelo SICONFI — nao se pode publicar invalida).
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="exercicio">Exercicio.</param>
    /// <param name="mes">Mes da competencia.</param>
    /// <param name="tipoMatriz">Tipo da matriz.</param>
    /// <param name="linhas">Linhas derivadas do balancete.</param>
    /// <returns>Nova matriz validada.</returns>
    /// <exception cref="MatrizSaldosDesbalanceadaException">Se o fechamento D=C global nao confere.</exception>
    /// <exception cref="MatrizSaldosClasseDesbalanceadaException">Se alguma classe contabil nao fecha D=C.</exception>
    /// <exception cref="MatrizSaldosContaInconsistenteException">Se SI+movimento != SF em alguma conta.</exception>
    public static MatrizSaldosContabeis Montar(
        Guid tenantId,
        int exercicio,
        int mes,
        TipoMatrizMsc tipoMatriz,
        IEnumerable<LinhaMsc> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);

        var matriz = new MatrizSaldosContabeis(tenantId, exercicio, mes, tipoMatriz, linhas.ToList());

        // Regra geral: o total D=C deve fechar (partida dobrada do balancete consolidado).
        if (!matriz.EstaBalanceada)
        {
            throw new MatrizSaldosDesbalanceadaException(
                exercicio,
                mes,
                matriz.TotalSaldoFinalDevedor,
                matriz.TotalSaldoFinalCredor);
        }

        // Regra dura SICONFI #1: balanco D=C POR CLASSE contabil.
        matriz.ValidarBalancoPorClasse(exercicio, mes);

        // Regra dura SICONFI #2: SI + movimento = SF por conta.
        matriz.ValidarConsistenciaSaldoPorConta();

        return matriz;
    }

    // Regra dura SICONFI #1 (Regras Gerais MSC 2026): em cada classe contabil (Patrimonial=1-4,
    // Orcamentaria=5-6, Controle=7-8) a soma dos saldos finais devedores deve igualar a dos credores.
    private void ValidarBalancoPorClasse(int exercicio, int mes)
    {
        foreach (var grupo in _linhas
                     .Where(l => l.TipoValor == TipoValorMsc.SaldoFinal)
                     .GroupBy(l => ClasseDe(l.ContaPcasp)))
        {
            var devedor = grupo
                .Where(l => l.NaturezaSaldo == NaturezaSaldo.Devedora)
                .Sum(l => l.Valor);
            var credor = grupo
                .Where(l => l.NaturezaSaldo == NaturezaSaldo.Credora)
                .Sum(l => l.Valor);

            if (Math.Abs(devedor - credor) > ToleranciaFechamento)
            {
                throw new MatrizSaldosClasseDesbalanceadaException(
                    exercicio,
                    mes,
                    grupo.Key.ToString(),
                    devedor,
                    credor);
            }
        }
    }

    // Regra dura SICONFI #2 (Regras Gerais MSC 2026): por conta, saldo_inicial + movimento = saldo_final.
    // Valores sao tomados COM SINAL no lado NATURAL da conta (Devedora p/ classes impares, Credora p/ pares):
    // uma linha no lado natural soma +Valor; no lado contrario, -Valor. O movimento liquido e a soma com
    // sinal das linhas de MovimentoPeriodo (D=+, C=- quando a conta e devedora; e o inverso quando credora).
    private void ValidarConsistenciaSaldoPorConta()
    {
        foreach (var grupo in _linhas.GroupBy(l => l.ContaPcasp, StringComparer.Ordinal))
        {
            var ladoNatural = NaturezaSaldoNaturalDe(grupo.Key);

            var saldoInicial = SomarComSinal(grupo, TipoValorMsc.SaldoInicial, ladoNatural);
            var movimento = SomarComSinal(grupo, TipoValorMsc.MovimentoPeriodo, ladoNatural);
            var saldoFinalInformado = SomarComSinal(grupo, TipoValorMsc.SaldoFinal, ladoNatural);

            var saldoFinalApurado = saldoInicial + movimento;
            if (Math.Abs(saldoFinalApurado - saldoFinalInformado) > ToleranciaFechamento)
            {
                throw new MatrizSaldosContaInconsistenteException(
                    grupo.Key,
                    saldoInicial,
                    movimento,
                    saldoFinalApurado,
                    saldoFinalInformado);
            }
        }
    }

    // Soma os valores de um tipo, com sinal no lado natural: +Valor se a linha esta no lado natural,
    // -Valor se no lado contrario (a MSC nao admite negativos, entao o sinal vem do lado D/C da linha).
    private static decimal SomarComSinal(
        IEnumerable<LinhaMsc> linhasDaConta,
        TipoValorMsc tipoValor,
        NaturezaSaldo ladoNatural)
        => linhasDaConta
            .Where(l => l.TipoValor == tipoValor)
            .Sum(l => l.NaturezaSaldo == ladoNatural ? l.Valor : -l.Valor);

    // Lado natural do saldo da conta a partir do 1o digito (PCASP §2): classes impares (1,3,5,7)
    // devedoras; pares (2,4,6,8) credoras. So o 1o digito do codigo importa para o lado natural.
    private static NaturezaSaldo NaturezaSaldoNaturalDe(string contaPcasp)
        => PrimeiroDigito(contaPcasp) % 2 == 1 ? NaturezaSaldo.Devedora : NaturezaSaldo.Credora;

    // Classe contabil (natureza de informacao) a partir do 1o digito da conta PCASP:
    // 1-4 Patrimonial, 5-6 Orcamentaria, 7-8 Controle (PCASP §3).
    private static NaturezaInformacao ClasseDe(string contaPcasp)
        => PrimeiroDigito(contaPcasp) switch
        {
            >= 1 and <= 4 => NaturezaInformacao.Patrimonial,
            5 or 6 => NaturezaInformacao.Orcamentaria,
            _ => NaturezaInformacao.Controle,
        };

    private static int PrimeiroDigito(string contaPcasp)
    {
        foreach (var c in contaPcasp)
        {
            if (char.IsAsciiDigit(c))
            {
                return c - '0';
            }
        }

        // Conta sem digito inicial e impossivel (LinhaMsc exige conta nao vazia); trata como controle.
        return 7;
    }

    /// <summary>
    /// Verifica, na MSC AGREGADA de dezembro (mes 12), a presenca das contas de controle de Restos a Pagar
    /// (Regras Gerais MSC 2026, quadro "Restos a Pagar", p.13): RPNP a liquidar/em liquidacao/liquidados
    /// (6.2.2.1.3.05/06/07) e RP inscritos (6.3.1.7.1/2, 6.3.2.7.0). A ausencia NAO e bloqueante (um ente
    /// sem restos a inscrever nao tera essas contas), mas e um alerta de conformidade para o SICONFI: a
    /// MSC de dezembro de um ente com execucao deve trazer a inscricao de RP. Retorna os prefixos AUSENTES.
    /// </summary>
    /// <returns>
    /// Lista dos prefixos de conta de RP nao encontrados na matriz; vazia se todos presentes (ou se a
    /// matriz nao for de dezembro — nesse caso a checagem nao se aplica e retorna vazia).
    /// </returns>
    public IReadOnlyList<string> ContasRestosAPagarAusentes()
    {
        if (Mes != MesInscricaoRestosAPagar)
        {
            return [];
        }

        var ausentes = new List<string>(PrefixosContasRestosAPagar.Length);
        foreach (var prefixo in PrefixosContasRestosAPagar)
        {
            var presente = _linhas.Any(l =>
                l.ContaPcasp.StartsWith(prefixo, StringComparison.Ordinal));
            if (!presente)
            {
                ausentes.Add(prefixo);
            }
        }

        return ausentes;
    }

    private decimal SomarSaldoFinal(NaturezaSaldo natureza)
        => _linhas
            .Where(l => l.TipoValor == TipoValorMsc.SaldoFinal && l.NaturezaSaldo == natureza)
            .Sum(l => l.Valor);
}
