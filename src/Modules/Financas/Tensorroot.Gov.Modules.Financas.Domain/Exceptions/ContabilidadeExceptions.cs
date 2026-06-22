namespace Tensorroot.Gov.Modules.Financas.Domain.Exceptions;

/// <summary>Exceção base para violações de invariante do motor contábil (PCASP/MCASP).</summary>
public abstract class ContabilidadeException : InvalidOperationException
{
    /// <summary>Inicializa com a mensagem informada.</summary>
    /// <param name="message">Mensagem de erro.</param>
    protected ContabilidadeException(string message) : base(message)
    {
    }
}

/// <summary>
/// Lançada quando a soma dos débitos difere da soma dos créditos de um lançamento
/// (método das partidas dobradas — MCASP §3).
/// </summary>
public sealed class PartidaDobradaDesbalanceadaException : ContabilidadeException
{
    /// <summary>Cria a exceção com os totais envolvidos.</summary>
    /// <param name="somaDebitos">Soma dos débitos.</param>
    /// <param name="somaCreditos">Soma dos créditos.</param>
    public PartidaDobradaDesbalanceadaException(decimal somaDebitos, decimal somaCreditos)
        : base($"Partida dobrada desbalanceada: debitos {somaDebitos:0.00} != creditos {somaCreditos:0.00}.")
    {
    }
}

/// <summary>
/// Lançada quando partidas de um mesmo lançamento cruzam naturezas de informação
/// (ex.: patrimonial com orçamentária) — proibido pelo MCASP §3.
/// </summary>
public sealed class NaturezasMisturadasException : ContabilidadeException
{
    /// <summary>Cria a exceção.</summary>
    public NaturezasMisturadasException()
        : base("Lancamento contabil nao pode cruzar naturezas de informacao distintas (MCASP §3).")
    {
    }
}

/// <summary>Lançada ao tentar lançar em conta não analítica (sintética) ou inativa.</summary>
public sealed class ContaNaoAnaliticaException : ContabilidadeException
{
    /// <summary>Cria a exceção com o código da conta ofensora.</summary>
    /// <param name="codigoConta">Código da conta.</param>
    public ContaNaoAnaliticaException(string codigoConta)
        : base($"Conta {codigoConta} nao e analitica ativa; nao pode receber lancamento.")
    {
    }
}

/// <summary>Lançada ao registrar lançamento em período contábil já encerrado.</summary>
public sealed class PeriodoContabilFechadoException : ContabilidadeException
{
    /// <summary>Cria a exceção com exercício/mês.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="periodoMes">Mês (1-12).</param>
    public PeriodoContabilFechadoException(int exercicio, int periodoMes)
        : base($"Periodo contabil {exercicio}/{periodoMes:00} esta encerrado; lancamento vedado.")
    {
    }
}

/// <summary>Lançada quando a configuração de um roteiro/conta contábil é inválida.</summary>
public sealed class RoteiroContabilInvalidoException : ContabilidadeException
{
    /// <summary>Cria a exceção com a mensagem.</summary>
    /// <param name="message">Detalhe.</param>
    public RoteiroContabilInvalidoException(string message) : base(message)
    {
    }
}

/// <summary>Lançada quando não há evento contábil vigente para um fato na data informada.</summary>
public sealed class EventoContabilNaoVigenteException : ContabilidadeException
{
    /// <summary>Cria a exceção com o fato e a data.</summary>
    /// <param name="fato">Fato contábil.</param>
    /// <param name="data">Data de competência.</param>
    public EventoContabilNaoVigenteException(string fato, DateOnly data)
        : base($"Nao ha evento contabil vigente para o fato '{fato}' em {data:yyyy-MM-dd}.")
    {
    }
}

/// <summary>
/// Lançada quando a Matriz de Saldos Contábeis de uma competência não fecha (Σ saldo final devedor ≠
/// Σ saldo final credor) — a partida dobrada do balancete está quebrada e a MSC não pode ser publicada.
/// </summary>
public sealed class MatrizSaldosDesbalanceadaException : ContabilidadeException
{
    /// <summary>Cria a exceção com a competência e os totais por lado.</summary>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="mes">Mês da competência.</param>
    /// <param name="totalDevedor">Soma dos saldos finais devedores.</param>
    /// <param name="totalCredor">Soma dos saldos finais credores.</param>
    public MatrizSaldosDesbalanceadaException(int exercicio, int mes, decimal totalDevedor, decimal totalCredor)
        : base($"MSC {exercicio}/{mes:00} desbalanceada: saldo final devedor {totalDevedor:0.00} != credor {totalCredor:0.00}.")
    {
    }
}

/// <summary>
/// Lançada quando uma demonstração DCASP não fecha o invariante esperado (ex.: BP Ativo ≠ Passivo+PL,
/// BO Receita ≠ Despesa, BF Ingressos ≠ Dispêndios) — indica balancete inconsistente ou mapa incorreto.
/// </summary>
public sealed class DemonstrativoDesbalanceadoException : ContabilidadeException
{
    /// <summary>Cria a exceção identificando o demonstrativo e os totais divergentes.</summary>
    /// <param name="demonstrativo">Nome do demonstrativo.</param>
    /// <param name="ladoA">Total do lado A.</param>
    /// <param name="ladoB">Total do lado B.</param>
    public DemonstrativoDesbalanceadoException(string demonstrativo, decimal ladoA, decimal ladoB)
        : base($"Demonstrativo {demonstrativo} desbalanceado: {ladoA:0.00} != {ladoB:0.00}.")
    {
    }
}
