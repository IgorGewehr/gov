using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;

/// <summary>
/// Rendimento de aplicacao financeira do saldo em conta vinculada (entidade filha do agregado
/// <see cref="ConvenioRecebido"/>). Os rendimentos INTEGRAM o objeto (Dec. 11.531/2023): se
/// <see cref="Aplicado"/> = false ao concluir, o saldo entra na PC final como devolucao (A-INV-6).
/// </summary>
public sealed class RendimentoAplicacaoFinanceira : Entity<Guid>
{
    private RendimentoAplicacaoFinanceira()
    {
    }

    private RendimentoAplicacaoFinanceira(Guid id, Dinheiro valor, DateOnly data)
        : base(id)
    {
        Valor = valor;
        Data = data;
        Aplicado = false;
    }

    /// <summary>Valor do rendimento.</summary>
    public Dinheiro Valor { get; private set; } = default!;

    /// <summary>Data do rendimento (competencia do extrato).</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Verdadeiro se o rendimento foi reaplicado no objeto (senao, devolve-se na PC final).</summary>
    public bool Aplicado { get; private set; }

    /// <summary>Registra um rendimento ainda nao aplicado.</summary>
    /// <param name="valor">Valor do rendimento.</param>
    /// <param name="data">Data do rendimento.</param>
    /// <returns>Novo rendimento (nao aplicado).</returns>
    public static RendimentoAplicacaoFinanceira Registrar(Dinheiro valor, DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(valor);
        return new RendimentoAplicacaoFinanceira(Guid.NewGuid(), valor, data);
    }

    /// <summary>Marca o rendimento como reaplicado no objeto.</summary>
    public void MarcarAplicado() => Aplicado = true;
}
