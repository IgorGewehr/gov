using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte de um <see cref="Motorista"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MotoristaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MotoristaId"/>.</returns>
    public static MotoristaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Condutor habilitado vinculado a um veículo da frota; controla a CNH e sua validade
/// (CTB, Lei 9.503/1997). Dado pessoal sob LGPD (nome, CNH, validade).
/// </summary>
public sealed class Motorista : Entity<MotoristaId>
{
    private Motorista()
    {
    }

    private Motorista(
        MotoristaId id,
        string nome,
        string cnh,
        string categoriaCnh,
        DateOnly validadeCnh)
        : base(id)
    {
        Nome = nome;
        Cnh = cnh;
        CategoriaCnh = categoriaCnh;
        ValidadeCnh = validadeCnh;
    }

    /// <summary>Nome do motorista.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Número da Carteira Nacional de Habilitação (CNH).</summary>
    public string Cnh { get; private set; } = default!;

    /// <summary>Categoria da CNH (ex.: A, B, AB, D).</summary>
    public string CategoriaCnh { get; private set; } = default!;

    /// <summary>Data de validade da CNH.</summary>
    public DateOnly ValidadeCnh { get; private set; }

    /// <summary>Indica se a CNH está válida (não vencida) na data informada (I-9).</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns><c>true</c> se a CNH estiver válida em <paramref name="hoje"/>.</returns>
    public bool CnhValidaEm(DateOnly hoje) => ValidadeCnh >= hoje;

    /// <summary>Vincula um novo motorista habilitado, exigindo CNH não vencida (I-9).</summary>
    /// <param name="nome">Nome do motorista.</param>
    /// <param name="cnh">Número da CNH.</param>
    /// <param name="categoriaCnh">Categoria da CNH.</param>
    /// <param name="validadeCnh">Validade da CNH.</param>
    /// <param name="hoje">Data de referência para a checagem de validade.</param>
    /// <returns>Novo <see cref="Motorista"/>.</returns>
    /// <exception cref="ArgumentException">Se nome ou CNH forem vazios.</exception>
    /// <exception cref="InvalidOperationException">Se a CNH estiver vencida.</exception>
    internal static Motorista Vincular(
        string nome,
        string cnh,
        string categoriaCnh,
        DateOnly validadeCnh,
        DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(cnh);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoriaCnh);
        if (validadeCnh < hoje)
        {
            throw new InvalidOperationException(
                $"Motorista com CNH vencida não pode ser designado. Validade: {validadeCnh:yyyy-MM-dd}.");
        }

        return new Motorista(MotoristaId.New(), nome, cnh, categoriaCnh, validadeCnh);
    }
}
