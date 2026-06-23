using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

/// <summary>Identificador forte do agregado <see cref="EventoESocial"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct EventoESocialId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="EventoESocialId"/>.</returns>
    public static EventoESocialId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Evento eSocial gerado a partir do nosso dominio (folha/servidor/rubricas) e levado pela maquina de
/// estados <c>Gerado -> Assinado -> Transmitido -> Processado/Rejeitado</c> (ESOCIAL-SPEC §4.4). Raiz
/// de agregado <see cref="IMustHaveTenant"/>. Carrega o XML do evento (gerado fiel ao leiaute S-1.3 —
/// // TODO(validar-oficial: XSD)), depois o XML assinado (XML-DSig A1 via Cofre), o protocolo do lote
/// e, ao final, o recibo (<c>nrRecibo</c>) por evento — prova de entrega exigida para retificacao/
/// exclusao. A idempotencia de GERACAO e garantida pela <see cref="ChaveIdempotencia"/>
/// <c>(tipo, idNegocio, competencia)</c>: um unico evento por chave de negocio.
/// </summary>
public sealed class EventoESocial : AggregateRoot<EventoESocialId>, IMustHaveTenant
{
    private EventoESocial()
    {
    }

    private EventoESocial(
        EventoESocialId id,
        Guid tenantId,
        TipoEventoESocial tipo,
        ChaveIdempotenciaEvento chaveIdempotencia,
        string idEvento,
        AmbienteESocial ambiente,
        byte[] xml,
        DateTimeOffset geradoEm)
        : base(id)
    {
        TenantId = tenantId;
        Tipo = tipo;
        ChaveIdempotencia = chaveIdempotencia;
        IdEvento = idEvento;
        Ambiente = ambiente;
        Xml = xml;
        HashXmlGerado = CalcularHash(xml);
        Estado = EstadoEventoESocial.Gerado;
        GeradoEm = geradoEm;
        RaiseDomainEvent(new EventoESocialGerado(id, tipo, idEvento));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Tipo do evento (S-1000, S-1200, ...).</summary>
    public TipoEventoESocial Tipo { get; private set; }

    /// <summary>Chave de idempotencia de geracao (unica por tenant).</summary>
    public ChaveIdempotenciaEvento ChaveIdempotencia { get; private set; } = default!;

    /// <summary>
    /// Atributo <c>Id</c> do evento (identificador de negocio no XML; consulta/download BX). NAO e o
    /// alvo da assinatura (a assinatura usa Reference URI="" — documento inteiro). // TODO(validar-oficial:
    /// formato exato do Id no MOS: prefixo "ID" + tpInsc + nrInsc + AAAAMMDDHHMMSS + sequencial).
    /// </summary>
    public string IdEvento { get; private set; } = default!;

    /// <summary>Ambiente de transmissao (Producao/Producao Restrita).</summary>
    public AmbienteESocial Ambiente { get; private set; }

    /// <summary>Estado atual na maquina de estados.</summary>
    public EstadoEventoESocial Estado { get; private set; }

    /// <summary>XML do evento gerado (UTF-8), fiel ao leiaute. // TODO(validar-oficial: XSD).</summary>
    public byte[] Xml { get; private set; } = [];

    /// <summary>Hash SHA-256 (hex) do XML gerado — trilha do "o que" foi assinado/transmitido.</summary>
    public string HashXmlGerado { get; private set; } = default!;

    /// <summary>XML assinado (XML-DSig A1); nulo antes de assinar.</summary>
    public byte[]? XmlAssinado { get; private set; }

    /// <summary>Thumbprint do certificado A1 usado na assinatura; nulo antes de assinar.</summary>
    public string? ThumbprintCertificado { get; private set; }

    /// <summary>Protocolo do lote retornado no envio (nivel 1 sincrono); nulo antes de transmitir.</summary>
    public string? ProtocoloLote { get; private set; }

    /// <summary>Recibo (<c>nrRecibo</c>) por evento, retornado no processamento; nulo antes de aceitar.</summary>
    public string? NumeroRecibo { get; private set; }

    /// <summary>Codigo do erro/ocorrencia quando rejeitado (local ou pelo eSocial); nulo caso contrario.</summary>
    public string? CodigoErro { get; private set; }

    /// <summary>Descricao do erro/ocorrencia quando rejeitado; nula caso contrario.</summary>
    public string? DescricaoErro { get; private set; }

    /// <summary>Instante de geracao.</summary>
    public DateTimeOffset GeradoEm { get; private set; }

    /// <summary>Instante da assinatura; nulo antes de assinar.</summary>
    public DateTimeOffset? AssinadoEm { get; private set; }

    /// <summary>Instante da transmissao; nulo antes de transmitir.</summary>
    public DateTimeOffset? TransmitidoEm { get; private set; }

    /// <summary>Instante do retorno (aceite/rejeicao); nulo antes do retorno.</summary>
    public DateTimeOffset? RetornoEm { get; private set; }

    /// <summary>Indica se o evento esta em estado terminal de sucesso (nao reprocessar).</summary>
    public bool EhTerminalSucesso => Estado == EstadoEventoESocial.Processado;

    /// <summary>
    /// Cria um evento eSocial recem-gerado (estado <see cref="EstadoEventoESocial.Gerado"/>), a partir
    /// do XML montado fiel ao leiaute pelo ACL de mapeamento dominio -> leiaute.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="tipo">Tipo do evento.</param>
    /// <param name="chaveIdempotencia">Chave de negocio (idempotencia de geracao).</param>
    /// <param name="idEvento">Atributo Id do evento (identificador de negocio no XML).</param>
    /// <param name="ambiente">Ambiente de transmissao.</param>
    /// <param name="xml">XML do evento (UTF-8), nao vazio.</param>
    /// <param name="geradoEm">Instante de geracao.</param>
    /// <param name="id">Identificador do agregado; quando nulo, e gerado um novo (<see cref="EventoESocialId.New"/>).</param>
    /// <returns>Novo <see cref="EventoESocial"/> em <see cref="EstadoEventoESocial.Gerado"/>.</returns>
    /// <exception cref="ArgumentNullException">Se a chave de idempotencia for nula.</exception>
    /// <exception cref="ArgumentException">Se o id do evento for vazio ou o XML estiver vazio.</exception>
    public static EventoESocial Gerar(
        Guid tenantId,
        TipoEventoESocial tipo,
        ChaveIdempotenciaEvento chaveIdempotencia,
        string idEvento,
        AmbienteESocial ambiente,
        byte[] xml,
        DateTimeOffset geradoEm,
        EventoESocialId? id = null)
    {
        ArgumentNullException.ThrowIfNull(chaveIdempotencia);
        ArgumentException.ThrowIfNullOrWhiteSpace(idEvento);
        ArgumentNullException.ThrowIfNull(xml);
        if (xml.Length == 0)
        {
            throw new ArgumentException("XML do evento nao pode ser vazio.", nameof(xml));
        }

        return new EventoESocial(
            id ?? EventoESocialId.New(),
            tenantId,
            tipo,
            chaveIdempotencia,
            idEvento.Trim(),
            ambiente,
            xml,
            geradoEm);
    }

    /// <summary>
    /// Transita <c>Gerado -> Assinado</c> apos a assinatura XML-DSig A1 (via Cofre). Idempotente: re-
    /// assinar um evento ja Assinado e no-op (mantem a assinatura existente).
    /// </summary>
    /// <param name="xmlAssinado">XML assinado (UTF-8).</param>
    /// <param name="thumbprintCertificado">Thumbprint do certificado usado.</param>
    /// <param name="assinadoEm">Instante da assinatura.</param>
    /// <exception cref="ArgumentException">Se o XML assinado estiver vazio ou o thumbprint for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o estado nao for <c>Gerado</c> nem <c>Assinado</c>.</exception>
    public void RegistrarAssinatura(byte[] xmlAssinado, string thumbprintCertificado, DateTimeOffset assinadoEm)
    {
        ArgumentNullException.ThrowIfNull(xmlAssinado);
        if (xmlAssinado.Length == 0)
        {
            throw new ArgumentException("XML assinado nao pode ser vazio.", nameof(xmlAssinado));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprintCertificado);

        if (Estado == EstadoEventoESocial.Assinado)
        {
            return; // Idempotente: ja assinado.
        }

        if (Estado != EstadoEventoESocial.Gerado)
        {
            throw new InvalidOperationException($"A assinatura so ocorre a partir de Gerado. Estado atual: {Estado}.");
        }

        XmlAssinado = xmlAssinado;
        ThumbprintCertificado = thumbprintCertificado;
        AssinadoEm = assinadoEm;
        Estado = EstadoEventoESocial.Assinado;
        RaiseDomainEvent(new EventoESocialAssinado(Id, Tipo));
    }

    /// <summary>
    /// Marca falha de validacao LOCAL (XSD/estrutura) antes da transmissao: transita para
    /// <see cref="EstadoEventoESocial.RejeitadoLocal"/> sem gastar cota. So a partir de
    /// <c>Gerado</c>/<c>Assinado</c>. O evento e re-gerável apos correcao.
    /// </summary>
    /// <param name="codigo">Codigo da ocorrencia (ex.: "XSD").</param>
    /// <param name="descricao">Descricao da falha de validacao.</param>
    /// <exception cref="ArgumentException">Se descricao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se ja transmitido/terminal.</exception>
    public void RejeitarLocalmente(string codigo, string descricao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        if (Estado is not (EstadoEventoESocial.Gerado or EstadoEventoESocial.Assinado))
        {
            throw new InvalidOperationException($"Rejeicao local so antes da transmissao. Estado atual: {Estado}.");
        }

        Estado = EstadoEventoESocial.RejeitadoLocal;
        CodigoErro = string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim();
        DescricaoErro = descricao.Trim();
        RaiseDomainEvent(new EventoESocialRejeitado(Id, Tipo, CodigoErro, DescricaoErro, Localizado: true));
    }

    /// <summary>
    /// Transita <c>Assinado -> Transmitido</c> ao enviar o lote e receber o protocolo (nivel 1 sincrono).
    /// Idempotente: re-registrar o mesmo protocolo num evento ja Transmitido e no-op.
    /// </summary>
    /// <param name="protocoloLote">Protocolo de envio do lote (nao vazio).</param>
    /// <param name="transmitidoEm">Instante da transmissao.</param>
    /// <exception cref="ArgumentException">Se o protocolo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o estado nao for <c>Assinado</c> nem <c>Transmitido</c>.</exception>
    public void RegistrarTransmissao(string protocoloLote, DateTimeOffset transmitidoEm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocoloLote);

        if (Estado == EstadoEventoESocial.Transmitido)
        {
            return; // Idempotente: ja transmitido.
        }

        if (Estado != EstadoEventoESocial.Assinado)
        {
            throw new InvalidOperationException($"A transmissao so ocorre a partir de Assinado. Estado atual: {Estado}.");
        }

        ProtocoloLote = protocoloLote.Trim();
        TransmitidoEm = transmitidoEm;
        Estado = EstadoEventoESocial.Transmitido;
        RaiseDomainEvent(new EventoESocialTransmitido(Id, Tipo, ProtocoloLote));
    }

