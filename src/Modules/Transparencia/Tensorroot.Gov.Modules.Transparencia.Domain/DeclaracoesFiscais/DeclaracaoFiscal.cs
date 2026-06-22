using Tensorroot.Gov.Modules.Transparencia.Domain.Events;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

/// <summary>Identificador forte do agregado <see cref="DeclaracaoFiscal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DeclaracaoFiscalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DeclaracaoFiscalId"/>.</returns>
    public static DeclaracaoFiscalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Declaracao fiscal periodica (MSC/RREO/RGF/DCA) transmitida ao SICONFI (STN). Consolida a Matriz de
/// Saldos Contabeis a partir de saldos contabeis consumidos do modulo Financas e percorre o ciclo
/// Consolidada -&gt; Transmitida -&gt; Homologada/Rejeitada (LRF arts. 48/48-A; Lei 4.320/64; Portarias STN).
/// Modulo CONSUMIDOR: nao calcula contabilidade primaria.
/// </summary>
public sealed class DeclaracaoFiscal : AggregateRoot<DeclaracaoFiscalId>, IMustHaveTenant
{
    private DeclaracaoFiscal()
    {
    }

    private DeclaracaoFiscal(
        DeclaracaoFiscalId id,
        Guid tenantId,
        TipoDeclaracaoFiscal tipoDeclaracao,
        int exercicio,
        Competencia? competencia,
        Bimestre? bimestre,
        Quadrimestre? quadrimestre,
        DateOnly dataLimite,
        DateOnly dataConsolidacao,
        MatrizSaldos matriz)
        : base(id)
    {
        TenantId = tenantId;
        TipoDeclaracao = tipoDeclaracao;
        Exercicio = exercicio;
        Competencia = competencia;
        Bimestre = bimestre;
        Quadrimestre = quadrimestre;
        DataLimite = dataLimite;
        DataConsolidacao = dataConsolidacao;
        Matriz = matriz;
        Situacao = SituacaoDeclaracaoFiscal.Consolidada;
    }

    /// <summary>Tenant (ente publico) dono do registro. <see cref="IMustHaveTenant"/>.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Especie do demonstrativo (Msc/Rreo/Rgf/Dca).</summary>
    public TipoDeclaracaoFiscal TipoDeclaracao { get; private set; }

    /// <summary>Competencia mensal (preenchida quando MSC).</summary>
    public Competencia? Competencia { get; private set; }

    /// <summary>Periodo bimestral (preenchido quando RREO).</summary>
    public Bimestre? Bimestre { get; private set; }

    /// <summary>Periodo quadrimestral (preenchido quando RGF).</summary>
    public Quadrimestre? Quadrimestre { get; private set; }

    /// <summary>Ano de exercicio (sempre presente; base do DCA).</summary>
    public int Exercicio { get; private set; }

    /// <summary>Estado atual no ciclo de vida.</summary>
    public SituacaoDeclaracaoFiscal Situacao { get; private set; }

    /// <summary>Prazo legal/parametrizado de transmissao do periodo.</summary>
    public DateOnly DataLimite { get; private set; }

    /// <summary>Data de consolidacao.</summary>
    public DateOnly DataConsolidacao { get; private set; }

    /// <summary>Data de transmissao ao SICONFI (nula antes do envio).</summary>
    public DateOnly? DataTransmissao { get; private set; }

    /// <summary>Matriz de saldos consolidada (entidade-filha).</summary>
    public MatrizSaldos Matriz { get; private set; } = default!;

    /// <summary>Protocolo retornado pelo SICONFI na transmissao.</summary>
    public string? ProtocoloSiconfi { get; private set; }

    /// <summary>Motivo registrado em caso de rejeicao pelo SICONFI (trilha).</summary>
    public string? MotivoRejeicao { get; private set; }

    /// <summary>
    /// Consolida a MSC/declaracao a partir dos saldos recebidos, nascendo valida e em
    /// <see cref="SituacaoDeclaracaoFiscal.Consolidada"/> (I-2, I-3, I-4, I-7).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="tipoDeclaracao">Especie do demonstrativo.</param>
    /// <param name="exercicio">Ano de exercicio (&gt;= 1900).</param>
    /// <param name="competencia">Competencia mensal (obrigatoria quando MSC).</param>
    /// <param name="bimestre">Bimestre (obrigatorio quando RREO).</param>
    /// <param name="quadrimestre">Quadrimestre (obrigatorio quando RGF).</param>
    /// <param name="dataLimite">Prazo legal/parametrizado de transmissao.</param>
    /// <param name="dataConsolidacao">Data de consolidacao.</param>
    /// <param name="matriz">Matriz de saldos contabeis montada.</param>
    /// <returns>Nova <see cref="DeclaracaoFiscal"/> em <see cref="SituacaoDeclaracaoFiscal.Consolidada"/>.</returns>
    /// <exception cref="ArgumentNullException">Se a matriz for nula (I-2).</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o exercicio for inferior a 1900 (I-2).</exception>
    /// <exception cref="ArgumentException">Se o periodo for incompativel com o tipo (I-3).</exception>
    /// <exception cref="InvalidOperationException">Se a matriz estiver desbalanceada (I-7).</exception>
    public static DeclaracaoFiscal ConsolidarMatriz(
        Guid tenantId,
        TipoDeclaracaoFiscal tipoDeclaracao,
        int exercicio,
        Competencia? competencia,
        Bimestre? bimestre,
        Quadrimestre? quadrimestre,
        DateOnly dataLimite,
        DateOnly dataConsolidacao,
        MatrizSaldos matriz)
    {
        // I-2: tipo, exercicio e matriz sao obrigatorios.
        ArgumentNullException.ThrowIfNull(matriz);
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);

