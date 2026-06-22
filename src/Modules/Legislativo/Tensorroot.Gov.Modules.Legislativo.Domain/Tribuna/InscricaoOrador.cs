using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

/// <summary>Identificador forte de uma <see cref="InscricaoOrador"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct InscricaoOradorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="InscricaoOradorId"/>.</returns>
    public static InscricaoOradorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Inscricao de um orador (vereador) na tribuna, com o cronometro autoritativo do servidor: registra
/// inicio/fim e pausas por timestamps (T-8), calcula tempo utilizado e excedente. Trilha append-only:
/// a situacao evolui, registros nao sao apagados (T-9). Entidade do agregado <see cref="TribunaSessao"/>.
/// </summary>
public sealed class InscricaoOrador : Entity<InscricaoOradorId>
{
    private readonly List<PausaFala> _pausas = [];

    private InscricaoOrador()
    {
    }

    private InscricaoOrador(
        InscricaoOradorId id,
        VereadorId vereadorId,
        FaseUsoPalavra fase,
        int ordem,
        TimeSpan tempoConcedido)
        : base(id)
    {
        VereadorId = vereadorId;
        Fase = fase;
        Ordem = ordem;
        TempoConcedido = tempoConcedido;
        Situacao = SituacaoInscricao.Inscrito;
    }

    /// <summary>Vereador inscrito.</summary>
    public VereadorId VereadorId { get; private set; }

    /// <summary>Fase de uso da palavra.</summary>
    public FaseUsoPalavra Fase { get; private set; }

    /// <summary>Ordem sequencial na fila (por tribuna).</summary>
    public int Ordem { get; private set; }

    /// <summary>Situacao da inscricao.</summary>
    public SituacaoInscricao Situacao { get; private set; }

    /// <summary>Tempo regimental concedido ao orador.</summary>
    public TimeSpan TempoConcedido { get; private set; }

    /// <summary>Momento (servidor) de inicio da fala.</summary>
    public DateTimeOffset? IniciadoEm { get; private set; }

    /// <summary>Momento (servidor) de encerramento da fala.</summary>
    public DateTimeOffset? EncerradoEm { get; private set; }

    /// <summary>Momento de inicio da pausa em curso (null quando nao pausado).</summary>
    public DateTimeOffset? PausaIniciadaEm { get; private set; }

    /// <summary>Intervalos de pausa concluidos.</summary>
    public IReadOnlyList<PausaFala> Pausas => _pausas;

    /// <summary>Numero de apartes (interrupcoes) — contagem opcional, sem cronometro proprio.</summary>
    public int Apartes { get; private set; }

    /// <summary>Indica se a fala esta pausada.</summary>
    public bool Pausada => PausaIniciadaEm is not null;

    /// <summary>Soma das pausas concluidas.</summary>
    public TimeSpan TotalPausas => _pausas.Aggregate(TimeSpan.Zero, (acumulado, pausa) => acumulado + pausa.Duracao);

    /// <summary>Tempo efetivamente utilizado (apos encerrar): <c>(EncerradoEm - IniciadoEm) - pausas</c>.</summary>
    public TimeSpan? TempoUtilizado =>
        IniciadoEm is { } inicio && EncerradoEm is { } fim
            ? (fim - inicio) - TotalPausas
            : null;

    /// <summary>Tempo que excedeu o concedido (>= zero), apos encerrar.</summary>
    public TimeSpan? Excedente =>
        TempoUtilizado is { } usado
            ? (usado > TempoConcedido ? usado - TempoConcedido : TimeSpan.Zero)
            : null;

    /// <summary>Cria uma inscricao em <see cref="SituacaoInscricao.Inscrito"/> (T-2).</summary>
    /// <param name="vereadorId">Vereador inscrito.</param>
    /// <param name="fase">Fase de uso da palavra.</param>
    /// <param name="ordem">Ordem sequencial.</param>
    /// <param name="tempoConcedido">Tempo regimental concedido.</param>
    /// <returns>Nova <see cref="InscricaoOrador"/>.</returns>
    public static InscricaoOrador Criar(VereadorId vereadorId, FaseUsoPalavra fase, int ordem, TimeSpan tempoConcedido)
        => new(InscricaoOradorId.New(), vereadorId, fase, ordem, tempoConcedido);

    /// <summary>Inicia a fala (so se <see cref="SituacaoInscricao.Inscrito"/>); set <see cref="IniciadoEm"/> — T-4.</summary>
    /// <param name="momento">Timestamp do servidor.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver inscrito.</exception>
    public void IniciarFala(DateTimeOffset momento)
    {
        if (Situacao != SituacaoInscricao.Inscrito)
        {
            throw new InvalidOperationException($"Inicio de fala exige inscricao Inscrito. Situacao atual: {Situacao}.");
        }

        IniciadoEm = momento;
        Situacao = SituacaoInscricao.EmUso;
    }

    /// <summary>Pausa a fala em curso (T-5).</summary>
    /// <param name="momento">Timestamp do servidor.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver em uso ou ja pausada.</exception>
    public void Pausar(DateTimeOffset momento)
    {
        if (Situacao != SituacaoInscricao.EmUso)
        {
            throw new InvalidOperationException("So e possivel pausar o orador em uso da palavra.");
        }

        if (Pausada)
        {
            throw new InvalidOperationException("Fala ja esta pausada.");
        }

        PausaIniciadaEm = momento;
    }

    /// <summary>Retoma a fala pausada, acumulando o intervalo (T-5).</summary>
    /// <param name="momento">Timestamp do servidor.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver pausada.</exception>
    public void Retomar(DateTimeOffset momento)
    {
        if (PausaIniciadaEm is not { } inicio)
        {
            throw new InvalidOperationException("Fala nao esta pausada.");
        }

        _pausas.Add(PausaFala.De(inicio, momento));
        PausaIniciadaEm = null;
    }

    /// <summary>Encerra a fala (so se em uso); fecha pausa pendente; passa a Concluido — T-6.</summary>
    /// <param name="momento">Timestamp do servidor.</param>
    /// <exception cref="InvalidOperationException">Se nao estiver em uso.</exception>
    public void EncerrarFala(DateTimeOffset momento)
    {
        if (Situacao != SituacaoInscricao.EmUso)
        {
            throw new InvalidOperationException("So e possivel encerrar o orador em uso da palavra.");
        }

        if (PausaIniciadaEm is { } inicioPausa)
        {
            _pausas.Add(PausaFala.De(inicioPausa, momento));
            PausaIniciadaEm = null;
        }

        EncerradoEm = momento;
        Situacao = SituacaoInscricao.Concluido;
    }

    /// <summary>Cancela a inscricao (so se ainda Inscrito) — T-7.</summary>
    /// <exception cref="InvalidOperationException">Se ja falou ou esta falando.</exception>
    public void Cancelar()
    {
        if (Situacao != SituacaoInscricao.Inscrito)
        {
            throw new InvalidOperationException("So e possivel cancelar inscricao ainda nao iniciada.");
        }

        Situacao = SituacaoInscricao.Cancelado;
    }

    /// <summary>Registra um aparte (contagem opcional).</summary>
    public void RegistrarAparte()
    {
        if (Situacao != SituacaoInscricao.EmUso)
        {
            throw new InvalidOperationException("Aparte so durante a fala em uso.");
        }

        Apartes++;
    }
}
