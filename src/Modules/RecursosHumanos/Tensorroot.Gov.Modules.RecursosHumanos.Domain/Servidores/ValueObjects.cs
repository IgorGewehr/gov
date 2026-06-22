using System.Globalization;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>Identificador forte do agregado <see cref="Servidor"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ServidorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ServidorId"/>.</returns>
    public static ServidorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Objeto de Valor da matricula do servidor: identificador unico do vinculo no tenant
/// (unico por <c>(TenantId, Matricula)</c>). Nao vazio, ate 20 caracteres.
/// </summary>
public sealed class Matricula : ValueObject
{
    /// <summary>Comprimento maximo da matricula.</summary>
    public const int ComprimentoMaximo = 20;

    private Matricula(string valor) => Valor = valor;

    /// <summary>Texto da matricula (normalizado em maiusculas, sem espacos nas pontas).</summary>
    public string Valor { get; }

    /// <summary>Cria uma matricula valida.</summary>
    /// <param name="valor">Texto da matricula (nao vazio, ate 20 caracteres).</param>
    /// <returns>Instancia valida de <see cref="Matricula"/>.</returns>
    /// <exception cref="ArgumentException">Se vazia ou exceder o comprimento maximo.</exception>
    public static Matricula De(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizada = valor.Trim().ToUpperInvariant();
        if (normalizada.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Matricula excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        return new Matricula(normalizada);
    }

    /// <inheritdoc />
    public override string ToString() => Valor;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}

/// <summary>
/// Objeto de Valor com os dados cadastrais sensiveis do servidor (LGPD): nome civil
/// e data de nascimento. Tratado como dado pessoal sensivel — minimizacao e mascaramento.
/// </summary>
public sealed class DadosPessoais : ValueObject
{
    private DadosPessoais(string nome, DateOnly dataNascimento)
    {
        Nome = nome;
        DataNascimento = dataNascimento;
    }

    /// <summary>Nome civil do servidor.</summary>
    public string Nome { get; }

    /// <summary>Data de nascimento.</summary>
    public DateOnly DataNascimento { get; }

    /// <summary>Cria os dados pessoais do servidor.</summary>
    /// <param name="nome">Nome civil (nao vazio).</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <returns>Instancia valida de <see cref="DadosPessoais"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    public static DadosPessoais Criar(string nome, DateOnly dataNascimento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        return new DadosPessoais(nome.Trim(), dataNascimento);
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Nome} ({DataNascimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)})";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nome;
        yield return DataNascimento;
    }
}
