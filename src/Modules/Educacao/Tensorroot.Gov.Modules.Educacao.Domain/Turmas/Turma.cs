using Tensorroot.Gov.Modules.Educacao.Domain.Escolas;
using Tensorroot.Gov.Modules.Educacao.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Turmas;

/// <summary>
/// Turma de uma escola em um ano letivo: etapa, serie, turno e capacidade de vagas, com o contador
/// desnormalizado de matriculados mantido pelo proprio dominio (enturmacao incrementa, encerramento/
/// transferencia decrementa, na mesma transacao da matricula). E a entidade-mestre que destrava a
/// matricula real (picker de turma com vagas disponiveis em vez de GUID digitado) e a invariante de
/// vaga (I-T2: matriculados &lt;= vagas), substituindo o stub do <c>TurmaRepository</c>. Raiz de
/// agregado; nasce <see cref="SituacaoTurma.Planejada"/> via <see cref="Criar"/>.
/// </summary>
public sealed class Turma : AggregateRoot<TurmaId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do texto da serie/etapa.</summary>
    public const int ComprimentoSerie = 60;

    private Turma()
    {
    }

    private Turma(
        TurmaId id,
        Guid tenantId,
        EscolaId escolaId,
        int anoLetivo,
        Etapa etapa,
        string serie,
        Turno turno,
        int vagas)
        : base(id)
    {
        TenantId = tenantId;
        EscolaId = escolaId;
        AnoLetivo = anoLetivo;
        Etapa = etapa;
        Serie = serie;
        Turno = turno;
        Vagas = vagas;
        Matriculados = 0;
        Situacao = SituacaoTurma.Planejada;
    }

    /// <summary>Tenant (ente municipal/rede de ensino) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Escola da turma (FK logica ao agregado Escola — referencia por Id).</summary>
    public EscolaId EscolaId { get; private set; }

    /// <summary>Ano letivo (ex.: 2026).</summary>
    public int AnoLetivo { get; private set; }

    /// <summary>Etapa/modalidade de ensino.</summary>
    public Etapa Etapa { get; private set; }

    /// <summary>Serie/ano (ex.: "1º ano", "Pre II").</summary>
    public string Serie { get; private set; } = default!;

    /// <summary>Turno de funcionamento.</summary>
    public Turno Turno { get; private set; }

    /// <summary>Capacidade de vagas (&gt; 0).</summary>
    public int Vagas { get; private set; }

    /// <summary>Contador desnormalizado de matriculados, mantido pelo dominio (I-T2).</summary>
    public int Matriculados { get; private set; }

    /// <summary>Situacao atual da turma.</summary>
    public SituacaoTurma Situacao { get; private set; }

    /// <summary>Vagas ainda disponiveis (capacidade menos matriculados).</summary>
    public int VagasDisponiveis => Vagas - Matriculados;

    /// <summary>Indica se a turma esta aberta e com vaga (I-T2/I-T3).</summary>
    public bool PossuiVaga => Situacao == SituacaoTurma.Aberta && VagasDisponiveis > 0;

    /// <summary>
    /// Cria uma turma (situacao inicial <see cref="SituacaoTurma.Planejada"/>). Valida vagas, etapa e
    /// turno (I-T1). Emite <see cref="TurmaCriada"/>.
    /// </summary>
    /// <param name="tenantId">Tenant (rede de ensino) dono do registro.</param>
    /// <param name="escolaId">Escola da turma.</param>
    /// <param name="anoLetivo">Ano letivo.</param>
    /// <param name="etapa">Etapa/modalidade.</param>
    /// <param name="serie">Serie/ano (obrigatorio).</param>
    /// <param name="turno">Turno.</param>
    /// <param name="vagas">Capacidade de vagas (&gt; 0).</param>
    /// <returns>Nova <see cref="Turma"/> em situacao Planejada.</returns>
    /// <exception cref="ArgumentException">Se a serie for vazia/excessiva.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se vagas &lt;= 0, ano letivo invalido, etapa/turno invalidos (I-T1).</exception>
    public static Turma Criar(
        Guid tenantId,
        EscolaId escolaId,
        int anoLetivo,
        Etapa etapa,
        string serie,
        Turno turno,
        int vagas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serie);
        var serieNormalizada = serie.Trim();
        if (serieNormalizada.Length > ComprimentoSerie)
        {
            throw new ArgumentException($"Serie excede {ComprimentoSerie} caracteres.", nameof(serie));
        }

        if (vagas <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vagas), "Vagas deve ser maior que zero (I-T1).");
        }

        if (anoLetivo < 1900 || anoLetivo > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(anoLetivo), "Ano letivo invalido.");
        }

        if (!Enum.IsDefined(etapa))
        {
            throw new ArgumentOutOfRangeException(nameof(etapa), "Etapa invalida.");
        }

        if (!Enum.IsDefined(turno))
        {
            throw new ArgumentOutOfRangeException(nameof(turno), "Turno invalido.");
        }

        var turma = new Turma(TurmaId.New(), tenantId, escolaId, anoLetivo, etapa, serieNormalizada, turno, vagas);
        turma.RaiseDomainEvent(new TurmaCriada(turma.Id));
        return turma;
    }

    /// <summary>Abre a turma para enturmacao (Planejada -&gt; Aberta). Emite <see cref="TurmaAberta"/>.</summary>
    /// <exception cref="InvalidOperationException">Se a turma nao estiver Planejada.</exception>
    public void Abrir()
    {
        if (Situacao != SituacaoTurma.Planejada)
        {
            throw new InvalidOperationException($"A abertura exige turma Planejada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoTurma.Aberta;
        RaiseDomainEvent(new TurmaAberta(Id));
    }

    /// <summary>
    /// Encerra a turma (estado terminal). Exige ausencia de matriculados (I-T5), salvo quando o
    /// encerramento e do ano letivo (parametrizavel — <paramref name="encerramentoAnoLetivo"/>).
    /// Emite <see cref="TurmaEncerrada"/>.
    /// </summary>
    /// <param name="encerramentoAnoLetivo">Se verdadeiro, permite encerrar mesmo com matriculados (fim do ano letivo).</param>
    /// <exception cref="InvalidOperationException">Se ja encerrada, ou se houver matriculados sem encerramento de ano (I-T5).</exception>
    public void Encerrar(bool encerramentoAnoLetivo = false)
    {
        if (Situacao == SituacaoTurma.Encerrada)
        {
            throw new InvalidOperationException("Turma ja encerrada.");
        }

        if (Matriculados > 0 && !encerramentoAnoLetivo)
        {
            throw new InvalidOperationException(
                $"Turma com {Matriculados} matriculado(s) so pode encerrar no fim do ano letivo (I-T5).");
        }

        Situacao = SituacaoTurma.Encerrada;
        RaiseDomainEvent(new TurmaEncerrada(Id));
    }

    /// <summary>Ajusta a capacidade de vagas. Nunca abaixo do ja enturmado (I-T3).</summary>
    /// <param name="vagas">Nova capacidade (&gt;= matriculados e &gt; 0).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se as vagas forem &lt;= 0 ou menores que os matriculados (I-T3).</exception>
    /// <exception cref="InvalidOperationException">Se a turma estiver encerrada.</exception>
    public void AjustarVagas(int vagas)
    {
        if (Situacao == SituacaoTurma.Encerrada)
        {
            throw new InvalidOperationException("Turma encerrada nao admite ajuste de vagas.");
        }

        if (vagas <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vagas), "Vagas deve ser maior que zero.");
        }

        if (vagas < Matriculados)
        {
            throw new ArgumentOutOfRangeException(
                nameof(vagas), $"Vagas ({vagas}) nao pode ser menor que os matriculados ({Matriculados}) (I-T3).");
        }

        Vagas = vagas;
    }

    /// <summary>
    /// Incrementa o contador de matriculados (chamado na matricula/enturmacao, mesma transacao).
    /// Exige turma Aberta com vaga (I-T2/I-T3).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a turma nao estiver Aberta ou sem vaga.</exception>
    public void IncrementarMatriculados()
    {
        if (Situacao != SituacaoTurma.Aberta)
        {
            throw new InvalidOperationException($"Enturmacao exige turma Aberta. Situacao atual: {Situacao}.");
        }

        if (VagasDisponiveis <= 0)
        {
            throw new InvalidOperationException("Turma sem vaga (I-T2).");
        }

        Matriculados++;
    }

    /// <summary>
    /// Decrementa o contador de matriculados (chamado no encerramento/transferencia da matricula,
    /// mesma transacao). Nunca abaixo de zero.
    /// </summary>
    public void DecrementarMatriculados()
    {
        if (Matriculados > 0)
        {
            Matriculados--;
        }
    }
}
