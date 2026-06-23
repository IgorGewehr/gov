using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Identificador forte do agregado <see cref="MarcacaoPonto"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MarcacaoPontoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MarcacaoPontoId"/>.</returns>
    public static MarcacaoPontoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Marcacao (batida) de ponto de um servidor — registro BRUTO, CRONOLOGICO e IMUTAVEL do AFD
/// (Portaria MTP 671/2021, art. 80). Modelada como LOG APPEND-ONLY: nasce por <see cref="Registrar"/>
/// e NUNCA e alterada nem removida (alinhado a auditoria imutavel — CLAUDE.md §4); correcoes ocorrem
/// no tratamento (PTRP/AEJ), jamais no AFD. Cada marcacao carrega um <see cref="Nsr"/> sequencial e
/// sem lacunas por tenant (REP), o CPF do trabalhador (leiaute vigente desde 10/02/2022) e a origem
/// (REP-P/REP-C/REP-A). Raiz de agregado.
/// </summary>
public sealed class MarcacaoPonto : AggregateRoot<MarcacaoPontoId>, IMustHaveTenant
{
    private MarcacaoPonto()
    {
    }

    private MarcacaoPonto(
        MarcacaoPontoId id,
        Guid tenantId,
        Guid servidorId,
        Cpf cpf,
        Nsr nsr,
        DateTimeOffset dataHora,
        SentidoMarcacao sentido,
        TipoRep origem,
        Guid? repId,
        long? nsrEquipamento)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        Cpf = cpf;
        Nsr = nsr;
        DataHora = dataHora;
        Sentido = sentido;
        Origem = origem;
        RepId = repId;
        NsrEquipamento = nsrEquipamento;
        RaiseDomainEvent(new MarcacaoPontoRegistrada(id, servidorId, nsr.Valor, dataHora));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor a que a marcacao pertence.</summary>
    public Guid ServidorId { get; private set; }

    /// <summary>CPF do trabalhador (leiaute AFD com CPF, vigente desde 10/02/2022).</summary>
    public Cpf Cpf { get; private set; } = default!;

    /// <summary>Numero Sequencial de Registro (sequencial e sem lacunas por tenant/REP).</summary>
    public Nsr Nsr { get; private set; } = default!;

    /// <summary>Data e hora EXATAS da marcacao (com offset; precisao ao segundo). Imutavel.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Sentido da marcacao (entrada/saida).</summary>
    public SentidoMarcacao Sentido { get; private set; }

    /// <summary>Origem do registro (REP-P/REP-C/REP-A).</summary>
    public TipoRep Origem { get; private set; }

    /// <summary>
    /// Equipamento (REP) de onde a marcacao foi COLETADA, quando importada de hardware fisico; <c>null</c>
    /// para marcacoes nascidas no nosso REP-P (registro direto). Compoe a chave natural de idempotencia
    /// da ingestao de AFD: <c>(TenantId, RepId, NsrEquipamento)</c>.
    /// </summary>
    public Guid? RepId { get; private set; }

    /// <summary>
    /// NSR do EQUIPAMENTO de origem (espaco de numeracao do REP fisico — distinto do nosso <see cref="Nsr"/>
    /// interno do REP-P). <c>null</c> para marcacoes proprias. Junto de <see cref="RepId"/> deduplica a
    /// reimportacao do mesmo AFD (re-upload/re-coleta) sem duplicar marcacoes ja ingeridas.
    /// </summary>
    public long? NsrEquipamento { get; private set; }

