using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

/// <summary>
/// Repasse (liberacao recebida) do convenio federal (entidade filha do agregado <see cref="ConvenioRecebido"/>):
/// uma parcela do cronograma de desembolso que e efetivamente liberada/recebida na conta vinculada.
/// </summary>
public sealed class Repasse : Entity<Guid>
{
    private Repasse()
    {
    }

    private Repasse(Guid id, int numeroOrdem, Dinheiro valor, DateOnly dataPrevista)
        : base(id)
    {
        NumeroOrdem = numeroOrdem;
        Valor = valor;
        DataPrevista = dataPrevista;
        Situacao = SituacaoRepasse.Prevista;
    }

    /// <summary>Numero de ordem da parcela (corresponde ao cronograma do plano).</summary>
    public int NumeroOrdem { get; private set; }

    /// <summary>Valor da parcela.</summary>
    public Dinheiro Valor { get; private set; } = default!;

    /// <summary>Data prevista de liberacao.</summary>
    public DateOnly DataPrevista { get; private set; }

    /// <summary>Data em que foi efetivamente liberada (nula enquanto prevista).</summary>
    public DateOnly? DataLiberada { get; private set; }

    /// <summary>Situacao do repasse (prevista/liberada/bloqueada).</summary>
    public SituacaoRepasse Situacao { get; private set; }

    /// <summary>Cria um repasse PREVISTO a partir de uma parcela do cronograma.</summary>
    /// <param name="numeroOrdem">Numero de ordem.</param>
    /// <param name="valor">Valor.</param>
    /// <param name="dataPrevista">Data prevista.</param>
    /// <returns>Novo <see cref="Repasse"/> em <see cref="SituacaoRepasse.Prevista"/>.</returns>
    public static Repasse Prever(int numeroOrdem, Dinheiro valor, DateOnly dataPrevista)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numeroOrdem);
        return new Repasse(Guid.NewGuid(), numeroOrdem, valor, dataPrevista);
    }

    /// <summary>Marca o repasse como liberado/recebido na <paramref name="dataLiberada"/>.</summary>
    /// <param name="dataLiberada">Data de liberacao.</param>
    /// <exception cref="InvalidOperationException">Se o repasse estiver bloqueado ou ja liberado.</exception>
    public void Liberar(DateOnly dataLiberada)
    {
        if (Situacao == SituacaoRepasse.Bloqueada)
        {
            throw new InvalidOperationException($"Repasse {NumeroOrdem} bloqueado nao pode ser liberado.");
        }

        if (Situacao == SituacaoRepasse.Liberada)
        {
            throw new InvalidOperationException($"Repasse {NumeroOrdem} ja foi liberado.");
        }

        DataLiberada = dataLiberada;
        Situacao = SituacaoRepasse.Liberada;
    }

    /// <summary>Bloqueia o repasse (gatilho de inadimplencia) — so se ainda nao liberado.</summary>
    public void Bloquear()
    {
        if (Situacao == SituacaoRepasse.Prevista)
        {
            Situacao = SituacaoRepasse.Bloqueada;
        }
    }
}
