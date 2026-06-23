using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

/// <summary>Identificador forte do agregado <see cref="Sessao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct SessaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="SessaoId"/>.</returns>
    public static SessaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Reuniao plenaria da Camara Municipal (ordinaria ou extraordinaria). Raiz de agregado que
/// controla a lista imutavel de presencas, organiza a Ordem do Dia e instala-se apenas com
/// quorum de instalacao = maioria absoluta dos membros (CF/88 art. 29, Lei Organica e Regimento
/// Interno). Garante trilha imutavel de presencas para prova juridica e LAI.
/// </summary>
public sealed class Sessao : AggregateRoot<SessaoId>, IMustHaveTenant
{
    private readonly List<Presenca> _presencas = [];
    private readonly List<ItemOrdemDoDia> _ordemDoDia = [];

    private Sessao()
    {
    }

    private Sessao(
        SessaoId id,
        Guid tenantId,
        TipoSessao tipo,
        DataHora dataHora,
        int totalMembros)
        : base(id)
    {
        TenantId = tenantId;
        Tipo = tipo;
        DataHora = dataHora;
        TotalMembros = totalMembros;
        Situacao = SituacaoSessao.Agendada;
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Especie da sessao (ordinaria / extraordinaria).</summary>
    public TipoSessao Tipo { get; private set; }

    /// <summary>Momento agendado/realizado da sessao.</summary>
    public DataHora DataHora { get; private set; } = default!;

    /// <summary>Numero de vereadores da Camara (base do quorum — CF art. 29-A, por tenant).</summary>
    public int TotalMembros { get; private set; }

    /// <summary>Situacao atual da sessao.</summary>
    public SituacaoSessao Situacao { get; private set; }

    /// <summary>
    /// Quorum de instalacao = maioria absoluta = <c>TotalMembros / 2 + 1</c> (> 50% dos membros).
    /// Derivado de <see cref="TotalMembros"/>; nao persistido (I-2).
    /// </summary>
    public int QuorumInstalacao => (TotalMembros / 2) + 1;

    /// <summary>Registros de presenca (trilha imutavel, append-only).</summary>
    public IReadOnlyList<Presenca> Presencas => _presencas;

    /// <summary>Proposicoes pautadas para deliberacao (Ordem do Dia).</summary>
    public IReadOnlyList<ItemOrdemDoDia> OrdemDoDia => _ordemDoDia;

    /// <summary>Indica se a sessao esta em estado terminal (Encerrada ou Cancelada) — I-10.</summary>
    public bool Terminal => Situacao is SituacaoSessao.Encerrada or SituacaoSessao.Cancelada;

    /// <summary>
    /// Agenda (cria) uma nova sessao em situacao <see cref="SituacaoSessao.Agendada"/> — I-1.
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="tipo">Especie da sessao.</param>
    /// <param name="dataHora">Momento agendado.</param>
    /// <param name="totalMembros">Numero de vereadores (maior que zero).</param>
    /// <returns>Nova <see cref="Sessao"/> agendada.</returns>
    /// <exception cref="ArgumentNullException">Se <paramref name="dataHora"/> for nulo.</exception>
    /// <exception cref="ArgumentException">Se <paramref name="totalMembros"/> for menor ou igual a zero.</exception>
    public static Sessao Agendar(Guid tenantId, TipoSessao tipo, DataHora dataHora, int totalMembros)
    {
        ArgumentNullException.ThrowIfNull(dataHora);
        if (totalMembros <= 0)
        {
            throw new ArgumentException("Total de membros deve ser positivo.", nameof(totalMembros));
        }

        return new Sessao(SessaoId.New(), tenantId, tipo, dataHora, totalMembros);
    }

    /// <summary>
    /// Inclui uma proposicao na Ordem do Dia (somente com a sessao nao terminal) — I-6.
    /// </summary>
    /// <param name="proposicaoId">Proposicao a pautar.</param>
    /// <exception cref="InvalidOperationException">Se a sessao estiver em estado terminal.</exception>
    public void IncluirNaOrdemDoDia(ProposicaoId proposicaoId)
    {
        GarantirNaoTerminal();
        var ordem = _ordemDoDia.Count + 1;
        _ordemDoDia.Add(ItemOrdemDoDia.Incluir(proposicaoId, ordem));
    }

    /// <summary>
    /// Registra a presenca de um vereador na trilha imutavel (idempotente por
    /// <see cref="VereadorId"/>; duplicata e ignorada) — I-3, I-11.
    /// </summary>
    /// <param name="vereadorId">Vereador presente.</param>
    /// <param name="registradaEm">Momento do registro.</param>
    /// <exception cref="InvalidOperationException">Se a sessao estiver em estado terminal.</exception>
    public void RegistrarPresenca(VereadorId vereadorId, DateTimeOffset registradaEm)
    {
        GarantirNaoTerminal();

        // I-3: idempotente — cada vereador registra presenca uma unica vez.
        if (_presencas.Exists(presenca => presenca.VereadorId == vereadorId))
        {
            return;
        }

        _presencas.Add(Presenca.Registrar(vereadorId, registradaEm));
    }

    /// <summary>
    /// Apura se o quorum de instalacao foi atingido (<c>Presencas &gt;= QuorumInstalacao</c>) e
    /// emite <see cref="QuorumVerificado"/> — I-4.
    /// </summary>
    /// <returns><c>true</c> se o quorum foi atingido; caso contrario, <c>false</c>.</returns>
    public bool VerificarQuorum()
    {
        // B-22: em sessao terminal (Encerrada/Cancelada) a verificacao de quorum nao faz sentido e
        // nao deve emitir QuorumVerificado (evento espurio que polui a trilha/painel).
        GarantirNaoTerminal();

        var atingido = _presencas.Count >= QuorumInstalacao;
        RaiseDomainEvent(new QuorumVerificado(Id, atingido));
        return atingido;
    }

    /// <summary>
    /// Instala (abre) a sessao a partir de <see cref="SituacaoSessao.Agendada"/> e com quorum
    /// atingido; passa a <see cref="SituacaoSessao.Aberta"/> e emite <see cref="SessaoAberta"/> — I-5.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Se a situacao nao for <see cref="SituacaoSessao.Agendada"/> ou se o quorum for insuficiente.
    /// </exception>
    public void Abrir()
    {
        if (Situacao != SituacaoSessao.Agendada)
        {
            throw new InvalidOperationException($"A abertura so ocorre a partir de Agendada. Situacao atual: {Situacao}.");
        }

        if (_presencas.Count < QuorumInstalacao)
        {
            throw new InvalidOperationException(
                $"Quorum de instalacao insuficiente: {_presencas.Count} presentes para um quorum de {QuorumInstalacao}.");
        }

        Situacao = SituacaoSessao.Aberta;
        RaiseDomainEvent(new SessaoAberta(Id));
    }

    /// <summary>Suspende temporariamente a sessao (exige <see cref="SituacaoSessao.Aberta"/>) — I-7.</summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoSessao.Aberta"/>.</exception>
    public void Suspender()
    {
        if (Situacao != SituacaoSessao.Aberta)
        {
            throw new InvalidOperationException($"A suspensao exige sessao Aberta. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoSessao.Suspensa;
    }

    /// <summary>Reabre a sessao suspensa (exige <see cref="SituacaoSessao.Suspensa"/>) — I-7.</summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoSessao.Suspensa"/>.</exception>
    public void Reabrir()
    {
        if (Situacao != SituacaoSessao.Suspensa)
        {
            throw new InvalidOperationException($"A reabertura exige sessao Suspensa. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoSessao.Aberta;
    }

    /// <summary>
    /// Encerra a sessao (exige <see cref="SituacaoSessao.Aberta"/> ou <see cref="SituacaoSessao.Suspensa"/>);
    /// passa a <see cref="SituacaoSessao.Encerrada"/> (terminal) e emite <see cref="SessaoEncerrada"/> — I-8.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for Aberta nem Suspensa.</exception>
    public void Encerrar()
    {
        if (Situacao is not (SituacaoSessao.Aberta or SituacaoSessao.Suspensa))
        {
            throw new InvalidOperationException($"O encerramento exige sessao Aberta ou Suspensa. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoSessao.Encerrada;
        RaiseDomainEvent(new SessaoEncerrada(Id));
    }

    /// <summary>
    /// Cancela a sessao (exige <see cref="SituacaoSessao.Agendada"/>, ex.: ausencia de quorum no
    /// horario); passa a <see cref="SituacaoSessao.Cancelada"/> (terminal) — I-9.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoSessao.Agendada"/>.</exception>
    public void Cancelar()
    {
        if (Situacao != SituacaoSessao.Agendada)
        {
            throw new InvalidOperationException($"O cancelamento exige sessao Agendada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoSessao.Cancelada;
    }

    private void GarantirNaoTerminal()
    {
        if (Terminal)
        {
            throw new InvalidOperationException($"Sessao terminal nao admite novas operacoes. Situacao atual: {Situacao}.");
        }
    }
}
