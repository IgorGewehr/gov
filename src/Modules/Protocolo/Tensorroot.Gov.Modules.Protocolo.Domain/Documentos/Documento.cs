using Tensorroot.Gov.Modules.Protocolo.Domain.Events;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;

/// <summary>Identificador forte do agregado <see cref="Documento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DocumentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DocumentoId"/>.</returns>
    public static DocumentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Peca processual (nato-digital ou digitalizada) em PDF/A, com hash SHA-256 e carimbo de tempo,
/// assinada por nivel de criticidade conforme a Lei 14.063/2020 e o Decreto 10.543/2020
/// (SIMPLES / AVANCADA / QUALIFICADA — ICP-Brasil). Imutavel apos a juntada: nunca excluida,
/// apenas tornada sem efeito, preservando a trilha documental (Lei 11.419/2006; e-ARQ Brasil).
/// </summary>
public sealed class Documento : AggregateRoot<DocumentoId>, IMustHaveTenant
{
    /// <summary>
    /// Mapa de dominio do nivel minimo de assinatura exigido por criticidade do ato
    /// (Decreto 10.543/2020): Alta → Qualificada, Media → Avancada, Baixa → Simples.
    /// </summary>
    private static readonly Dictionary<CriticidadeAto, Documentos.TipoAssinatura> NivelMinimoPorCriticidade =
        new()
        {
            [CriticidadeAto.Baixa] = Documentos.TipoAssinatura.AssinaturaSimples,
            [CriticidadeAto.Media] = Documentos.TipoAssinatura.AssinaturaAvancada,
            [CriticidadeAto.Alta] = Documentos.TipoAssinatura.AssinaturaQualificada,
        };

    private Documento()
    {
    }

    private Documento(
        DocumentoId id,
        Guid tenantId,
        Hash hash,
        CriticidadeAto criticidade,
        NivelDeAcesso nivelAcesso,
        bool formatoPdfA)
        : base(id)
    {
        TenantId = tenantId;
        Hash = hash;
        Criticidade = criticidade;
        NivelAcesso = nivelAcesso;
        FormatoPdfA = formatoPdfA;
        Situacao = SituacaoDocumento.Rascunho;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Processo ao qual esta juntado (nulo antes da juntada).</summary>
    public Guid? ProcessoId { get; private set; }

    /// <summary>Resumo SHA-256 do conteudo (integridade); imutavel apos a juntada.</summary>
    public Hash Hash { get; private set; } = default!;

    /// <summary>Carimbo de tempo da assinatura (nulo antes de assinar).</summary>
    public CarimboDeTempo? CarimboTempo { get; private set; }

    /// <summary>Criticidade do ato — determina o nivel minimo de assinatura.</summary>
    public CriticidadeAto Criticidade { get; private set; }

    /// <summary>Visibilidade do documento.</summary>
    public NivelDeAcesso NivelAcesso { get; private set; }

    /// <summary>Indica conformidade com PDF/A (e-ARQ Brasil).</summary>
    public bool FormatoPdfA { get; private set; }

    /// <summary>Signatario (nulo antes da assinatura).</summary>
    public Guid? SignatarioId { get; private set; }

    /// <summary>Tipo da assinatura aplicada (nulo antes de assinar).</summary>
    public TipoAssinatura? TipoAssinatura { get; private set; }

    /// <summary>Data da juntada ao processo (nula antes).</summary>
    public DateOnly? DataJuntada { get; private set; }

    /// <summary>Motivo do "sem efeito" (nulo enquanto valido).</summary>
    public string? MotivoSemEfeito { get; private set; }

    /// <summary>Situacao atual do documento.</summary>
    public SituacaoDocumento Situacao { get; private set; }

    /// <summary>
    /// Cria um documento em situacao <see cref="SituacaoDocumento.Rascunho"/> (I-1: exige hash e PDF/A).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="hash">Resumo SHA-256 do conteudo.</param>
    /// <param name="criticidade">Criticidade do ato.</param>
    /// <param name="nivelAcesso">Visibilidade do documento.</param>
    /// <param name="formatoPdfA">Conformidade com PDF/A (deve ser <c>true</c>).</param>
    /// <returns>Novo <see cref="Documento"/> em rascunho.</returns>
    /// <exception cref="ArgumentNullException">Se o hash nao for informado.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver em PDF/A (I-1).</exception>
    public static Documento Criar(
        Guid tenantId,
        Hash hash,
        CriticidadeAto criticidade,
        NivelDeAcesso nivelAcesso,
        bool formatoPdfA)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (!formatoPdfA)
        {
            throw new InvalidOperationException("Documento deve estar em PDF/A (e-ARQ Brasil).");
        }

        return new Documento(DocumentoId.New(), tenantId, hash, criticidade, nivelAcesso, formatoPdfA);
    }