    /// <summary>
    /// Transita <c>Transmitido -> Processado</c> ao receber o recibo (<c>nrRecibo</c>) por evento.
    /// Idempotente: re-aplicar o mesmo recibo num evento ja Processado e no-op (terminal de sucesso).
    /// </summary>
    /// <param name="numeroRecibo">Numero do recibo retornado pelo eSocial (nao vazio).</param>
    /// <param name="retornoEm">Instante do retorno.</param>
    /// <exception cref="ArgumentException">Se o recibo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o estado nao for <c>Transmitido</c> nem <c>Processado</c>.</exception>
    public void RegistrarRecibo(string numeroRecibo, DateTimeOffset retornoEm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroRecibo);

        if (Estado == EstadoEventoESocial.Processado)
        {
            return; // Idempotente: ja aceito.
        }

        if (Estado != EstadoEventoESocial.Transmitido)
        {
            throw new InvalidOperationException($"O recibo so e aceito a partir de Transmitido. Estado atual: {Estado}.");
        }

        NumeroRecibo = numeroRecibo.Trim();
        RetornoEm = retornoEm;
        Estado = EstadoEventoESocial.Processado;
        RaiseDomainEvent(new EventoESocialProcessado(Id, Tipo, NumeroRecibo));
    }

    /// <summary>
    /// Transita <c>Transmitido -> Rejeitado</c> ao receber erros por evento no processamento do lote.
    /// Re-gerável apos correcao (nao terminal definitivo).
    /// </summary>
    /// <param name="codigo">Codigo do erro retornado pelo eSocial.</param>
    /// <param name="descricao">Descricao do erro.</param>
    /// <param name="retornoEm">Instante do retorno.</param>
    /// <exception cref="ArgumentException">Se descricao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o estado nao for <c>Transmitido</c>.</exception>
    public void RegistrarRejeicao(string codigo, string descricao, DateTimeOffset retornoEm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        if (Estado != EstadoEventoESocial.Transmitido)
        {
            throw new InvalidOperationException($"A rejeicao so e aceita a partir de Transmitido. Estado atual: {Estado}.");
        }

        Estado = EstadoEventoESocial.Rejeitado;
        CodigoErro = string.IsNullOrWhiteSpace(codigo) ? null : codigo.Trim();
        DescricaoErro = descricao.Trim();
        RetornoEm = retornoEm;
        RaiseDomainEvent(new EventoESocialRejeitado(Id, Tipo, CodigoErro, DescricaoErro, Localizado: false));
    }

    private static string CalcularHash(byte[] conteudo)
    {
        var hash = SHA256.HashData(conteudo);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
