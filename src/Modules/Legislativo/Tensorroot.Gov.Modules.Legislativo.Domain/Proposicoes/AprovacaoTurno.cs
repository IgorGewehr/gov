using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Identificador forte de uma <see cref="AprovacaoTurno"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AprovacaoTurnoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AprovacaoTurnoId"/>.</returns>
    public static AprovacaoTurnoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro imutavel da aprovacao de UM turno de votacao de uma proposicao. Materias de rito
/// qualificado (Emenda a Lei Organica Municipal — CF/88 art. 29, caput; simetria do art. 60 §2º)
/// so se promulgam apos DOIS turnos favoraveis, cada qual com a maioria exigida e observado o
/// intersticio minimo entre eles. Esta entidade materializa cada turno aprovado na trilha do
/// agregado, dando estado ao rito (o turno da votacao deixa de ser dado morto) e
/// permitindo provar juridicamente que ambos os turnos ocorreram em datas/sessoes distintas.
/// </summary>
public sealed class AprovacaoTurno : Entity<AprovacaoTurnoId>
{
    private AprovacaoTurno()
    {
    }

    private AprovacaoTurno(
        AprovacaoTurnoId id,
        ProposicaoId proposicaoId,
        int numero,
        MaioriaProposicao maioriaAtingida,
        DateOnly data)
        : base(id)
    {
        ProposicaoId = proposicaoId;
        Numero = numero;
        MaioriaAtingida = maioriaAtingida;
        Data = data;
    }

    /// <summary>Proposicao a qual o turno se vincula.</summary>
    public ProposicaoId ProposicaoId { get; private set; }

    /// <summary>Numero do turno aprovado (1 = primeiro turno; 2 = segundo turno).</summary>
    public int Numero { get; private set; }

    /// <summary>Maior maioria efetivamente atingida no turno (deve satisfazer a exigida pelo tipo).</summary>
    public MaioriaProposicao MaioriaAtingida { get; private set; }

    /// <summary>Data da deliberacao do turno (base do intersticio para o turno seguinte).</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra a aprovacao de um turno na trilha do agregado.</summary>
    /// <param name="proposicaoId">Proposicao vinculada.</param>
    /// <param name="numero">Numero do turno (1 ou 2).</param>
    /// <param name="maioriaAtingida">Maioria efetivamente atingida no turno.</param>
    /// <param name="data">Data da deliberacao.</param>
    /// <returns>Novo registro de <see cref="AprovacaoTurno"/>.</returns>
    public static AprovacaoTurno Registrar(
        ProposicaoId proposicaoId,
        int numero,
        MaioriaProposicao maioriaAtingida,
        DateOnly data)
        => new(AprovacaoTurnoId.New(), proposicaoId, numero, maioriaAtingida, data);
}
