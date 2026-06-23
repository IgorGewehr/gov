using System.Collections.ObjectModel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;

/// <summary>
/// Lançada quando a LOA viola a cadeia de compatibilidade LOA ⊆ LDO ⊆ PPA
/// (CF 167, I e §1º; CF 165 §2º) — lista os itens infratores para correção.
/// </summary>
public sealed class IncompatibilidadeOrcamentariaException : InvalidOperationException
{
    /// <summary>Cria a exceção com os motivos de incompatibilidade.</summary>
    /// <param name="motivos">Descrições dos itens/regras infringidos.</param>
    public IncompatibilidadeOrcamentariaException(IReadOnlyList<string> motivos)
        : base($"LOA incompativel com PPA/LDO: {string.Join("; ", motivos ?? [])}.")
        => Motivos = new ReadOnlyCollection<string>(motivos is null ? [] : [.. motivos]);

    /// <summary>Motivos de incompatibilidade detectados.</summary>
    public IReadOnlyList<string> Motivos { get; }
}

/// <summary>
/// Lançada quando uma transição de situação (PPA/LDO/LOA) não é permitida pela
/// máquina de estados do planejamento.
/// </summary>
public sealed class TransicaoPlanejamentoInvalidaException : InvalidOperationException
{
    /// <summary>Cria a exceção descrevendo a transição inválida.</summary>
    /// <param name="agregado">Nome do agregado.</param>
    /// <param name="origem">Situação de origem.</param>
    /// <param name="operacao">Operação tentada.</param>
    public TransicaoPlanejamentoInvalidaException(string agregado, string origem, string operacao)
        : base($"{agregado}: operacao '{operacao}' invalida a partir da situacao '{origem}'.")
    {
    }
}

/// <summary>
/// Lançada quando um crédito adicional viola a regra de limite de suplementação por
/// decreto autorizado na LOA (art. 7º da Lei 4.320/64 / CF 167, V).
/// </summary>
public sealed class LimiteSuplementacaoExcedidoException : InvalidOperationException
{
    /// <summary>Cria a exceção com os valores envolvidos.</summary>
    /// <param name="limite">Limite autorizado (em moeda).</param>
    /// <param name="acumuladoComEste">Soma de suplementações por decreto incluindo a atual.</param>
    public LimiteSuplementacaoExcedidoException(decimal limite, decimal acumuladoComEste)
        : base($"Limite de suplementacao por decreto excedido: autorizado {limite:0.00}, acumulado {acumuladoComEste:0.00}.")
    {
    }
}
