using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.Modules.Educacao.Domain.Matriculas;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Transporte;

/// <summary>
/// Entidade-filha da <see cref="RotaTransporte"/>: um aluno transportado, com o ponto de embarque.
/// Reusa <see cref="Aluno"/> e <see cref="Matricula"/> por Id (FK logica, mesmo modulo Educacao). O
/// vinculo a matricula (opcional) amarra o transporte ao vinculo escolar vigente para fins de PNATE.
/// </summary>
public sealed class AlunoTransportado : Entity<AlunoTransportadoId>
{
    /// <summary>Comprimento maximo do texto do ponto de embarque.</summary>
    public const int ComprimentoPonto = 200;

    private AlunoTransportado()
    {
    }

    private AlunoTransportado(
        AlunoTransportadoId id,
        AlunoId alunoId,
        MatriculaId? matriculaId,
        string pontoEmbarque)
        : base(id)
    {
        AlunoId = alunoId;
        MatriculaId = matriculaId;
        PontoEmbarque = pontoEmbarque;
        Ativo = true;
    }

    /// <summary>Aluno transportado (FK logica ao agregado Aluno — referencia por Id).</summary>
    public AlunoId AlunoId { get; private set; }

    /// <summary>Matricula vigente do aluno (FK logica opcional ao agregado Matricula).</summary>
    public MatriculaId? MatriculaId { get; private set; }

    /// <summary>Ponto de embarque/desembarque do aluno na rota.</summary>
    public string PontoEmbarque { get; private set; } = string.Empty;

    /// <summary>Indica se o vinculo do aluno a rota esta ativo (desligamento e logico).</summary>
    public bool Ativo { get; private set; }

    /// <summary>Vincula um aluno a rota, com o ponto de embarque (I-R3).</summary>
    /// <param name="alunoId">Aluno (por Id).</param>
    /// <param name="matriculaId">Matricula vigente (opcional, por Id).</param>
    /// <param name="pontoEmbarque">Ponto de embarque (obrigatorio).</param>
    /// <returns>Novo <see cref="AlunoTransportado"/> ativo.</returns>
    /// <exception cref="ArgumentException">Se o aluno for vazio ou o ponto invalido/excessivo (I-R3).</exception>
    internal static AlunoTransportado Vincular(AlunoId alunoId, MatriculaId? matriculaId, string pontoEmbarque)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pontoEmbarque);
        if (alunoId.Value == Guid.Empty)
        {
            throw new ArgumentException("Aluno obrigatorio.", nameof(alunoId));
        }

        var ponto = pontoEmbarque.Trim();
        if (ponto.Length > ComprimentoPonto)
        {
            throw new ArgumentException($"Ponto de embarque excede {ComprimentoPonto} caracteres.", nameof(pontoEmbarque));
        }

        return new AlunoTransportado(AlunoTransportadoId.New(), alunoId, matriculaId, ponto);
    }

    /// <summary>Desliga (logicamente) o aluno da rota.</summary>
    internal void Desligar() => Ativo = false;
}
