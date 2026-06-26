using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Domicilio;

/// <summary>Identificador forte da entidade <see cref="MensagemFiscal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MensagemFiscalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MensagemFiscalId"/>.</returns>
    public static MensagemFiscalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Tipo (natureza) da comunicação fiscal eletrônica.</summary>
public enum TipoMensagemFiscal
{
    /// <summary>Intimação (efeito de intimação pessoal — abre prazo processual).</summary>
    Intimacao = 1,

    /// <summary>Notificação de lançamento/cobrança.</summary>
    Notificacao = 2,

    /// <summary>Aviso/comunicado geral (sem prazo processual).</summary>
    Aviso = 3,
}

/// <summary>Forma como se deu a ciência da mensagem.</summary>
public enum FormaCiencia
{
    /// <summary>Ainda sem ciência.</summary>
    Pendente = 0,

    /// <summary>Ciência EXPRESSA pela consulta do contribuinte.</summary>
    Expressa = 1,

    /// <summary>Ciência TÁCITA pelo decurso do prazo de disponibilização sem consulta (presunção legal).</summary>
    Tacita = 2,
}

/// <summary>
/// Mensagem fiscal disponibilizada no Domicílio Eletrônico do Contribuinte: tipo, assunto, corpo, datas
/// de disponibilização, de ciência tácita (limite) e de ciência efetiva, a forma de ciência e o prazo de
/// manifestação contado A PARTIR DA CIÊNCIA. Entidade-filha de
/// <see cref="DomicilioEletronicoContribuinte"/>. As datas são do fato (informadas), não do relógio
/// (CLAUDE.md §16). A ciência é imutável uma vez registrada (auditoria).
/// </summary>
public sealed class MensagemFiscal : Entity<MensagemFiscalId>
{
    private MensagemFiscal()
    {
    }

    private MensagemFiscal(
        MensagemFiscalId id,
        DomicilioEletronicoContribuinteId domicilioId,
        TipoMensagemFiscal tipo,
        string assunto,
        string corpo,
        DateOnly dataDisponibilizacao,
        DateOnly dataLimiteCienciaTacita,
        int diasPrazoManifestacao,
        string? referenciaExterna)
        : base(id)
    {
        DomicilioEletronicoContribuinteId = domicilioId;
        Tipo = tipo;
        Assunto = assunto;
        Corpo = corpo;
        DataDisponibilizacao = dataDisponibilizacao;
        DataLimiteCienciaTacita = dataLimiteCienciaTacita;
        DiasPrazoManifestacao = diasPrazoManifestacao;
        ReferenciaExterna = referenciaExterna;
        Forma = FormaCiencia.Pendente;
    }

    /// <summary>Domicílio ao qual a mensagem pertence.</summary>
    public DomicilioEletronicoContribuinteId DomicilioEletronicoContribuinteId { get; private set; }

    /// <summary>Tipo da comunicação.</summary>
    public TipoMensagemFiscal Tipo { get; private set; }

    /// <summary>Assunto da mensagem.</summary>
    public string Assunto { get; private set; } = default!;

    /// <summary>Corpo da comunicação.</summary>
    public string Corpo { get; private set; } = default!;

    /// <summary>Referência ao ato de origem (ex.: nº do lançamento/CDA), quando houver.</summary>
    public string? ReferenciaExterna { get; private set; }

    /// <summary>Data de disponibilização (início da contagem para a ciência tácita).</summary>
    public DateOnly DataDisponibilizacao { get; private set; }

    /// <summary>Data-limite da ciência tácita = disponibilização + prazo de disponibilização (dias).</summary>
    public DateOnly DataLimiteCienciaTacita { get; private set; }

    /// <summary>Prazo (dias) de manifestação/pagamento contado da CIÊNCIA; 0 se sem prazo.</summary>
    public int DiasPrazoManifestacao { get; private set; }

    /// <summary>Forma como se deu a ciência (pendente/expressa/tácita).</summary>
    public FormaCiencia Forma { get; private set; }

    /// <summary>Data efetiva da ciência (consulta ou decurso do prazo); nula enquanto pendente.</summary>
    public DateOnly? DataCiencia { get; private set; }

