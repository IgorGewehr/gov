using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="RegistroMensalAtendimento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroMensalAtendimentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroMensalAtendimentoId"/>.</returns>
    public static RegistroMensalAtendimentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Identificador forte de <see cref="LinhaRmaServico"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LinhaRmaServicoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LinhaRmaServicoId"/>.</returns>
    public static LinhaRmaServicoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Linha consolidada do RMA por <b>servico</b> (PAIF/PAEFI/SCFV) — quantidade de atendimentos do servico
/// na competencia. Entidade-filha (owned) do agregado <see cref="RegistroMensalAtendimento"/>: nao
/// implementa <c>IMustHaveTenant</c> (isolamento herdado do dono via FK).
/// </summary>
public sealed class LinhaRmaServico : Entity<LinhaRmaServicoId>
{
    private LinhaRmaServico()
    {
    }

    private LinhaRmaServico(LinhaRmaServicoId id, RegistroMensalAtendimentoId rmaId, TipoServico servico, int quantidade)
        : base(id)
    {
        RmaId = rmaId;
        Servico = servico;
        Quantidade = quantidade;
    }

    /// <summary>RMA ao qual a linha pertence.</summary>
    public RegistroMensalAtendimentoId RmaId { get; private set; }

    /// <summary>Servico socioassistencial consolidado (PAIF/PAEFI/SCFV).</summary>
    public TipoServico Servico { get; private set; }

    /// <summary>Quantidade de atendimentos do servico na competencia (&gt;= 0).</summary>
    public int Quantidade { get; private set; }

    internal static LinhaRmaServico Criar(RegistroMensalAtendimentoId rmaId, TipoServico servico, int quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantidade);
        return new LinhaRmaServico(LinhaRmaServicoId.New(), rmaId, servico, quantidade);
    }
}

/// <summary>Situacao do fechamento do RMA na competencia.</summary>
public enum SituacaoRma
{
    /// <summary>Aberto/consolidando — admite reconsolidacao a partir do prontuario.</summary>
    Aberto = 1,

    /// <summary>Fechado — competencia selada para envio ao MDS (terminal; auditavel).</summary>
    Fechado = 2,
}

/// <summary>
/// <b>A-2 — Registro Mensal de Atendimentos (RMA).</b> Consolidacao MENSAL dos atendimentos de uma
/// unidade (CRAS/CREAS/Centro POP) por <b>(Unidade, Competencia)</b>, <b>gerada a partir do Prontuario
/// SUAS/atendimentos ja existentes</b> (evita dupla digitacao) e fechavel/auditavel. E a base do envio ao
/// <b>RMA/SAGI do MDS</b> (mensal, ate 30 dias apos o mes — Res. CIT 4/2011 e 20/2013). Os dados sao
/// agregados (volumes por servico) — NAO trafega dado sigiloso identificavel do prontuario (LGPD art. 11).
/// <para>
/// Raiz de agregado; as <see cref="LinhaRmaServico"/> sao entidades-filhas (consolidacao por PAIF/PAEFI/
/// SCFV). A reconsolidacao a partir do prontuario e idempotente enquanto a competencia esta
/// <see cref="SituacaoRma.Aberto"/>; o fechamento sela a competencia.
/// </para>
/// // TODO(validar-oficial): o conjunto exato de campos por questionario (CRAS/CREAS/POP) e o canal/API
/// do RMA/SAGI dependem do Manual RMA vigente (pesquisa-assistencia §2) — a consolidacao por servico ja
/// deriva fielmente do prontuario.
/// </summary>
public sealed class RegistroMensalAtendimento : AggregateRoot<RegistroMensalAtendimentoId>, IMustHaveTenant
{
    private readonly List<LinhaRmaServico> _linhas = [];

    private RegistroMensalAtendimento()
    {
    }

