using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;

/// <summary>Identificador forte do agregado <see cref="DomicilioEletronicoContribuinte"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DomicilioEletronicoContribuinteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DomicilioEletronicoContribuinteId"/>.</returns>
    public static DomicilioEletronicoContribuinteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Domicílio Eletrônico do Contribuinte (DEC): caixa postal fiscal eletrônica que centraliza as
/// comunicações oficiais do fisco municipal ao contribuinte (intimações, notificações de lançamento,
/// avisos). Após a adesão/credenciamento, as comunicações enviadas ao DEC têm efeito legal de
/// intimação pessoal. A CIÊNCIA da mensagem ocorre na consulta pelo contribuinte OU, decorrido o
/// prazo legal de disponibilização sem consulta, presume-se a ciência TÁCITA (modelo análogo ao
/// e-CAC/DTE). A partir da ciência, conta-se o PRAZO de manifestação/pagamento. Paridade com o
/// incumbente SAPI. Domínio rico: o agregado protege seus invariantes (CLAUDE.md §7). Datas são do
/// fato (informadas), nunca do relógio do servidor (CLAUDE.md §16).
/// </summary>
public sealed class DomicilioEletronicoContribuinte : AggregateRoot<DomicilioEletronicoContribuinteId>, IMustHaveTenant
{
    /// <summary>
    /// Prazo padrão, em dias corridos, para a ciência TÁCITA da comunicação eletrônica contado da
    /// disponibilização (sem consulta). Parametrizável por tenant via <see cref="DiasCienciaTacita"/>;
    /// 15 dias é o usual no e-CAC/DTE — nunca hardcoded no cálculo.
    /// </summary>
    public const int DiasCienciaTacitaPadrao = 15;

    private readonly List<MensagemFiscal> _mensagens = [];

    private DomicilioEletronicoContribuinte()
    {
    }