    /// <summary>Data-limite de manifestação = ciência + prazo de manifestação; nula enquanto sem ciência ou sem prazo.</summary>
    public DateOnly? DataLimiteManifestacao { get; private set; }

    /// <summary>Indica se a mensagem já teve ciência (expressa ou tácita).</summary>
    public bool TeveCiencia => Forma != FormaCiencia.Pendente;

    /// <summary>Disponibiliza (cria) uma mensagem fiscal não lida.</summary>
    /// <param name="domicilioId">Domicílio proprietário.</param>
    /// <param name="tipo">Tipo da comunicação.</param>
    /// <param name="assunto">Assunto.</param>
    /// <param name="corpo">Corpo.</param>
    /// <param name="dataDisponibilizacao">Data de disponibilização.</param>
    /// <param name="diasCienciaTacita">Prazo (dias) de disponibilização para a ciência tácita.</param>
    /// <param name="diasPrazoManifestacao">Prazo (dias) de manifestação contado da ciência; 0 se sem prazo.</param>
    /// <param name="referenciaExterna">Referência ao ato de origem (opcional).</param>
    /// <returns>Nova <see cref="MensagemFiscal"/>.</returns>
    public static MensagemFiscal Disponibilizar(
        DomicilioEletronicoContribuinteId domicilioId,
        TipoMensagemFiscal tipo,
        string assunto,
        string corpo,
        DateOnly dataDisponibilizacao,
        int diasCienciaTacita,
        int diasPrazoManifestacao,
        string? referenciaExterna)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assunto);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpo);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de mensagem fiscal inválido.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(diasCienciaTacita, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(diasPrazoManifestacao);

        var dataLimiteCienciaTacita = dataDisponibilizacao.AddDays(diasCienciaTacita);
        return new MensagemFiscal(
            MensagemFiscalId.New(),
            domicilioId,
            tipo,
            assunto.Trim(),
            corpo.Trim(),
            dataDisponibilizacao,
            dataLimiteCienciaTacita,
            diasPrazoManifestacao,
            string.IsNullOrWhiteSpace(referenciaExterna) ? null : referenciaExterna.Trim());
    }

    /// <summary>
    /// Registra a ciência EXPRESSA pela consulta na data informada, se ainda pendente. Define o prazo de
    /// manifestação a partir da ciência. Retorna <c>true</c> se a ciência foi registrada agora.
    /// </summary>
    /// <param name="dataConsulta">Data da consulta (data do fato).</param>
    /// <returns><c>true</c> se a ciência foi registrada nesta chamada; <c>false</c> se já havia ciência.</returns>
    public bool RegistrarCienciaExpressa(DateOnly dataConsulta)
    {
        if (TeveCiencia)
        {
            return false;
        }

        if (dataConsulta < DataDisponibilizacao)
        {
            throw new ArgumentOutOfRangeException(nameof(dataConsulta), "A consulta não pode anteceder a disponibilização.");
        }

        RegistrarCiencia(FormaCiencia.Expressa, dataConsulta);
        return true;
    }

    /// <summary>
    /// Registra a ciência TÁCITA se a data-limite de ciência tácita já se completou na referência e ainda
    /// não houve ciência. A ciência tácita ocorre NA data-limite (decurso do prazo). Retorna <c>true</c>
    /// se foi registrada agora.
    /// </summary>
    /// <param name="referencia">Data de referência (data do fato).</param>
    /// <returns><c>true</c> se a ciência tácita foi registrada nesta chamada.</returns>
    public bool RegistrarCienciaTacitaSeVencida(DateOnly referencia)
    {
        if (TeveCiencia || referencia < DataLimiteCienciaTacita)
        {
            return false;
        }

        RegistrarCiencia(FormaCiencia.Tacita, DataLimiteCienciaTacita);
        return true;
    }

    private void RegistrarCiencia(FormaCiencia forma, DateOnly dataCiencia)
    {
        Forma = forma;
        DataCiencia = dataCiencia;
        DataLimiteManifestacao = DiasPrazoManifestacao > 0 ? dataCiencia.AddDays(DiasPrazoManifestacao) : null;
    }
}
