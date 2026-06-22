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
        TipoRep origem)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        Cpf = cpf;
        Nsr = nsr;
        DataHora = dataHora;
        Sentido = sentido;
        Origem = origem;
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
            origem);
    }
}