    private RegistroMensalAtendimento(
        RegistroMensalAtendimentoId id,
        Guid tenantId,
        Guid unidadeAtendimentoId,
        TipoUnidadeAtendimento tipoUnidade,
        Competencia competencia)
        : base(id)
    {
        TenantId = tenantId;
        UnidadeAtendimentoId = unidadeAtendimentoId;
        TipoUnidade = tipoUnidade;
        Competencia = competencia;
        Situacao = SituacaoRma.Aberto;
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Unidade (CRAS/CREAS/Centro POP) consolidada.</summary>
    public Guid UnidadeAtendimentoId { get; private set; }

    /// <summary>Tipo da unidade (define os servicos esperados: PAIF=CRAS, PAEFI=CREAS).</summary>
    public TipoUnidadeAtendimento TipoUnidade { get; private set; }

    /// <summary>Competencia (ano/mes) de referencia.</summary>
    public Competencia Competencia { get; private set; }

    /// <summary>Situacao do fechamento (Aberto/Fechado).</summary>
    public SituacaoRma Situacao { get; private set; }

    /// <summary>Data/hora (UTC) do fechamento da competencia (nulo enquanto aberto).</summary>
    public DateTime? FechadoEmUtc { get; private set; }

    /// <summary>Linhas consolidadas por servico (PAIF/PAEFI/SCFV).</summary>
    public IReadOnlyCollection<LinhaRmaServico> Linhas => _linhas.AsReadOnly();

    /// <summary>Total de atendimentos consolidados na competencia (soma das linhas).</summary>
    public int TotalAtendimentos => _linhas.Sum(l => l.Quantidade);

    /// <summary>Abre o RMA de uma unidade numa competencia (sem linhas; consolida-se a partir do prontuario).</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="unidadeAtendimentoId">Unidade consolidada (obrigatoria).</param>
    /// <param name="tipoUnidade">Tipo da unidade (CRAS/CREAS/Centro POP).</param>
    /// <param name="competencia">Competencia de referencia (valida).</param>
    /// <returns>Novo <see cref="RegistroMensalAtendimento"/> em <see cref="SituacaoRma.Aberto"/>.</returns>
    /// <exception cref="ArgumentException">Se a unidade for vazia ou a competencia invalida.</exception>
    public static RegistroMensalAtendimento Abrir(
        Guid tenantId,
        Guid unidadeAtendimentoId,
        TipoUnidadeAtendimento tipoUnidade,
        Competencia competencia)
    {
        if (unidadeAtendimentoId == Guid.Empty)
        {
            throw new ArgumentException("Unidade de atendimento e obrigatoria no RMA.", nameof(unidadeAtendimentoId));
        }

        if (!competencia.EhValida())
        {
            throw new ArgumentException("Competencia (ano/mes) do RMA e obrigatoria e valida.", nameof(competencia));
        }

        return new RegistroMensalAtendimento(RegistroMensalAtendimentoId.New(), tenantId, unidadeAtendimentoId, tipoUnidade, competencia);
    }

    /// <summary>
    /// (Re)consolida o RMA a partir das contagens por servico derivadas do Prontuario/atendimentos da
    /// unidade na competencia — substitui as linhas anteriores (idempotente). So enquanto Aberto.
    /// </summary>
    /// <param name="contagensPorServico">Quantidade de atendimentos por servico (PAIF/PAEFI/SCFV) no mes.</param>
    /// <exception cref="ArgumentNullException">Se as contagens forem nulas.</exception>
    /// <exception cref="InvalidOperationException">Se o RMA ja estiver fechado.</exception>
    public void ConsolidarDoProntuario(IReadOnlyDictionary<TipoServico, int> contagensPorServico)
    {
        ArgumentNullException.ThrowIfNull(contagensPorServico);
        GarantirAberto();

        _linhas.Clear();
        foreach (var (servico, quantidade) in contagensPorServico.OrderBy(p => p.Key))
        {
            _linhas.Add(LinhaRmaServico.Criar(Id, servico, quantidade));
        }
    }

    /// <summary>Quantidade consolidada de um servico (0 se ausente).</summary>
    /// <param name="servico">Servico socioassistencial.</param>
    /// <returns>Quantidade consolidada do servico.</returns>
    public int QuantidadeDoServico(TipoServico servico)
        => _linhas.Where(l => l.Servico == servico).Sum(l => l.Quantidade);

    /// <summary>
    /// Fecha a competencia, selando o RMA para envio ao MDS e emitindo
    /// <see cref="RmaFechado"/> (Outbox). Operacao terminal: nao reabre.
    /// </summary>
    /// <param name="fechadoEmUtc">Momento (UTC) do fechamento.</param>
    /// <exception cref="InvalidOperationException">Se o RMA ja estiver fechado.</exception>
    public void Fechar(DateTime fechadoEmUtc)
    {
        GarantirAberto();
        Situacao = SituacaoRma.Fechado;
        FechadoEmUtc = fechadoEmUtc;
        RaiseDomainEvent(new RmaFechado(Id, UnidadeAtendimentoId, Competencia, TotalAtendimentos));
    }

    private void GarantirAberto()
    {
        if (Situacao != SituacaoRma.Aberto)
        {
            throw new InvalidOperationException($"RMA ja fechado nao admite consolidacao/fechamento. Situacao atual: {Situacao}.");
        }
    }
}
