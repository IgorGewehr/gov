using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>Identificador forte da entidade <see cref="RemessaProtesto"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RemessaProtestoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RemessaProtestoId"/>.</returns>
    public static RemessaProtestoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação da remessa de protesto extrajudicial ao CRA/cartório.</summary>
public enum SituacaoRemessaProtesto
{
    /// <summary>Remessa gerada, aguardando transmissão ao CRA.</summary>
    Gerada = 1,

    /// <summary>Transmitida ao CRA estadual (apresentada).</summary>
    Transmitida = 2,

    /// <summary>Retorno processado (ver <see cref="OcorrenciaProtesto"/>).</summary>
    RetornoProcessado = 3,
}

/// <summary>
/// Ocorrência de retorno do protesto (cartório/CRA → ente). Códigos mínimos relevantes; o leiaute exato
/// do CRA-RS/IEPTB-RS define os códigos numéricos. // TODO(validar-oficial): mapear para os códigos de
/// ocorrência do CRA-RS (CNAB 240/400, XML/WebService CRA21 ou registro 600 bytes — varia por CRA).
/// </summary>
public enum OcorrenciaProtesto
{
    /// <summary>Sem retorno processado ainda.</summary>
    Pendente = 0,

    /// <summary>Protesto lavrado (devedor não pagou no prazo de intimação).</summary>
    Lavrado = 1,

    /// <summary>Pago/retirado no cartório dentro da janela de intimação (extingue o crédito).</summary>
    PagoOuRetirado = 2,

    /// <summary>Protesto sustado (por ordem judicial/administrativa).</summary>
    Sustado = 3,

    /// <summary>Remessa rejeitada pelo CRA (erro de leiaute/dados) — exige reapresentação.</summary>
    Rejeitado = 4,
}

/// <summary>
/// Remessa de protesto extrajudicial de uma CDA ao CRA estadual (Lei 9.492/97 art. 1º p.ú., incluído
/// pela Lei 12.767/2012; STF ADI 5.135; STJ REsp 1.895.557 — dispensa lei local). Modelada como ATO
/// (entidade-filha da <see cref="DividaAtiva"/>): o ERP gera a remessa e processa o RETORNO; a transmissão
/// real ao CRA-RS é integração de convênio à parte, atrás de ACL versionada por CRA. A geração do leiaute
/// fica no adapter (ACL) — o domínio guarda apenas o estado/ocorrência auditável.
/// </summary>
public sealed class RemessaProtesto : Entity<RemessaProtestoId>
{
    private RemessaProtesto()
    {
    }

    private RemessaProtesto(
        RemessaProtestoId id,
        Guid tenantId,
        DividaAtivaId dividaAtivaId,
        string numeroCda,
        string identificadorCra,
        DateOnly dataGeracao)
        : base(id)
    {
        TenantId = tenantId;
        DividaAtivaId = dividaAtivaId;
        NumeroCda = numeroCda;
        IdentificadorCra = identificadorCra;
        DataGeracao = dataGeracao;
        Situacao = SituacaoRemessaProtesto.Gerada;
        Ocorrencia = OcorrenciaProtesto.Pendente;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Dívida ativa (CDA) protestada.</summary>
    public DividaAtivaId DividaAtivaId { get; private set; }

    /// <summary>Número da CDA remetida.</summary>
    public string NumeroCda { get; private set; } = default!;

    /// <summary>Identificador do CRA estadual de destino (ex.: "CRA-RS"). Parametrizável por tenant.</summary>
    public string IdentificadorCra { get; private set; } = default!;

    /// <summary>Data de geração da remessa (data do fato, não o relógio).</summary>
    public DateOnly DataGeracao { get; private set; }

    /// <summary>Data de transmissão ao CRA, quando houver.</summary>
    public DateOnly? DataTransmissao { get; private set; }

    /// <summary>Data do retorno processado, quando houver.</summary>
    public DateOnly? DataRetorno { get; private set; }

    /// <summary>Situação da remessa.</summary>
    public SituacaoRemessaProtesto Situacao { get; private set; }

    /// <summary>Ocorrência de retorno.</summary>
    public OcorrenciaProtesto Ocorrencia { get; private set; }

    /// <summary>Protocolo do CRA/cartório, quando informado no retorno.</summary>
    public string? ProtocoloCartorio { get; private set; }

    /// <summary>Gera uma remessa de protesto para uma CDA emitida.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="dividaAtivaId">Dívida ativa (CDA) a protestar.</param>
    /// <param name="numeroCda">Número da CDA.</param>
    /// <param name="identificadorCra">CRA estadual de destino.</param>
    /// <param name="dataGeracao">Data de geração (data do fato).</param>
    /// <returns>Nova <see cref="RemessaProtesto"/> no estado Gerada.</returns>
    /// <exception cref="ArgumentException">Se número da CDA ou identificador do CRA estiverem vazios.</exception>
    internal static RemessaProtesto Gerar(
        Guid tenantId,
        DividaAtivaId dividaAtivaId,
        string numeroCda,
        string identificadorCra,
        DateOnly dataGeracao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroCda);
        ArgumentException.ThrowIfNullOrWhiteSpace(identificadorCra);
        return new RemessaProtesto(RemessaProtestoId.New(), tenantId, dividaAtivaId, numeroCda.Trim(), identificadorCra.Trim(), dataGeracao);
    }

    /// <summary>Marca a remessa como transmitida ao CRA.</summary>
    /// <param name="dataTransmissao">Data da transmissão.</param>
    /// <exception cref="InvalidOperationException">Se a remessa não estiver no estado Gerada.</exception>
    internal void MarcarTransmitida(DateOnly dataTransmissao)
    {
        if (Situacao != SituacaoRemessaProtesto.Gerada)
        {
            throw new InvalidOperationException($"Só remessas geradas podem ser transmitidas. Situação atual: {Situacao}.");
        }

        DataTransmissao = dataTransmissao;
        Situacao = SituacaoRemessaProtesto.Transmitida;
    }

    /// <summary>Processa o retorno do CRA/cartório (ocorrência + protocolo).</summary>
    /// <param name="ocorrencia">Ocorrência de retorno.</param>
    /// <param name="dataRetorno">Data do retorno.</param>
    /// <param name="protocoloCartorio">Protocolo do cartório (opcional).</param>
    /// <exception cref="InvalidOperationException">Se a remessa ainda não foi transmitida.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a ocorrência for inválida ou Pendente.</exception>
    internal void ProcessarRetorno(OcorrenciaProtesto ocorrencia, DateOnly dataRetorno, string? protocoloCartorio)
    {
        if (Situacao == SituacaoRemessaProtesto.Gerada)
        {
            throw new InvalidOperationException("O retorno só pode ser processado após a transmissão da remessa.");
        }

        if (!Enum.IsDefined(ocorrencia) || ocorrencia == OcorrenciaProtesto.Pendente)
        {
            throw new ArgumentOutOfRangeException(nameof(ocorrencia), ocorrencia, "Ocorrência de retorno inválida.");
        }

        Ocorrencia = ocorrencia;
        DataRetorno = dataRetorno;
        ProtocoloCartorio = string.IsNullOrWhiteSpace(protocoloCartorio) ? null : protocoloCartorio.Trim();
        Situacao = SituacaoRemessaProtesto.RetornoProcessado;
    }
}
