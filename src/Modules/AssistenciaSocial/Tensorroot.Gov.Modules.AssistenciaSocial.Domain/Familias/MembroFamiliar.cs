using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;

/// <summary>Identificador forte da entidade-filha <see cref="MembroFamiliar"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MembroFamiliarId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MembroFamiliarId"/>.</returns>
    public static MembroFamiliarId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Relacao de parentesco do membro com o responsavel familiar.</summary>
public enum Parentesco
{
    /// <summary>Responsavel pela unidade familiar (RF).</summary>
    ResponsavelFamiliar = 1,

    /// <summary>Conjuge/companheiro(a).</summary>
    Conjuge = 2,

    /// <summary>Filho(a).</summary>
    Filho = 3,

    /// <summary>Demais membros do nucleo familiar.</summary>
    Outro = 9,
}

/// <summary>
/// Membro do nucleo familiar (entidade-filha da raiz <see cref="Familia"/>): pessoa que compoe a
/// familia, com renda individual declarada, parentesco e condicoes (inclusive dado sensivel
/// <see cref="EhPcd"/> — deficiencia, art. 11 LGPD; minimizacao e finalidade).
/// </summary>
public sealed class MembroFamiliar : Entity<MembroFamiliarId>
{
    private MembroFamiliar()
    {
    }

    private MembroFamiliar(
        MembroFamiliarId id,
        Cpf cpf,
        Parentesco parentesco,
        DateOnly dataNascimento,
        ValorMonetario rendaIndividual,
        bool ehPcd)
        : base(id)
    {
        Cpf = cpf;
        Parentesco = parentesco;
        DataNascimento = dataNascimento;
        RendaIndividual = rendaIndividual;
        EhPcd = ehPcd;
    }

    /// <summary>CPF do membro.</summary>
    public Cpf Cpf { get; private set; } = default!;

    /// <summary>Relacao com o responsavel familiar.</summary>
    public Parentesco Parentesco { get; private set; }

    /// <summary>Data de nascimento (para apuracao de idade: crianca/adolescente, idoso maior ou igual a 65).</summary>
    public DateOnly DataNascimento { get; private set; }

    /// <summary>Renda declarada do membro (compoe a renda familiar total).</summary>
    public ValorMonetario RendaIndividual { get; private set; } = default!;

    /// <summary>Indicador de pessoa com deficiencia (dado sensivel — art. 11 LGPD).</summary>
    public bool EhPcd { get; private set; }

    /// <summary>Registra um membro do nucleo familiar.</summary>
    /// <param name="cpf">CPF valido do membro.</param>
    /// <param name="parentesco">Relacao de parentesco.</param>
    /// <param name="dataNascimento">Data de nascimento.</param>
    /// <param name="rendaIndividual">Renda individual declarada.</param>
    /// <param name="ehPcd">Indicador de pessoa com deficiencia.</param>
    /// <returns>Novo <see cref="MembroFamiliar"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o CPF ou a renda forem nulos.</exception>
    public static MembroFamiliar Registrar(
        Cpf cpf,
        Parentesco parentesco,
        DateOnly dataNascimento,
        ValorMonetario rendaIndividual,
        bool ehPcd)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        ArgumentNullException.ThrowIfNull(rendaIndividual);
        return new MembroFamiliar(MembroFamiliarId.New(), cpf, parentesco, dataNascimento, rendaIndividual, ehPcd);
    }

    /// <summary>Calcula a idade do membro na data de referencia informada.</summary>
    /// <param name="referencia">Data de referencia.</param>
    /// <returns>Idade em anos completos.</returns>
    public int IdadeEm(DateOnly referencia)
    {
        var idade = referencia.Year - DataNascimento.Year;
        if (referencia < DataNascimento.AddYears(idade))
        {
            idade--;
        }

        return idade;
    }
}