    /// <summary>
    /// Junta o documento a um processo, tornando-o imutavel e gravando a data da juntada (I-2).
    /// </summary>
    /// <param name="processoId">Processo de destino.</param>
    /// <param name="dataJuntada">Data da juntada.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o processo nao for informado.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoDocumento.Rascunho"/>.</exception>
    public void Juntar(Guid processoId, DateOnly dataJuntada)
    {
        if (processoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(processoId), "Processo de destino e obrigatorio.");
        }

        if (Situacao != SituacaoDocumento.Rascunho)
        {
            throw new InvalidOperationException($"A juntada so ocorre a partir de Rascunho. Situacao atual: {Situacao}.");
        }

        ProcessoId = processoId;
        DataJuntada = dataJuntada;
        Situacao = SituacaoDocumento.Juntado;
        RaiseDomainEvent(new DocumentoJuntado(Id, processoId, Hash));
    }

    /// <summary>
    /// Assina o documento, exigindo nivel de assinatura compativel com a criticidade (I-5/I-6) e
    /// carimbo de tempo confiavel (I-8); so ocorre sobre documento juntado/valido (I-7).
    /// </summary>
    /// <param name="signatarioId">Sujeito que assina.</param>
    /// <param name="tipo">Nivel da assinatura aplicada.</param>
    /// <param name="carimboTempo">Carimbo de tempo confiavel da assinatura.</param>
    /// <exception cref="ArgumentNullException">Se o carimbo de tempo nao for informado.</exception>
    /// <exception cref="InvalidOperationException">
    /// Se a situacao nao for juntada/valida, ou se o nivel de assinatura for inferior ao exigido.
    /// </exception>
    public void Assinar(Guid signatarioId, TipoAssinatura tipo, CarimboDeTempo carimboTempo)
    {
        ArgumentNullException.ThrowIfNull(carimboTempo);
        if (Situacao is not (SituacaoDocumento.Juntado or SituacaoDocumento.Assinado))
        {
            throw new InvalidOperationException($"A assinatura so ocorre sobre documento Juntado/Assinado. Situacao atual: {Situacao}.");
        }

        GarantirNivelAdequado(Criticidade, tipo);
        GarantirCarimboCompativel(Criticidade, carimboTempo);

        // I-CT2 (vinculo hash <-> token): quando o carimbo porta um hash atestado (ACT/RFC 3161), ele
        // DEVE coincidir com o hash deste documento — a prova so e oponivel se atestar este conteudo.
        if (!carimboTempo.AtestaHash(Hash.Valor))
        {
            throw new InvalidOperationException(
                "Carimbo de tempo atesta um hash diferente do documento (vinculo hash<->token violado).");
        }

        var assinatura = Assinatura.De(tipo, signatarioId, carimboTempo);
        SignatarioId = assinatura.SignatarioId;
        TipoAssinatura = assinatura.Tipo;
        CarimboTempo = assinatura.CarimboTempo;
        Situacao = SituacaoDocumento.Assinado;
        RaiseDomainEvent(new DocumentoAssinado(Id, assinatura.SignatarioId, assinatura.Tipo));
    }

    /// <summary>
    /// Torna o documento sem efeito (terminal), preservando o registro na trilha documental (I-9).
    /// Nao remove a linha; nao ha comando de exclusao (I-4).
    /// </summary>
    /// <param name="motivo">Motivo obrigatorio do "sem efeito".</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for Juntado/Assinado (I-9/I-10).</exception>
    public void TornarSemEfeito(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is not (SituacaoDocumento.Juntado or SituacaoDocumento.Assinado))
        {
            throw new InvalidOperationException($"Tornar sem efeito so ocorre sobre documento Juntado/Assinado. Situacao atual: {Situacao}.");
        }

        MotivoSemEfeito = motivo.Trim();
        Situacao = SituacaoDocumento.SemEfeito;
    }

    /// <summary>
    /// Verifica a integridade do documento comparando o hash armazenado com um digest recalculado (I-12).
    /// </summary>
    /// <param name="hashRecalculado">Digest SHA-256 recalculado do conteudo.</param>
    /// <returns><c>true</c> se integro (hashes coincidem).</returns>
    public bool VerificarIntegridade(string hashRecalculado) => Hash.Corresponde(hashRecalculado);

    /// <summary>
    /// Revalida o vinculo entre o carimbo de tempo e este documento (I-CT2): o hash atestado pela ACT
    /// deve coincidir com o hash do documento — base da oponibilidade ao TCE. Documento sem carimbo
    /// (ainda nao assinado) e considerado nao-verificavel.
    /// </summary>
    /// <returns><c>true</c> se ha carimbo e o hash atestado coincide com o do documento.</returns>
    public bool VerificarCarimbo() => CarimboTempo is not null && CarimboTempo.AtestaHash(Hash.Valor);

    /// <summary>
    /// Garante que o nivel de assinatura aplicado e maior ou igual ao minimo exigido pela criticidade
    /// (Decreto 10.543/2020 — I-5/I-6). Rejeita nivel inferior.
    /// </summary>
    /// <param name="criticidade">Criticidade do ato.</param>
    /// <param name="tipo">Nivel da assinatura tentada.</param>
    /// <exception cref="InvalidOperationException">Se o nivel for inferior ao minimo exigido.</exception>
    private static void GarantirNivelAdequado(CriticidadeAto criticidade, Documentos.TipoAssinatura tipo)
    {
        var minimo = NivelMinimoPorCriticidade[criticidade];
        if (tipo < minimo)
        {
            throw new InvalidOperationException(
                $"Criticidade {criticidade} exige assinatura minima {minimo}; nivel {tipo} e insuficiente.");
        }
    }

    /// <summary>
    /// Garante que o carimbo de tempo e compativel com a criticidade do ato (I-CT3 / W9.4): ato de
    /// criticidade <see cref="CriticidadeAto.Alta"/> (assinatura qualificada ICP-Brasil) exige carimbo
    /// de ACT credenciada (<see cref="OrigemCarimbo.Act"/>); o carimbo do relogio LOCAL (fallback/dev)
    /// NAO satisfaz a oponibilidade exigida do ato qualificado.
    /// </summary>
    /// <param name="criticidade">Criticidade do ato.</param>
    /// <param name="carimboTempo">Carimbo de tempo aplicado.</param>
    /// <exception cref="InvalidOperationException">Se a criticidade for Alta e o carimbo for Local.</exception>
    private static void GarantirCarimboCompativel(CriticidadeAto criticidade, CarimboDeTempo carimboTempo)
    {
        if (criticidade == CriticidadeAto.Alta && carimboTempo.Origem != OrigemCarimbo.Act)
        {
            throw new InvalidOperationException(
                "Criticidade Alta (assinatura qualificada ICP-Brasil) exige carimbo de tempo de ACT credenciada; "
                + "carimbo local nao satisfaz o ato qualificado.");
        }
    }
}