        // I-3: coerencia periodo x tipo.
        GarantirCoerenciaPeriodo(tipoDeclaracao, competencia, bimestre, quadrimestre);

        // I-7: matriz balanceada (partidas dobradas, PCASP).
        if (!matriz.EstaBalanceada)
        {
            throw new InvalidOperationException(
                "Matriz de saldos desbalanceada: total de debitos deve ser igual ao total de creditos.");
        }

        return new DeclaracaoFiscal(
            DeclaracaoFiscalId.New(),
            tenantId,
            tipoDeclaracao,
            exercicio,
            competencia,
            bimestre,
            quadrimestre,
            dataLimite,
            dataConsolidacao,
            matriz);
    }

    /// <summary>
    /// Transmite a declaracao consolidada ao SICONFI, passando a
    /// <see cref="SituacaoDeclaracaoFiscal.Transmitida"/> (I-5). Emite <see cref="MscEnviadaSiconfi"/>
    /// quando MSC ou <see cref="DeclaracaoTransmitida"/> nos demais tipos (I-6).
    /// </summary>
    /// <param name="dataTransmissao">Data da transmissao.</param>
    /// <param name="protocolo">Protocolo retornado pelo SICONFI.</param>
    /// <exception cref="ArgumentException">Se o protocolo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for Consolidada (I-5, I-10).</exception>
    public void TransmitirSiconfi(DateOnly dataTransmissao, string protocolo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocolo);

        // I-5/I-10: so a partir de Consolidada (estado Transmissivel).
        if (Situacao != SituacaoDeclaracaoFiscal.Consolidada)
        {
            throw new InvalidOperationException(
                $"Transmissao so e permitida a partir de Consolidada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoDeclaracaoFiscal.Transmitida;
        DataTransmissao = dataTransmissao;
        ProtocoloSiconfi = protocolo;

        // I-6: MSC emite MscEnviadaSiconfi; demais tipos emitem DeclaracaoTransmitida.
        if (TipoDeclaracao == TipoDeclaracaoFiscal.Msc)
        {
            RaiseDomainEvent(new MscEnviadaSiconfi(Id, Competencia?.ToString() ?? string.Empty, protocolo));
        }
        else
        {
            RaiseDomainEvent(new DeclaracaoTransmitida(Id, TipoDeclaracao.ToString(), DescreverPeriodo()));
        }
    }

    /// <summary>
    /// Registra a homologacao pelo SICONFI/STN, passando a
    /// <see cref="SituacaoDeclaracaoFiscal.Homologada"/> e emitindo <see cref="DeclaracaoHomologada"/> (I-8).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a situacao nao for Transmitida (I-8, I-10).</exception>
    public void Homologar()
    {
        // I-8/I-10: so a partir de Transmitida.
        if (Situacao != SituacaoDeclaracaoFiscal.Transmitida)
        {
            throw new InvalidOperationException(
                $"Homologacao so e permitida a partir de Transmitida. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoDeclaracaoFiscal.Homologada;
        RaiseDomainEvent(new DeclaracaoHomologada(Id, TipoDeclaracao.ToString()));
    }

    /// <summary>
    /// Registra a rejeicao pelo SICONFI, passando a <see cref="SituacaoDeclaracaoFiscal.Rejeitada"/>
    /// (terminal; correcao exige nova consolidacao) (I-9).
    /// </summary>
    /// <param name="motivo">Motivo da rejeicao (registrado para trilha).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for Transmitida (I-9, I-10).</exception>
    public void Rejeitar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        // I-9/I-10: so a partir de Transmitida.
        if (Situacao != SituacaoDeclaracaoFiscal.Transmitida)
        {
            throw new InvalidOperationException(
                $"Rejeicao so e permitida a partir de Transmitida. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoDeclaracaoFiscal.Rejeitada;
        MotivoRejeicao = motivo;
    }

    private static void GarantirCoerenciaPeriodo(
        TipoDeclaracaoFiscal tipo,
        Competencia? competencia,
        Bimestre? bimestre,
        Quadrimestre? quadrimestre)
    {
        switch (tipo)
        {
            case TipoDeclaracaoFiscal.Msc when competencia is null:
                throw new ArgumentException("Declaracao do tipo Msc exige Competencia.", nameof(competencia));
            case TipoDeclaracaoFiscal.Rreo when bimestre is null:
                throw new ArgumentException("Declaracao do tipo Rreo exige Bimestre.", nameof(bimestre));
            case TipoDeclaracaoFiscal.Rgf when quadrimestre is null:
                throw new ArgumentException("Declaracao do tipo Rgf exige Quadrimestre.", nameof(quadrimestre));
            case TipoDeclaracaoFiscal.Msc when bimestre is not null || quadrimestre is not null:
            case TipoDeclaracaoFiscal.Rreo when competencia is not null || quadrimestre is not null:
            case TipoDeclaracaoFiscal.Rgf when competencia is not null || bimestre is not null:
            case TipoDeclaracaoFiscal.Dca when competencia is not null || bimestre is not null || quadrimestre is not null:
                throw new ArgumentException("Periodo incompativel com o tipo de declaracao.", nameof(tipo));
            default:
                break;
        }
    }

    private string DescreverPeriodo()
        => TipoDeclaracao switch
        {
            TipoDeclaracaoFiscal.Msc => Competencia?.ToString() ?? string.Empty,
            TipoDeclaracaoFiscal.Rreo => Bimestre?.ToString() ?? string.Empty,
            TipoDeclaracaoFiscal.Rgf => Quadrimestre?.ToString() ?? string.Empty,
            _ => Exercicio.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
}
