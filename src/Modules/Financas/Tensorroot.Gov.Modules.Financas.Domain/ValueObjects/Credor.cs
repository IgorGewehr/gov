using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

/// <summary>Natureza jurídica do credor.</summary>
public enum TipoPessoa
{
    /// <summary>Pessoa física (CPF).</summary>
    Fisica = 1,

    /// <summary>Pessoa jurídica (CNPJ).</summary>
    Juridica = 2,
}

/// <summary>
/// Credor de um empenho (fornecedor/beneficiário). Guarda nome, tipo de pessoa
/// e documento (CPF/CNPJ) já validado e normalizado (sem máscara).
/// </summary>
public sealed class Credor : ValueObject
{
    private Credor(string nome, TipoPessoa tipo, string documento)
    {
        Nome = nome;
        Tipo = tipo;
        Documento = documento;
    }

    /// <summary>Nome/razão social do credor.</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Tipo de pessoa.</summary>
    public TipoPessoa Tipo { get; private set; }

    /// <summary>Documento (CPF ou CNPJ) normalizado, sem máscara.</summary>
    public string Documento { get; private set; } = default!;

    /// <summary>Cria um credor pessoa jurídica.</summary>
    /// <param name="nome">Razão social.</param>
    /// <param name="cnpj">CNPJ válido.</param>
    /// <returns>Instância de <see cref="Credor"/>.</returns>
    public static Credor PessoaJuridica(string nome, Cnpj cnpj)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentNullException.ThrowIfNull(cnpj);
        return new Credor(nome.Trim(), TipoPessoa.Juridica, cnpj.Digitos);
    }

    /// <summary>Cria um credor pessoa física.</summary>
    /// <param name="nome">Nome.</param>
    /// <param name="cpf">CPF válido.</param>
    /// <returns>Instância de <see cref="Credor"/>.</returns>
    public static Credor PessoaFisica(string nome, Cpf cpf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentNullException.ThrowIfNull(cpf);
        return new Credor(nome.Trim(), TipoPessoa.Fisica, cpf.Digitos);
    }

    /// <summary>Cria um credor a partir de tipo e documento textual (valida conforme o tipo).</summary>
    /// <param name="nome">Nome/razão social.</param>
    /// <param name="tipo">Tipo de pessoa.</param>
    /// <param name="documento">Documento com ou sem máscara.</param>
    /// <returns>Instância de <see cref="Credor"/>.</returns>
    /// <exception cref="ArgumentException">Se o documento for inválido para o tipo.</exception>
    public static Credor De(string nome, TipoPessoa tipo, string documento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        return tipo switch
        {
            TipoPessoa.Juridica => PessoaJuridica(nome, Cnpj.Create(documento)),
            TipoPessoa.Fisica => PessoaFisica(nome, Cpf.Create(documento)),
            _ => throw new ArgumentException("Tipo de pessoa invalido.", nameof(tipo)),
        };
    }

    /// <inheritdoc />
    public override string ToString() => $"{Nome} ({Documento})";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nome;
        yield return Tipo;
        yield return Documento;
    }
}
