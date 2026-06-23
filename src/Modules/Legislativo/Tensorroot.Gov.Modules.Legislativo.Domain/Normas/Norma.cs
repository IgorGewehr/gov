using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Normas;

/// <summary>Identificador forte do agregado <see cref="Norma"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct NormaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="NormaId"/>.</returns>
    public static NormaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Norma juridica do municipio/Camara (lei, decreto legislativo, resolucao, emenda a LOM, Lei
/// Organica). Raiz de agregado consultavel: numero/ano/tipo unicos por tenant, ementa, data de
/// promulgacao, texto articulado opcional e vinculo opcional a proposicao de origem. Controla o
/// ciclo de vigencia (em vigor / alterada / revogada) com trilha imutavel append-only para prova
/// juridica/LAI (CF/88 art. 59; Lei Organica Municipal; Regimento Interno).
/// </summary>
public sealed class Norma : AggregateRoot<NormaId>, IMustHaveTenant
{
    /// <summary>Ano minimo plausivel de promulgacao.</summary>
    public const int AnoMinimo = 1900;

    /// <summary>Ano maximo plausivel de promulgacao.</summary>
    public const int AnoMaximo = 2100;

    private readonly List<EventoVigencia> _historicoVigencia = [];

    private Norma()
    {
    }

    private Norma(
        NormaId id,
        Guid tenantId,
        TipoNorma tipo,
        int numero,
        int ano,
        Ementa ementa,
        DateOnly dataPromulgacao,
        string? textoArticulado,
        ProposicaoId? proposicaoOrigemId)
        : base(id)
    {
        TenantId = tenantId;
        Tipo = tipo;
        Numero = numero;
        Ano = ano;
        Ementa = ementa;
        DataPromulgacao = dataPromulgacao;
        TextoArticulado = textoArticulado;
        ProposicaoOrigemId = proposicaoOrigemId;
        SituacaoVigencia = SituacaoVigencia.EmVigor;
        _historicoVigencia.Add(EventoVigencia.Registrar(TipoEventoVigencia.Promulgacao, dataPromulgacao));
    }

    /// <summary>Tenant (Camara Municipal) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Especie da norma.</summary>
    public TipoNorma Tipo { get; private set; }

    /// <summary>Numero da norma (positivo).</summary>
    public int Numero { get; private set; }

    /// <summary>Ano de promulgacao.</summary>
    public int Ano { get; private set; }

    /// <summary>Ementa (resumo do objeto).</summary>
    public Ementa Ementa { get; private set; }

    /// <summary>Data de promulgacao/publicacao.</summary>
    public DateOnly DataPromulgacao { get; private set; }

    /// <summary>Texto articulado (opcional na PoC; campo livre, sem motor de consolidacao).</summary>
    public string? TextoArticulado { get; private set; }

    /// <summary>Proposicao (mesmo modulo) que originou a norma, quando aplicavel.</summary>
    public ProposicaoId? ProposicaoOrigemId { get; private set; }

    /// <summary>Situacao do ciclo de vigencia.</summary>
    public SituacaoVigencia SituacaoVigencia { get; private set; }

    /// <summary>Data de revogacao (definida ao revogar).</summary>
    public DateOnly? DataRevogacao { get; private set; }

    /// <summary>Norma que revogou esta (quando aplicavel).</summary>
    public NormaId? NormaRevogadoraId { get; private set; }

    /// <summary>Trilha imutavel (append-only) de eventos de vigencia.</summary>
    public IReadOnlyList<EventoVigencia> HistoricoVigencia => _historicoVigencia;

    /// <summary>Indica se a norma esta em estado terminal de vigencia (Revogada).</summary>
    public bool Terminal => SituacaoVigencia == SituacaoVigencia.Revogada;

    /// <summary>
    /// Promulga (cadastra) uma nova norma em <see cref="SituacaoVigencia.EmVigor"/> com 1 evento
    /// <see cref="TipoEventoVigencia.Promulgacao"/> no historico — N-1, N-3.
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="tipo">Especie da norma.</param>
    /// <param name="numero">Numero (positivo).</param>
    /// <param name="ano">Ano de promulgacao (plausivel).</param>
    /// <param name="ementa">Ementa (nao vazia).</param>
    /// <param name="dataPromulgacao">Data de promulgacao.</param>
    /// <param name="textoArticulado">Texto articulado (opcional).</param>
    /// <param name="proposicaoOrigemId">Proposicao de origem (opcional).</param>
    /// <returns>Nova <see cref="Norma"/> em vigor.</returns>
    /// <exception cref="ArgumentException">Se numero, ano ou tipo forem invalidos.</exception>
    public static Norma Promulgar(
        Guid tenantId,
        TipoNorma tipo,
        int numero,
        int ano,
        Ementa ementa,
        DateOnly dataPromulgacao,
        string? textoArticulado = null,
        ProposicaoId? proposicaoOrigemId = null)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de norma invalido.", nameof(tipo));
        }

        if (numero <= 0)
        {
            throw new ArgumentException("Numero da norma deve ser positivo.", nameof(numero));
        }

        if (ano is < AnoMinimo or > AnoMaximo)
        {
            throw new ArgumentException("Ano da norma implausivel.", nameof(ano));
        }

        var texto = string.IsNullOrWhiteSpace(textoArticulado) ? null : textoArticulado.Trim();

        var norma = new Norma(
            NormaId.New(),
            tenantId,
            tipo,
            numero,
            ano,
            ementa,
            dataPromulgacao,
            texto,
            proposicaoOrigemId);

        norma.RaiseDomainEvent(new NormaPromulgada(norma.Id, tipo, numero, ano));
        return norma;
    }

    /// <summary>
    /// Revoga a norma (a partir de <see cref="SituacaoVigencia.EmVigor"/> ou
    /// <see cref="SituacaoVigencia.Alterada"/>); passa a <see cref="SituacaoVigencia.Revogada"/>
    /// (terminal), registra o evento e emite <see cref="NormaRevogada"/> — N-4.
    /// </summary>
    /// <param name="dataRevogacao">Data da revogacao (nao anterior a promulgacao).</param>
    /// <param name="normaRevogadoraId">Norma revogadora (opcional).</param>
    /// <param name="observacao">Observacao (opcional).</param>
    /// <exception cref="InvalidOperationException">Se a norma ja estiver revogada.</exception>
    /// <exception cref="ArgumentException">Se a data de revogacao for anterior a promulgacao.</exception>
    public void Revogar(DateOnly dataRevogacao, NormaId? normaRevogadoraId = null, string? observacao = null)
    {
        if (Terminal)
        {
            throw new InvalidOperationException("Norma ja revogada nao admite nova revogacao.");
        }

        if (dataRevogacao < DataPromulgacao)
        {
            throw new ArgumentException("Data de revogacao nao pode ser anterior a promulgacao.", nameof(dataRevogacao));
        }

        // Auto-referencia na trilha juridica e invalida: uma norma nao pode revogar a si mesma.
        if (normaRevogadoraId == Id)
        {
            throw new ArgumentException("A norma revogadora nao pode ser a propria norma.", nameof(normaRevogadoraId));
        }

        SituacaoVigencia = SituacaoVigencia.Revogada;
        DataRevogacao = dataRevogacao;
        NormaRevogadoraId = normaRevogadoraId;
        _historicoVigencia.Add(EventoVigencia.Registrar(TipoEventoVigencia.Revogacao, dataRevogacao, normaRevogadoraId, observacao));
        RaiseDomainEvent(new NormaRevogada(Id, dataRevogacao));
    }

    /// <summary>
    /// Registra uma alteracao por norma posterior (se nao <see cref="SituacaoVigencia.Revogada"/>);
    /// passa a <see cref="SituacaoVigencia.Alterada"/> (nao terminal) e faz append no historico — N-5.
    /// </summary>
    /// <param name="dataReferencia">Data da alteracao.</param>
    /// <param name="normaAlteradoraId">Norma que altera esta.</param>
    /// <param name="observacao">Observacao (opcional).</param>
    /// <exception cref="InvalidOperationException">Se a norma estiver revogada.</exception>
    public void RegistrarAlteracao(DateOnly dataReferencia, NormaId normaAlteradoraId, string? observacao = null)
    {
        if (Terminal)
        {
            throw new InvalidOperationException("Norma revogada nao admite registro de alteracao.");
        }

        // Espelha Revogar: a alteracao nao pode ser anterior a promulgacao (data implausivel na trilha).
        if (dataReferencia < DataPromulgacao)
        {
            throw new ArgumentException("Data de alteracao nao pode ser anterior a promulgacao.", nameof(dataReferencia));
        }

        // Auto-referencia invalida: uma norma nao pode alterar a si mesma.
        if (normaAlteradoraId == Id)
        {
            throw new ArgumentException("A norma alteradora nao pode ser a propria norma.", nameof(normaAlteradoraId));
        }

        SituacaoVigencia = SituacaoVigencia.Alterada;
        _historicoVigencia.Add(EventoVigencia.Registrar(TipoEventoVigencia.Alteracao, dataReferencia, normaAlteradoraId, observacao));
    }

    /// <summary>
    /// Vincula a proposicao de origem (idempotente; so permitido enquanto o vinculo for nulo) — N-6.
    /// </summary>
    /// <param name="proposicaoId">Proposicao de origem.</param>
    public void VincularProposicaoOrigem(ProposicaoId proposicaoId)
    {
        if (ProposicaoOrigemId is not null)
        {
            return; // N-6: idempotente — nao sobrescreve vinculo existente.
        }

        ProposicaoOrigemId = proposicaoId;
    }
}
