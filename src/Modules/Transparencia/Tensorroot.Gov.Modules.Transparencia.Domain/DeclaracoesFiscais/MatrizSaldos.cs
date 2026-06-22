using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

/// <summary>Identificador forte de uma <see cref="MatrizSaldos"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MatrizSaldosId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MatrizSaldosId"/>.</returns>
    public static MatrizSaldosId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Matriz de Saldos Contabeis (MSC): conjunto consolidado de saldos contabeis do periodo,
/// composto por <see cref="LinhaContabil"/>. Entidade-filha da raiz <see cref="DeclaracaoFiscal"/>.
/// </summary>
public sealed class MatrizSaldos : Entity<MatrizSaldosId>
{
    private readonly List<LinhaContabil> _linhas = [];

    private MatrizSaldos()
    {
    }

    private MatrizSaldos(MatrizSaldosId id, IEnumerable<LinhaContabil> linhas)
        : base(id)
    {
        _linhas.AddRange(linhas);
    }

    /// <summary>Linhas (somente-leitura) que compoem a matriz.</summary>
    public IReadOnlyCollection<LinhaContabil> Linhas => _linhas;

    /// <summary>Total dos saldos de natureza devedora.</summary>
    public ValorMonetario TotalDebitos
        => _linhas
            .Where(linha => linha.NaturezaSaldo == NaturezaSaldo.Devedor)
            .Aggregate(ValorMonetario.Zero, (acumulado, linha) => acumulado.Somar(linha.Valor));

    /// <summary>Total dos saldos de natureza credora.</summary>
    public ValorMonetario TotalCreditos
        => _linhas
            .Where(linha => linha.NaturezaSaldo == NaturezaSaldo.Credor)
            .Aggregate(ValorMonetario.Zero, (acumulado, linha) => acumulado.Somar(linha.Valor));

    /// <summary>Indica se a matriz esta balanceada (partidas dobradas, PCASP): debitos == creditos.</summary>
    public bool EstaBalanceada => TotalDebitos.Equals(TotalCreditos);

    /// <summary>Monta uma matriz de saldos a partir das linhas informadas.</summary>
    /// <param name="linhas">Linhas contabeis (saldos recebidos).</param>
    /// <returns>Nova <see cref="MatrizSaldos"/>.</returns>
    /// <exception cref="ArgumentNullException">Se a colecao de linhas for nula.</exception>
    public static MatrizSaldos Montar(IEnumerable<LinhaContabil> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);
        return new MatrizSaldos(MatrizSaldosId.New(), linhas);
    }
}