    /// <summary>
    /// Registra uma nova marcacao imutavel, recebendo o proximo NSR do REP (calculado pela aplicacao
    /// a partir do ultimo NSR persistido — sequencia sem lacunas por tenant). Vedada a marcacao
    /// automatica/pre-preenchida (Portaria 671 art. 80, §) — a hora deve refletir a batida real.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor da marcacao.</param>
    /// <param name="cpf">CPF do trabalhador.</param>
    /// <param name="nsr">NSR atribuido (proximo da sequencia do tenant).</param>
    /// <param name="dataHora">Data/hora exata da batida.</param>
    /// <param name="sentido">Entrada ou saida.</param>
    /// <param name="origem">Origem (REP-P/REP-C/REP-A).</param>
    /// <returns>Nova <see cref="MarcacaoPonto"/> imutavel.</returns>
    /// <exception cref="ArgumentNullException">Se CPF ou NSR forem nulos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se sentido/origem forem invalidos.</exception>
    public static MarcacaoPonto Registrar(
        Guid tenantId,
        Guid servidorId,
        Cpf cpf,
        Nsr nsr,
        DateTimeOffset dataHora,
        SentidoMarcacao sentido,
        TipoRep origem)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        ArgumentNullException.ThrowIfNull(nsr);
        if (!Enum.IsDefined(sentido))
        {
            throw new ArgumentOutOfRangeException(nameof(sentido), "Sentido de marcacao invalido.");
        }

        if (!Enum.IsDefined(origem))
        {
            throw new ArgumentOutOfRangeException(nameof(origem), "Origem (tipo de REP) invalida.");
        }

        return new MarcacaoPonto(
            MarcacaoPontoId.New(),
            tenantId,
            servidorId,
            cpf,
            nsr,
            dataHora,
            sentido,
            origem,
            repId: null,
            nsrEquipamento: null);
    }

    /// <summary>
    /// Registra uma marcacao IMPORTADA de um equipamento REP fisico (ingestao de AFD), preservando a
    /// chave natural <c>(TenantId, RepId, NsrEquipamento)</c> para idempotencia: reimportar o mesmo AFD
    /// NAO duplica a marcacao. O <paramref name="nsr"/> e o NSR do NOSSO espaco (proximo da sequencia do
    /// tenant), enquanto <paramref name="nsrEquipamento"/> e o NSR original gravado no equipamento.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor resolvido por CPF no tenant.</param>
    /// <param name="cpf">CPF do trabalhador (lido do AFD).</param>
    /// <param name="nsr">NSR do nosso REP-P (proximo da sequencia sem lacunas do tenant).</param>
    /// <param name="dataHora">Data/hora exata da batida (lida do AFD).</param>
    /// <param name="sentido">Entrada ou saida (o AFD nao carrega sentido — definido pelo pareamento).</param>
    /// <param name="origem">Origem (REP-C/REP-A para hardware de terceiros).</param>
    /// <param name="repId">Equipamento REP de origem (idempotency key).</param>
    /// <param name="nsrEquipamento">NSR original do equipamento (idempotency key, &gt;= 1).</param>
    /// <returns>Nova <see cref="MarcacaoPonto"/> imutavel de equipamento.</returns>
    /// <exception cref="ArgumentNullException">Se CPF ou NSR forem nulos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se sentido/origem/NSR de equipamento forem invalidos.</exception>
    public static MarcacaoPonto RegistrarDeEquipamento(
        Guid tenantId,
        Guid servidorId,
        Cpf cpf,
        Nsr nsr,
        DateTimeOffset dataHora,
        SentidoMarcacao sentido,
        TipoRep origem,
        Guid repId,
        long nsrEquipamento)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        ArgumentNullException.ThrowIfNull(nsr);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(nsrEquipamento);
        if (!Enum.IsDefined(sentido))
        {
            throw new ArgumentOutOfRangeException(nameof(sentido), "Sentido de marcacao invalido.");
        }

        if (!Enum.IsDefined(origem))
        {
            throw new ArgumentOutOfRangeException(nameof(origem), "Origem (tipo de REP) invalida.");
        }

        return new MarcacaoPonto(
            MarcacaoPontoId.New(),
            tenantId,
            servidorId,
            cpf,
            nsr,
            dataHora,
            sentido,
            origem,
            repId,
            nsrEquipamento);
    }
}
