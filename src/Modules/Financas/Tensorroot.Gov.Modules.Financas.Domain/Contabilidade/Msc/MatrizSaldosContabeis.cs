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
    /// Monta a matriz a partir das linhas ja derivadas, validando o invariante de fechamento. Bloqueia a
    /// criacao se desbalanceada (partida dobrada quebrada — nao se pode publicar uma MSC invalida).
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="exercicio">Exercicio.</param>
    /// <param name="mes">Mes da competencia.</param>
    /// <param name="tipoMatriz">Tipo da matriz.</param>
    /// <param name="linhas">Linhas derivadas do balancete.</param>
    /// <returns>Nova matriz validada.</returns>
    /// <exception cref="MatrizSaldosDesbalanceadaException">Se o fechamento D=C nao confere.</exception>
    public static MatrizSaldosContabeis Montar(
        Guid tenantId,
        int exercicio,
        int mes,
        TipoMatrizMsc tipoMatriz,
        IEnumerable<LinhaMsc> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);

        var matriz = new MatrizSaldosContabeis(tenantId, exercicio, mes, tipoMatriz, linhas.ToList());

        if (!matriz.EstaBalanceada)
        {
            throw new MatrizSaldosDesbalanceadaException(
                exercicio,
                mes,
                matriz.TotalSaldoFinalDevedor,
                matriz.TotalSaldoFinalCredor);
        }

        return matriz;
    }

    private decimal SomarSaldoFinal(NaturezaSaldo natureza)
        => _linhas
            .Where(l => l.TipoValor == TipoValorMsc.SaldoFinal && l.NaturezaSaldo == natureza)
            .Sum(l => l.Valor);
}