    private DomicilioEletronicoContribuinte(
        DomicilioEletronicoContribuinteId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        DateOnly dataAdesao,
        int diasCienciaTacita)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        DataAdesao = dataAdesao;
        DiasCienciaTacita = diasCienciaTacita;
        Ativo = true;
        RaiseDomainEvent(new DomicilioEletronicoAderido(id, tenantId, contribuinteId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte titular do domicílio eletrônico.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Data de adesão/credenciamento ao DEC (a partir da qual as comunicações têm efeito legal).</summary>
    public DateOnly DataAdesao { get; private set; }

    /// <summary>Prazo (dias corridos) para a ciência tácita contado da disponibilização (parametrizável por tenant).</summary>
    public int DiasCienciaTacita { get; private set; } = DiasCienciaTacitaPadrao;

    /// <summary>Indica se o domicílio está ativo (adesão vigente, não cancelada).</summary>
    public bool Ativo { get; private set; }

    /// <summary>Mensagens fiscais disponibilizadas na caixa.</summary>
    public IReadOnlyCollection<MensagemFiscal> Mensagens => _mensagens;

    /// <summary>Quantidade de mensagens na caixa.</summary>
    public int QuantidadeMensagens => _mensagens.Count;

    /// <summary>Adere o contribuinte ao Domicílio Eletrônico (credenciamento).</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte titular.</param>
    /// <param name="dataAdesao">Data de adesão (data do fato).</param>
    /// <param name="diasCienciaTacita">Prazo em dias para a ciência tácita (parametrizável); padrão 15.</param>
    /// <returns>Novo <see cref="DomicilioEletronicoContribuinte"/> ativo.</returns>
    public static DomicilioEletronicoContribuinte Aderir(
        Guid tenantId,
        ContribuinteId contribuinteId,
        DateOnly dataAdesao,
        int diasCienciaTacita = DiasCienciaTacitaPadrao)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(diasCienciaTacita, 1);
        return new DomicilioEletronicoContribuinte(
            DomicilioEletronicoContribuinteId.New(),
            tenantId,
            contribuinteId,
            dataAdesao,
            diasCienciaTacita);
    }

    /// <summary>
    /// Disponibiliza (envia) uma mensagem fiscal ao domicílio: a partir da disponibilização inicia-se a
    /// contagem para a ciência tácita. A mensagem nasce NÃO LIDA. Só é possível em domicílio ativo.
    /// </summary>
    /// <param name="tipo">Tipo da comunicação (intimação/notificação/aviso).</param>
    /// <param name="assunto">Assunto da mensagem.</param>
    /// <param name="corpo">Corpo da comunicação.</param>
    /// <param name="dataDisponibilizacao">Data de disponibilização (data do fato).</param>
    /// <param name="diasPrazoManifestacao">Prazo (dias) de manifestação/pagamento contado da CIÊNCIA; 0 se sem prazo.</param>
    /// <param name="referenciaExterna">Referência ao ato de origem (ex.: nº do lançamento/CDA), opcional.</param>
    /// <returns>A mensagem disponibilizada.</returns>
    /// <exception cref="InvalidOperationException">Se o domicílio não estiver ativo.</exception>
    public MensagemFiscal Disponibilizar(
        TipoMensagemFiscal tipo,
        string assunto,
        string corpo,
        DateOnly dataDisponibilizacao,
        int diasPrazoManifestacao,
        string? referenciaExterna)
    {
        if (!Ativo)
        {
            throw new InvalidOperationException("Não é possível disponibilizar mensagens num domicílio eletrônico inativo.");
        }

        if (dataDisponibilizacao < DataAdesao)
        {
            throw new ArgumentOutOfRangeException(nameof(dataDisponibilizacao), "A disponibilização não pode anteceder a adesão ao domicílio.");
        }

        var mensagem = MensagemFiscal.Disponibilizar(
            Id,
            tipo,
            assunto,
            corpo,
            dataDisponibilizacao,
            DiasCienciaTacita,
            diasPrazoManifestacao,
            referenciaExterna);
        _mensagens.Add(mensagem);
        RaiseDomainEvent(new MensagemFiscalDisponibilizada(Id, TenantId, ContribuinteId, mensagem.Id, tipo));
        return mensagem;
    }

    /// <summary>
    /// Registra a CIÊNCIA EXPRESSA de uma mensagem pela consulta do contribuinte na data informada
    /// (data do fato). A partir dela conta-se o prazo de manifestação. Idempotente: reabrir não recalcula.
    /// </summary>
    /// <param name="mensagemId">Mensagem consultada.</param>
    /// <param name="dataConsulta">Data da consulta/ciência expressa (data do fato).</param>
    /// <exception cref="InvalidOperationException">Se a mensagem não pertencer à caixa.</exception>
    public void DarCienciaPorConsulta(MensagemFiscalId mensagemId, DateOnly dataConsulta)
    {
        var mensagem = ObterMensagem(mensagemId);
        if (mensagem.RegistrarCienciaExpressa(dataConsulta))
        {
            RaiseDomainEvent(new CienciaMensagemFiscalRegistrada(Id, TenantId, ContribuinteId, mensagem.Id, mensagem.DataCiencia!.Value, Tacita: false));
        }
    }

    /// <summary>
    /// Aplica a CIÊNCIA TÁCITA às mensagens cuja data de ciência tácita já se completou na data de
    /// referência e que ainda não tiveram ciência expressa (presunção legal de intimação). Determinístico
    /// (só datas do fato). Idempotente. Retorna as mensagens que passaram a ter ciência tácita agora.
    /// </summary>
    /// <param name="referencia">Data de referência (data do fato — sem relógio no domínio).</param>
    /// <returns>Mensagens cuja ciência tácita foi registrada nesta passagem.</returns>
    public IReadOnlyList<MensagemFiscal> AplicarCienciaTacita(DateOnly referencia)
    {
        var afetadas = new List<MensagemFiscal>();
        foreach (var mensagem in _mensagens)
        {
            if (mensagem.RegistrarCienciaTacitaSeVencida(referencia))
            {
                afetadas.Add(mensagem);
                RaiseDomainEvent(new CienciaMensagemFiscalRegistrada(Id, TenantId, ContribuinteId, mensagem.Id, mensagem.DataCiencia!.Value, Tacita: true));
            }
        }

        return afetadas;
    }

    /// <summary>Cancela a adesão ao domicílio (desativa): novas mensagens deixam de poder ser disponibilizadas.</summary>
    /// <exception cref="InvalidOperationException">Se o domicílio já estiver inativo.</exception>
    public void CancelarAdesao()
    {
        if (!Ativo)
        {
            throw new InvalidOperationException("O domicílio eletrônico já está inativo.");
        }

        Ativo = false;
        RaiseDomainEvent(new DomicilioEletronicoCancelado(Id, TenantId, ContribuinteId));
    }

    private MensagemFiscal ObterMensagem(MensagemFiscalId mensagemId)
        => _mensagens.FirstOrDefault(m => m.Id == mensagemId)
        ?? throw new InvalidOperationException("A mensagem não pertence a este domicílio eletrônico.");
}
