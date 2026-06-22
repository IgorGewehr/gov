using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>Identificador forte de um <see cref="Dependente"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DependenteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DependenteId"/>.</returns>
    public static DependenteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Dependente do servidor para fins de Imposto de Renda e beneficios — entidade-filha do
/// agregado <see cref="Servidor"/>, exposta somente atraves da raiz. Dados pessoais
/// sensiveis (LGPD).
/// </summary>
public sealed class Dependente : Entity<DependenteId>
{
    private Dependente()
    {
    }

    private Dependente(DependenteId id, string nome, string parentesco, DateOnly dataNascimento)
        : base(id)
    {
        Nome = nome;
        Parentesco = parentesco;
        DataNascimento = dataNascimento;
    }

    /// <summary>Nome civil do dependente.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Grau de parentesco com o servidor.</summary>
    public string Parentesco { get; private set; } = default!;

    /// <summary>Data de nascimento do dependente.</summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>Registra um novo dependente.</summary>
    /// <param name="nome">Nome civil (nao vazio).</param>
    /// <param name="parentesco">Grau de parentesco (nao vazio).</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <returns>Novo <see cref="Dependente"/>.</returns>
    /// <exception cref="ArgumentException">Se nome/parentesco for vazio.</exception>
    public static Dependente Registrar(string nome, string parentesco, DateOnly dataNascimento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentesco);
        return new Dependente(DependenteId.New(), nome.Trim(), parentesco.Trim(), dataNascimento);
    }
}
