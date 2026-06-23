using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

/// <summary>
/// Entidade-filha da <see cref="DistribuicaoMerenda"/>: a baixa efetiva de um genero alimenticio no
/// dia da distribuicao (genero por Id + quantidade consumida). A baixa real do saldo/lote ocorre no
/// almoxarifado de Patrimonio, acionada por Integration Event (cross-module via Contracts) — aqui se
/// registra o consumo reconhecido (auditavel, imutavel).
/// </summary>
public sealed class ConsumoGenero : Entity<ConsumoGeneroId>
{
    private ConsumoGenero()
    {
    }

    private ConsumoGenero(ConsumoGeneroId id, Guid generoEstoqueId, decimal quantidade, string unidadeMedida)
        : base(id)
    {
        GeneroEstoqueId = generoEstoqueId;
        Quantidade = quantidade;
        UnidadeMedida = unidadeMedida;
    }

    /// <summary>Genero alimenticio (FK logica ao <c>ItemEstoque</c> do almoxarifado de Patrimonio).</summary>
    public Guid GeneroEstoqueId { get; private set; }

    /// <summary>Quantidade consumida (baixa) na unidade do genero.</summary>
    public decimal Quantidade { get; private set; }

    /// <summary>Unidade de medida do genero (kg, L, un).</summary>
    public string UnidadeMedida { get; private set; } = string.Empty;

    /// <summary>Registra a baixa de um genero (quantidade positiva — I-M4).</summary>
    /// <param name="generoEstoqueId">Genero (ItemEstoque) por Id.</param>
    /// <param name="quantidade">Quantidade consumida (&gt; 0).</param>
    /// <param name="unidadeMedida">Unidade de medida.</param>
    /// <returns>Novo <see cref="ConsumoGenero"/>.</returns>
    /// <exception cref="ArgumentException">Se o genero for vazio ou a unidade nao informada.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva (I-M4).</exception>
    internal static ConsumoGenero Registrar(Guid generoEstoqueId, decimal quantidade, string unidadeMedida)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unidadeMedida);
        if (generoEstoqueId == Guid.Empty)
        {
            throw new ArgumentException("Genero (ItemEstoque) obrigatorio.", nameof(generoEstoqueId));
        }

        if (quantidade <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "Quantidade consumida deve ser positiva (I-M4).");
        }

        return new ConsumoGenero(ConsumoGeneroId.New(), generoEstoqueId, quantidade, unidadeMedida.Trim());
    }
}
