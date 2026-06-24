using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;

/// <summary>
/// Repasse (saida) a OSC (entidade filha do agregado <see cref="ParceriaOsc"/>): uma parcela do cronograma
/// efetivamente liberada, com espelho da execucao orcamentaria (empenho/liquidacao/pagamento de Financas).
/// Um repasse so e liberado quando ha <c>empenho-&gt;liquidacao-&gt;pagamento</c> correspondentes (B-INV-5).
/// </summary>
public sealed class RepasseOsc : Entity<Guid>
{
    private RepasseOsc()
    {
    }

    private RepasseOsc(Guid id, int numeroOrdem, Dinheiro valor, DateOnly dataPrevista, string condicionantes)
        : base(id)
    {
        NumeroOrdem = numeroOrdem;
        Valor = valor;
        DataPrevista = dataPrevista;
        Condicionantes = condicionantes;
        Situacao = SituacaoRepasse.Prevista;
    }

    /// <summary>Numero de ordem da parcela.</summary>
    public int NumeroOrdem { get; private set; }

    /// <summary>Valor da parcela.</summary>
    public Dinheiro Valor { get; private set; } = default!;

    /// <summary>Data prevista de repasse.</summary>
    public DateOnly DataPrevista { get; private set; }

    /// <summary>Condicionantes da liberacao (ex.: "PC parcial aprovada").</summary>
    public string Condicionantes { get; private set; } = default!;

    /// <summary>Situacao do repasse (prevista/liberada/bloqueada).</summary>
    public SituacaoRepasse Situacao { get; private set; }

    /// <summary>Data em que foi liberado (nula enquanto previsto).</summary>
    public DateOnly? DataLiberada { get; private set; }

    /// <summary>Empenho correspondente (espelho de Financas) — nulo ate o empenho.</summary>
    public Guid? EmpenhoId { get; private set; }

    /// <summary>Liquidacao correspondente (espelho de Financas) — nula ate a liquidacao.</summary>
    public Guid? LiquidacaoId { get; private set; }

    /// <summary>Pagamento correspondente (espelho de Financas) — nulo ate o pagamento.</summary>
    public Guid? PagamentoId { get; private set; }

    /// <summary>Cria um repasse PREVISTO a partir de uma parcela do cronograma.</summary>
    /// <param name="numeroOrdem">Numero de ordem.</param>
    /// <param name="valor">Valor.</param>
    /// <param name="dataPrevista">Data prevista.</param>
    /// <param name="condicionantes">Condicionantes.</param>
    /// <returns>Novo repasse previsto.</returns>
    public static RepasseOsc Prever(int numeroOrdem, Dinheiro valor, DateOnly dataPrevista, string condicionantes)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numeroOrdem);
        return new RepasseOsc(Guid.NewGuid(), numeroOrdem, valor, dataPrevista, condicionantes ?? string.Empty);
    }

    /// <summary>Verdadeiro se o espelho de execucao orcamentaria esta completo (B-INV-5).</summary>
    public bool ExecucaoOrcamentariaCompleta => EmpenhoId is not null && LiquidacaoId is not null && PagamentoId is not null;

    /// <summary>Vincula o empenho (espelho de Financas).</summary>
    /// <param name="empenhoId">Identificador do empenho.</param>
    public void VincularEmpenho(Guid empenhoId) => EmpenhoId = empenhoId;

    /// <summary>Vincula a liquidacao (espelho de Financas).</summary>
    /// <param name="liquidacaoId">Identificador da liquidacao.</param>
    public void VincularLiquidacao(Guid liquidacaoId) => LiquidacaoId = liquidacaoId;

    /// <summary>Vincula o pagamento (espelho de Financas).</summary>
    /// <param name="pagamentoId">Identificador do pagamento.</param>
    public void VincularPagamento(Guid pagamentoId) => PagamentoId = pagamentoId;

    /// <summary>
    /// Libera o repasse (B-INV-5): exige espelho de execucao orcamentaria completo e que o repasse nao
    /// esteja bloqueado/ja liberado.
    /// </summary>
    /// <param name="dataLiberada">Data de liberacao.</param>
    /// <exception cref="InvalidOperationException">Se bloqueado, ja liberado, ou sem execucao orcamentaria completa (B-INV-5).</exception>
    public void Liberar(DateOnly dataLiberada)
    {
        if (Situacao == SituacaoRepasse.Bloqueada)
        {
            throw new InvalidOperationException($"Repasse {NumeroOrdem} bloqueado (inadimplencia) nao pode ser liberado (B-INV-9).");
        }

        if (Situacao == SituacaoRepasse.Liberada)
        {
            throw new InvalidOperationException($"Repasse {NumeroOrdem} ja foi liberado.");
        }

        if (!ExecucaoOrcamentariaCompleta)
        {
            throw new InvalidOperationException(
                $"Repasse {NumeroOrdem} exige empenho->liquidacao->pagamento correspondentes (B-INV-5).");
        }

        DataLiberada = dataLiberada;
        Situacao = SituacaoRepasse.Liberada;
    }

    /// <summary>Bloqueia o repasse (gatilho de inadimplencia LRF — B-INV-9) — so se ainda nao liberado.</summary>
    public void Bloquear()
    {
        if (Situacao == SituacaoRepasse.Prevista)
        {
            Situacao = SituacaoRepasse.Bloqueada;
        }
    }
}
