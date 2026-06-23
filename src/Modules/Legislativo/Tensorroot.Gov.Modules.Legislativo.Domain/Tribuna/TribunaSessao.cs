using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Tribuna;

/// <summary>Identificador forte do agregado <see cref="TribunaSessao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TribunaSessaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TribunaSessaoId"/>.</returns>
    public static TribunaSessaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tribuna de uma sessao: gerencia a inscricao de oradores por fase de uso da palavra e o cronometro
/// autoritativo do servidor (iniciar/pausar/retomar/encerrar), garantindo no maximo um orador em uso
/// por vez (T-3). Agregado proprio vinculado a <see cref="SessaoId"/> (mesmo modulo) para nao inchar a
/// Sessao. Tempos derivam de timestamps do servidor (anti-fraude/prova — T-8).
/// </summary>
public sealed class TribunaSessao : AggregateRoot<TribunaSessaoId>, IMustHaveTenant
{
    /// <summary>Tempo padrao de orador quando nao parametrizado (5 minutos).</summary>
    public static readonly TimeSpan TempoPadraoFallback = TimeSpan.FromMinutes(5);

    private readonly List<InscricaoOrador> _inscricoes = [];

    private TribunaSessao()
    {
    }

    private TribunaSessao(TribunaSessaoId id, Guid tenantId, SessaoId sessaoId, TimeSpan tempoPadraoOrador)
        : base(id)
    {
        TenantId = tenantId;
        SessaoId = sessaoId;
        TempoPadraoOrador = tempoPadraoOrador;
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Sessao a que esta tribuna pertence (vinculo interno — mesmo modulo).</summary>
    public SessaoId SessaoId { get; private set; }

    /// <summary>Tempo padrao concedido por orador (parametrizavel por tenant; nao hardcoded).</summary>
    public TimeSpan TempoPadraoOrador { get; private set; }

    /// <summary>Inscricoes de oradores (fila por fase).</summary>
    public IReadOnlyList<InscricaoOrador> Inscricoes => _inscricoes;

    /// <summary>Inscricao do orador atualmente em uso da palavra (no maximo uma — T-3).</summary>
    public InscricaoOrador? OradorEmUso => _inscricoes.Find(i => i.Situacao == SituacaoInscricao.EmUso);

    /// <summary>
    /// Abre a tribuna para uma sessao nao terminal (T-1). O tempo padrao vem da configuracao do tenant
    /// (nunca hardcoded); na ausencia, usa <see cref="TempoPadraoFallback"/>.
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="sessao">Sessao (deve estar nao terminal).</param>
    /// <param name="tempoPadraoOrador">Tempo padrao de orador (parametrizavel).</param>
    /// <returns>Nova <see cref="TribunaSessao"/>.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="sessao"/> for nula.</exception>
    /// <exception cref="InvalidOperationException">Se a sessao estiver em estado terminal.</exception>
    public static TribunaSessao Abrir(Guid tenantId, Sessao sessao, TimeSpan? tempoPadraoOrador = null)
    {
        ArgumentNullException.ThrowIfNull(sessao);
        if (sessao.Terminal)
        {
            throw new InvalidOperationException("Tribuna so pode ser aberta para sessao nao terminal.");
        }

        var tempo = tempoPadraoOrador is { } valor && valor > TimeSpan.Zero ? valor : TempoPadraoFallback;
        return new TribunaSessao(TribunaSessaoId.New(), tenantId, sessao.Id, tempo);
    }

    /// <summary>
    /// Inscreve um orador (idempotente por vereador+fase — T-2): segunda inscricao na mesma fase
    /// retorna a inscricao existente. Define ordem sequencial. Tempo concedido = padrao se omitido.
    /// </summary>
    /// <param name="vereadorId">Vereador a inscrever.</param>
    /// <param name="fase">Fase de uso da palavra.</param>
    /// <param name="tempoConcedido">Tempo concedido (default = <see cref="TempoPadraoOrador"/>).</param>
    /// <returns>A inscricao (nova ou existente, em caso idempotente).</returns>
    public InscricaoOrador Inscrever(VereadorId vereadorId, FaseUsoPalavra fase, TimeSpan? tempoConcedido = null)
    {
        if (!Enum.IsDefined(fase))
        {
            throw new ArgumentException("Fase de uso da palavra invalida.", nameof(fase));
        }

        // T-2 / BUG-6: idempotente apenas sobre inscricao AINDA PENDENTE (Inscrito ou EmUso). Uma
        // inscricao Concluido/Cancelado nao deve ser devolvida: devolver a Concluido fazia IniciarFala
        // lancar ("exige Inscrito") e travava a segunda fala do orador na mesma fase. Re-inscrever apos
        // concluir cria uma NOVA inscricao na mesma fase.
        var existente = _inscricoes.Find(i =>
            i.VereadorId == vereadorId
            && i.Fase == fase
            && i.Situacao is SituacaoInscricao.Inscrito or SituacaoInscricao.EmUso);
        if (existente is not null)
        {
            return existente;
        }

        var tempo = tempoConcedido is { } valor && valor > TimeSpan.Zero ? valor : TempoPadraoOrador;
        var ordem = _inscricoes.Count + 1;
        var inscricao = InscricaoOrador.Criar(vereadorId, fase, ordem, tempo);
        _inscricoes.Add(inscricao);
        return inscricao;
    }

    /// <summary>
    /// Inicia a fala de um orador, garantindo a exclusao mutua: nenhum outro pode estar em uso (T-3, T-4).
    /// Emite <see cref="OradorIniciouFala"/>.
    /// </summary>
    /// <param name="inscricaoId">Inscricao a iniciar.</param>
    /// <param name="momento">Timestamp do servidor.</param>
    /// <exception cref="InvalidOperationException">Se houver outro orador em uso.</exception>
    public void IniciarFala(InscricaoOradorId inscricaoId, DateTimeOffset momento)
    {
        if (OradorEmUso is not null)
        {
            throw new InvalidOperationException("Ja existe um orador em uso da palavra (exclusao mutua).");
        }

        var inscricao = Localizar(inscricaoId);
        inscricao.IniciarFala(momento);
        RaiseDomainEvent(new OradorIniciouFala(Id, inscricaoId, inscricao.VereadorId));
    }

    /// <summary>Pausa a fala em curso (T-5).</summary>
    /// <param name="inscricaoId">Inscricao alvo.</param>
    /// <param name="momento">Timestamp do servidor.</param>
    public void PausarFala(InscricaoOradorId inscricaoId, DateTimeOffset momento)
        => Localizar(inscricaoId).Pausar(momento);

    /// <summary>Retoma a fala pausada (T-5).</summary>
    /// <param name="inscricaoId">Inscricao alvo.</param>
    /// <param name="momento">Timestamp do servidor.</param>
    public void RetomarFala(InscricaoOradorId inscricaoId, DateTimeOffset momento)
        => Localizar(inscricaoId).Retomar(momento);

    /// <summary>Encerra a fala, calcula tempo/excedente e emite <see cref="OradorEncerrouFala"/> — T-6.</summary>
    /// <param name="inscricaoId">Inscricao alvo.</param>
    /// <param name="momento">Timestamp do servidor.</param>
    public void EncerrarFala(InscricaoOradorId inscricaoId, DateTimeOffset momento)
    {
        var inscricao = Localizar(inscricaoId);
        inscricao.EncerrarFala(momento);
        RaiseDomainEvent(new OradorEncerrouFala(
            Id,
            inscricaoId,
            inscricao.VereadorId,
            inscricao.TempoUtilizado ?? TimeSpan.Zero,
            inscricao.Excedente ?? TimeSpan.Zero));
    }

    /// <summary>Cancela uma inscricao ainda nao iniciada (T-7).</summary>
    /// <param name="inscricaoId">Inscricao alvo.</param>
    public void CancelarInscricao(InscricaoOradorId inscricaoId)
        => Localizar(inscricaoId).Cancelar();

    private InscricaoOrador Localizar(InscricaoOradorId inscricaoId)
        => _inscricoes.Find(i => i.Id == inscricaoId)
            ?? throw new InvalidOperationException("Inscricao de orador nao encontrada na tribuna.");
}
