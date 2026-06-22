using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

/// <summary>
/// Dados civis do paciente (owned type): nome, nome social, data de nascimento, sexo e CPF.
/// Mapeado como owned type do agregado <see cref="Paciente"/>.
/// </summary>
public readonly record struct Identificacao
{
    /// <summary>Comprimento maximo do nome.</summary>
    public const int ComprimentoNome = 120;

    /// <summary>Cria os dados civis do paciente.</summary>
    /// <param name="nome">Nome civil (obrigatorio).</param>
    /// <param name="dataNascimento">Data de nascimento (nao futura).</param>
    /// <param name="sexo">Sexo.</param>
    /// <param name="nomeSocial">Nome social (opcional).</param>
    /// <param name="cpf">CPF (opcional).</param>
    /// <param name="hoje">Data corrente para a guarda de nascimento nao futuro.</param>
    /// <exception cref="ArgumentException">Se o nome for vazio ou exceder o limite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a data de nascimento for futura.</exception>
    public Identificacao(
        string nome,
        DateOnly dataNascimento,
        Sexo sexo,
        string? nomeSocial,
        Cpf? cpf,
        DateOnly hoje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        var normalizado = nome.Trim();
        if (normalizado.Length > ComprimentoNome)
        {
            throw new ArgumentException($"Nome do paciente excede {ComprimentoNome} caracteres.", nameof(nome));
        }

        if (dataNascimento > hoje)
        {
            throw new ArgumentOutOfRangeException(nameof(dataNascimento), "Data de nascimento nao pode ser futura.");
        }

        Nome = normalizado;
        DataNascimento = dataNascimento;
        Sexo = sexo;
        NomeSocial = string.IsNullOrWhiteSpace(nomeSocial) ? null : nomeSocial.Trim();
        Cpf = cpf;
    }

    /// <summary>Nome civil do paciente.</summary>
    public string Nome { get; }

    /// <summary>Nome social, quando informado.</summary>
    public string? NomeSocial { get; }

    /// <summary>Data de nascimento.</summary>
    public DateOnly DataNascimento { get; }

    /// <summary>Sexo.</summary>
    public Sexo Sexo { get; }

    /// <summary>CPF, quando informado.</summary>
    public Cpf? Cpf { get; }
}
