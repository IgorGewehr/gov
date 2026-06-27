using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

/// <summary>Identificador forte do agregado <see cref="ComunicacaoAcidente"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ComunicacaoAcidenteId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ComunicacaoAcidenteId"/>.</returns>
    public static ComunicacaoAcidenteId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Comunicacao de Acidente de Trabalho (CAT) — base do evento eSocial <b>S-2210</b>. Obrigacao legal de
/// comunicar acidente/doenca ocupacional ao orgao previdenciario ate o 1o dia util seguinte (Lei 8.213/1991
/// art. 22), imediatamente em caso de obito. Modela a data/hora do acidente, o tipo (tipico/doenca/trajeto),
/// a CID, a parte do corpo lesionada e o agente causador, a hipotese de obito e o vinculo a CAT de origem
/// (reabertura/obito apontam a CAT inicial). Raiz de agregado <see cref="IMustHaveTenant"/>; nasce valida
/// via <see cref="Comunicar"/>. // TODO(validar-oficial): dominios das Tabelas 25/26/13/20 e campos do
/// grupo <c>cat</c> no XSD travado do S-1.3.
/// </summary>
public sealed class ComunicacaoAcidente : AggregateRoot<ComunicacaoAcidenteId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo da CID (codigo CID-10).</summary>
    public const int ComprimentoMaximoCid = 4;

    /// <summary>Comprimento maximo da descricao da situacao geradora.</summary>
    public const int ComprimentoMaximoDescricao = 999;

    private ComunicacaoAcidente()
    {
    }

    private ComunicacaoAcidente(
        ComunicacaoAcidenteId id,
        Guid tenantId,
        ServidorId servidorId,
        TipoCat tipoCat,
        TipoAcidente tipoAcidente,
        DateTimeOffset dataHoraAcidente,
        bool houveObito,
        DateOnly? dataObito,
        string descricaoSituacao,
        string? cid,
        string? parteCorpoAtingida,
        string? agenteCausador,
        ComunicacaoAcidenteId? catOrigem)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        TipoCat = tipoCat;
        TipoAcidente = tipoAcidente;
        DataHoraAcidente = dataHoraAcidente;
        HouveObito = houveObito;
        DataObito = dataObito;
        DescricaoSituacao = descricaoSituacao;
        Cid = cid;
        ParteCorpoAtingida = parteCorpoAtingida;
        AgenteCausador = agenteCausador;
        CatOrigem = catOrigem;
        Situacao = SituacaoRegistroSst.Registrado;
        RaiseDomainEvent(new ComunicacaoAcidenteRegistrada(id, servidorId, tipoCat, dataHoraAcidente, houveObito));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor acidentado.</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Tipo da CAT (inicial/reabertura/obito).</summary>
    public TipoCat TipoCat { get; private set; }

    /// <summary>Tipo do acidente (tipico/doenca/trajeto).</summary>
    public TipoAcidente TipoAcidente { get; private set; }

    /// <summary>Data/hora do acidente (<c>dtAcid</c>/<c>hrAcid</c>).</summary>
    public DateTimeOffset DataHoraAcidente { get; private set; }

    /// <summary>Indica se houve obito decorrente do acidente (<c>indCatObito = S</c>).</summary>
    public bool HouveObito { get; private set; }

    /// <summary>Data do obito quando houve (<c>dtObito</c>); nula caso contrario.</summary>
    public DateOnly? DataObito { get; private set; }

    /// <summary>Descricao da situacao geradora do acidente/doenca.</summary>
    public string DescricaoSituacao { get; private set; } = default!;

    /// <summary>CID-10 do diagnostico (Tabela 13); nula quando ausente.</summary>
    public string? Cid { get; private set; }

    /// <summary>Parte do corpo atingida (Tabela 13); nula quando ausente.</summary>
    public string? ParteCorpoAtingida { get; private set; }

    /// <summary>Agente causador (Tabela 13); nulo quando ausente.</summary>
    public string? AgenteCausador { get; private set; }

    /// <summary>
    /// CAT de origem referenciada (<c>nrRecCatOrig</c> em negocio): reabertura e obito apontam a CAT
    /// inicial. Nula para CAT inicial.
    /// </summary>
    public ComunicacaoAcidenteId? CatOrigem { get; private set; }

    /// <summary>Situacao do registro (vigente/cancelado).</summary>
    public SituacaoRegistroSst Situacao { get; private set; }

    /// <summary>Motivo do cancelamento, quando cancelado; nulo enquanto vigente.</summary>
    public string? MotivoCancelamento { get; private set; }

    /// <summary>
    /// Comunica um acidente de trabalho (CAT). Invariantes: reabertura/obito exigem a CAT de origem;
    /// obito exige a data do obito; CAT inicial nao referencia origem. Emite
    /// <see cref="ComunicacaoAcidenteRegistrada"/> (gancho p/ geracao do S-2210).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor acidentado.</param>
    /// <param name="tipoCat">Tipo da CAT (inicial/reabertura/obito).</param>
    /// <param name="tipoAcidente">Tipo do acidente.</param>
    /// <param name="dataHoraAcidente">Data/hora do acidente.</param>
    /// <param name="descricaoSituacao">Descricao da situacao geradora (nao vazia).</param>
    /// <param name="houveObito">Indica obito.</param>
    /// <param name="dataObito">Data do obito (obrigatoria quando houve obito).</param>
    /// <param name="cid">CID-10 (opcional).</param>
    /// <param name="parteCorpoAtingida">Parte do corpo (opcional).</param>
    /// <param name="agenteCausador">Agente causador (opcional).</param>
    /// <param name="catOrigem">CAT de origem (obrigatoria em reabertura/obito; nula em inicial).</param>
    /// <returns>Nova <see cref="ComunicacaoAcidente"/> em situacao <see cref="SituacaoRegistroSst.Registrado"/>.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia ou a CID exceder o limite.</exception>
    /// <exception cref="InvalidOperationException">Se as invariantes de obito/origem forem violadas.</exception>
    public static ComunicacaoAcidente Comunicar(
        Guid tenantId,
        ServidorId servidorId,
        TipoCat tipoCat,
        TipoAcidente tipoAcidente,
        DateTimeOffset dataHoraAcidente,
        string descricaoSituacao,
        bool houveObito = false,
        DateOnly? dataObito = null,
        string? cid = null,
        string? parteCorpoAtingida = null,
        string? agenteCausador = null,
        ComunicacaoAcidenteId? catOrigem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricaoSituacao);

        var descricaoNormalizada = descricaoSituacao.Trim();
        if (descricaoNormalizada.Length > ComprimentoMaximoDescricao)
        {
            throw new ArgumentException($"Descricao da situacao excede {ComprimentoMaximoDescricao} caracteres.", nameof(descricaoSituacao));
        }

        var cidNormalizada = NormalizarCodigo(cid, nameof(cid));

        // Invariante: reabertura e comunicacao de obito sempre referenciam a CAT inicial; a inicial nao.
        if (tipoCat is TipoCat.Reabertura or TipoCat.ComunicacaoObito && catOrigem is null)
        {
            throw new InvalidOperationException("CAT de reabertura/obito exige a referencia a CAT de origem (inicial).");
        }

        if (tipoCat == TipoCat.Inicial && catOrigem is not null)
        {
            throw new InvalidOperationException("CAT inicial nao referencia CAT de origem.");
        }

        // Invariante: obito exige a data; ausencia de obito nao admite data de obito.
        if (houveObito && dataObito is null)
        {
            throw new InvalidOperationException("Comunicacao de obito exige a data do obito.");
        }

        if (!houveObito && dataObito is not null)
        {
            throw new InvalidOperationException("Data de obito so e admitida quando houve obito.");
        }

        return new ComunicacaoAcidente(
            ComunicacaoAcidenteId.New(),
            tenantId,
            servidorId,
            tipoCat,
            tipoAcidente,
            dataHoraAcidente,
            houveObito,
            dataObito,
            descricaoNormalizada,
            cidNormalizada,
            NormalizarTexto(parteCorpoAtingida),
            NormalizarTexto(agenteCausador),
            catOrigem);
    }

    /// <summary>Cancela a CAT (tornada sem efeito; estado terminal). Emite <see cref="RegistroSstCancelado"/>.</summary>
    /// <param name="motivo">Motivo do cancelamento (nao vazio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se ja cancelada.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoRegistroSst.Cancelado)
        {
            throw new InvalidOperationException("CAT cancelada e estado terminal; nao admite alteracao.");
        }

        Situacao = SituacaoRegistroSst.Cancelado;
        MotivoCancelamento = motivo.Trim();
        RaiseDomainEvent(new RegistroSstCancelado(Id.Value, nameof(ComunicacaoAcidente), MotivoCancelamento));
    }

    private static string? NormalizarCodigo(string? valor, string nomeParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim().ToUpperInvariant();
        if (normalizado.Length > ComprimentoMaximoCid)
        {
            throw new ArgumentException($"Codigo CID excede {ComprimentoMaximoCid} caracteres.", nomeParametro);
        }

        return normalizado;
    }

    private static string? NormalizarTexto(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
