using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;

/// <summary>
/// Matricula — vinculo do aluno a uma turma de uma escola na data de referencia do Censo
/// (Matricula Inicial). Raiz de agregado que percorre os estados
/// <c>Ativa -&gt; Transferida/Concluida/Abandono</c> e e a base das matriculas ponderadas do
/// FUNDEB e da Situacao do Aluno (2a etapa do Censo). Nasce valida via a factory
/// <see cref="MatricularAluno"/> e protege as invariantes da maquina de estados.
/// </summary>
public sealed class Matricula : AggregateRoot<MatriculaId>, IMustHaveTenant
{
    /// <summary>Conjunto de situacoes terminais (estados Encerrados) que nao admitem novas transicoes.</summary>
    private static readonly SituacaoMatricula[] SituacoesEncerradas =
    [
        SituacaoMatricula.Transferida,
        SituacaoMatricula.Concluida,
        SituacaoMatricula.Abandono,
    ];

    private Matricula()
    {
    }

    private Matricula(
        MatriculaId id,
        Guid tenantId,
        AlunoId alunoId,
        TurmaId turmaId,
        EscolaId escolaId,
        DateOnly dataReferencia)
        : base(id)
    {
        TenantId = tenantId;
        AlunoId = alunoId;
        TurmaId = turmaId;
        EscolaId = escolaId;
        DataReferencia = dataReferencia;
        Situacao = SituacaoMatricula.Ativa;
        RaiseDomainEvent(new AlunoMatriculado(id, alunoId, turmaId, escolaId));
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Aluno vinculado (sujeito de dados menor — LGPD art. 14).</summary>
    public AlunoId AlunoId { get; private set; }

    /// <summary>Turma de enturmacao do aluno.</summary>
    public TurmaId TurmaId { get; private set; }

    /// <summary>Escola da matricula.</summary>
    public EscolaId EscolaId { get; private set; }

    /// <summary>Data de referencia do Censo (Matricula Inicial).</summary>
    public DateOnly DataReferencia { get; private set; }

    /// <summary>Situacao atual da matricula.</summary>
    public SituacaoMatricula Situacao { get; private set; }

    /// <summary>Rendimento + movimento (2a etapa do Censo); nulo ate o registro.</summary>
    public SituacaoDoAluno? SituacaoDoAluno { get; private set; }

    /// <summary>
    /// Matricula Inicial: cria o vinculo aluno-turma-escola em situacao <see cref="SituacaoMatricula.Ativa"/>
    /// na data de referencia do Censo e emite o evento <see cref="AlunoMatriculado"/> (I-1, I-2, I-4).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="alunoId">Aluno vinculado.</param>
    /// <param name="turmaId">Turma de enturmacao.</param>
    /// <param name="escolaId">Escola da matricula.</param>
    /// <param name="dataReferencia">Data de referencia do Censo (Matricula Inicial).</param>
    /// <returns>Nova <see cref="Matricula"/> em situacao Ativa.</returns>
    public static Matricula MatricularAluno(
        Guid tenantId,
        AlunoId alunoId,
        TurmaId turmaId,
        EscolaId escolaId,
        DateOnly dataReferencia)
    {
        // I-2: aluno/turma/escola/data sao obrigatorios. AlunoId, TurmaId e EscolaId sao
        // identificadores fortes (record struct) — value types nao-anulaveis garantidos pelo tipo;
        // a obrigatoriedade do GUID subjacente e validada no command (NotEmpty no validator).
        return new Matricula(MatriculaId.New(), tenantId, alunoId, turmaId, escolaId, dataReferencia);
    }

    /// <summary>
    /// Transfere o aluno para outra escola/turma, levando a matricula ao estado terminal
    /// <see cref="SituacaoMatricula.Transferida"/> e emitindo <see cref="AlunoTransferido"/> (I-5, I-6, I-9).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoMatricula.Ativa"/>.</exception>
    public void Transferir()
    {
        GarantirAtiva();
        Situacao = SituacaoMatricula.Transferida;
        RaiseDomainEvent(new AlunoTransferido(Id));
    }

    /// <summary>
    /// Encerra a matricula por conclusao da etapa/ano, levando ao estado terminal
    /// <see cref="SituacaoMatricula.Concluida"/> e emitindo <see cref="MatriculaEncerrada"/> (I-5, I-7, I-9).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoMatricula.Ativa"/>.</exception>
    public void Concluir()
    {
        GarantirAtiva();
        Situacao = SituacaoMatricula.Concluida;
        RaiseDomainEvent(new MatriculaEncerrada(Id));
    }

    /// <summary>
    /// Registra o abandono escolar, levando ao estado terminal <see cref="SituacaoMatricula.Abandono"/>
    /// e emitindo <see cref="MatriculaEncerrada"/> (I-5, I-8, I-9).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoMatricula.Ativa"/>.</exception>
    public void RegistrarAbandono()
    {
        GarantirAtiva();
        Situacao = SituacaoMatricula.Abandono;
        RaiseDomainEvent(new MatriculaEncerrada(Id));
    }

    /// <summary>
    /// Registra a Situacao do Aluno (2a etapa do Censo — rendimento + movimento) sem alterar a
    /// situacao da matricula. Pre-requisito do encerramento do ano letivo (I-5, I-10).
    /// </summary>
    /// <param name="rendimento">Rendimento do aluno (aprovado/reprovado).</param>
    /// <param name="movimento">Movimento do aluno (sem movimento/transferido/abandono/falecido).</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoMatricula.Ativa"/>.</exception>
    public void RegistrarSituacaoDoAluno(Rendimento rendimento, Movimento movimento)
    {
        GarantirAtiva();
        SituacaoDoAluno = SituacaoDoAluno.De(rendimento, movimento);
    }

    private void GarantirAtiva()
    {
        if (Situacao != SituacaoMatricula.Ativa)
        {
            throw new InvalidOperationException(
                $"Operacao exige matricula Ativa. Situacao atual: {Situacao}.");
        }
    }
}
