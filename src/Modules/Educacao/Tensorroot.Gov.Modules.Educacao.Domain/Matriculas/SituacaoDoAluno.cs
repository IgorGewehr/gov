using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

/// <summary>
/// Situacao do Aluno (2a etapa do Censo): rendimento (aprovado/reprovado) combinado
/// ao movimento (sem movimento/transferido/abandono/falecido). Pre-requisito do
/// encerramento do ano letivo (I-10). Objeto de valor imutavel, comparado por igualdade
/// estrutural; persistido como propriedade owned da <see cref="Matricula"/>.
/// </summary>
public sealed class SituacaoDoAluno : ValueObject
{
    private SituacaoDoAluno(Rendimento rendimento, Movimento movimento)
    {
        Rendimento = rendimento;
        Movimento = movimento;
    }

    /// <summary>Rendimento do aluno (aprovado/reprovado).</summary>
    public Rendimento Rendimento { get; }

    /// <summary>Movimento do aluno (sem movimento/transferido/abandono/falecido).</summary>
    public Movimento Movimento { get; }

    /// <summary>Cria a Situacao do Aluno (2a etapa do Censo).</summary>
    /// <param name="rendimento">Rendimento do aluno.</param>
    /// <param name="movimento">Movimento do aluno.</param>
    /// <returns>Nova instancia de <see cref="SituacaoDoAluno"/>.</returns>
    public static SituacaoDoAluno De(Rendimento rendimento, Movimento movimento)
        => new(rendimento, movimento);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Rendimento;
        yield return Movimento;
    }
}
