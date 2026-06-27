using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>Identificador forte do agregado <see cref="Apolice"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ApoliceId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ApoliceId"/>.</returns>
    public static ApoliceId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Apólice de seguro de um veículo da frota (raiz de agregado próprio): vincula o veículo a uma
/// seguradora, com vigência (início/fim), cobertura contratada, prêmio e categoria — seja o seguro
/// obrigatório (DPVAT/SPVAT — Lei 6.194/1974, LC 207/2024, conforme cobrança vigente) ou os seguros
/// facultativos de frota (casco/RCF-V/multirrisco). Controla o ciclo de vigência (vigente → vencida
/// ou cancelada) e habilita o alerta de vencimento (renovação tempestiva da cobertura).
/// </summary>
public sealed class Apolice : AggregateRoot<ApoliceId>, IMustHaveTenant
{
    private Apolice()
    {
    }

    private Apolice(
        ApoliceId id,
        Guid tenantId,
        VeiculoId veiculoId,
        CategoriaSeguro categoria,
        string seguradora,
        string numeroApolice,
        DateOnly inicioVigencia,
        DateOnly fimVigencia,
        ValorMonetario premio,
        ValorMonetario importanciaSegurada)
        : base(id)
    {
        TenantId = tenantId;
        VeiculoId = veiculoId;
        Categoria = categoria;
        Seguradora = seguradora;
        NumeroApolice = numeroApolice;
        InicioVigencia = inicioVigencia;
        FimVigencia = fimVigencia;
        Premio = premio;
        ImportanciaSegurada = importanciaSegurada;
        Situacao = SituacaoApolice.Vigente;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Veículo segurado por esta apólice.</summary>
    public VeiculoId VeiculoId { get; private set; }

    /// <summary>Categoria do seguro (obrigatório/casco/RCF-V/frota).</summary>
    public CategoriaSeguro Categoria { get; private set; }

    /// <summary>Seguradora contratada (razão social).</summary>
    public string Seguradora { get; private set; } = default!;

    /// <summary>Número da apólice junto à seguradora.</summary>
    public string NumeroApolice { get; private set; } = default!;

    /// <summary>Resumo da cobertura contratada (texto descritivo das garantias).</summary>
    public string? Cobertura { get; private set; }

    /// <summary>Início da vigência da apólice.</summary>
    public DateOnly InicioVigencia { get; private set; }

    /// <summary>Fim da vigência da apólice (alvo do alerta de vencimento).</summary>
    public DateOnly FimVigencia { get; private set; }

    /// <summary>Prêmio (valor pago pela cobertura).</summary>
    public ValorMonetario Premio { get; private set; } = default!;

    /// <summary>Importância segurada (limite máximo de indenização / valor segurado).</summary>
    public ValorMonetario ImportanciaSegurada { get; private set; } = default!;

    /// <summary>Situação atual da apólice no seu ciclo de vigência.</summary>
    public SituacaoApolice Situacao { get; private set; }

    /// <summary>Indica se a apólice é do seguro obrigatório (DPVAT/SPVAT).</summary>
    public bool EhSeguroObrigatorio => Categoria == CategoriaSeguro.Obrigatorio;

    /// <summary>Indica se a cobertura está vigente na data informada (dentro da vigência e não cancelada).</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns><c>true</c> se a cobertura estiver ativa em <paramref name="hoje"/>.</returns>
    public bool CoberturaVigenteEm(DateOnly hoje)
        => Situacao == SituacaoApolice.Vigente && hoje >= InicioVigencia && hoje <= FimVigencia;

    /// <summary>Dias até o fim da vigência a partir da data informada (negativo se já vencida).</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns>Dias até o vencimento (pode ser negativo).</returns>
    public int DiasParaVencer(DateOnly hoje) => FimVigencia.DayNumber - hoje.DayNumber;

    /// <summary>
    /// Contrata (registra) uma nova apólice de seguro para um veículo, com cobertura vigente.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="veiculoId">Veículo segurado.</param>
    /// <param name="categoria">Categoria do seguro.</param>
    /// <param name="seguradora">Seguradora contratada.</param>
    /// <param name="numeroApolice">Número da apólice.</param>
    /// <param name="inicioVigencia">Início da vigência.</param>
    /// <param name="fimVigencia">Fim da vigência (deve ser posterior ao início).</param>
    /// <param name="premio">Prêmio pago.</param>
    /// <param name="importanciaSegurada">Importância segurada.</param>
    /// <param name="cobertura">Resumo da cobertura (opcional).</param>
    /// <returns>Nova <see cref="Apolice"/> vigente.</returns>
    /// <exception cref="ArgumentException">Se seguradora ou número da apólice forem vazios.</exception>
    /// <exception cref="ArgumentNullException">Se prêmio ou importância segurada forem nulos.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o fim da vigência não for posterior ao início.</exception>
    public static Apolice Contratar(
        Guid tenantId,
        VeiculoId veiculoId,
        CategoriaSeguro categoria,
        string seguradora,
        string numeroApolice,
        DateOnly inicioVigencia,
        DateOnly fimVigencia,
        ValorMonetario premio,
        ValorMonetario importanciaSegurada,
        string? cobertura)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seguradora);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroApolice);
        ArgumentNullException.ThrowIfNull(premio);
        ArgumentNullException.ThrowIfNull(importanciaSegurada);
        if (fimVigencia <= inicioVigencia)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fimVigencia),
                $"Fim da vigência ({fimVigencia:yyyy-MM-dd}) deve ser posterior ao início ({inicioVigencia:yyyy-MM-dd}).");
        }

        var apolice = new Apolice(
            ApoliceId.New(),
            tenantId,
            veiculoId,
            categoria,
            seguradora,
            numeroApolice,
            inicioVigencia,
            fimVigencia,
            premio,
            importanciaSegurada)
        {
            Cobertura = cobertura,
        };

        apolice.RaiseDomainEvent(new ApoliceContratada(apolice.Id, veiculoId, fimVigencia));
        return apolice;
    }

    /// <summary>
    /// Renova a apólice estendendo a vigência para um novo período, com novo prêmio. Reativa a cobertura
    /// (de vencida para vigente) sem criar um novo registro. O novo período deve começar a partir do fim
    /// da vigência corrente (continuidade) e ser consistente (fim posterior ao início).
    /// </summary>
    /// <param name="novoInicioVigencia">Início do novo período (≥ fim da vigência atual).</param>
    /// <param name="novoFimVigencia">Fim do novo período (posterior ao novo início).</param>
    /// <param name="novoPremio">Prêmio do novo período.</param>
    /// <exception cref="ArgumentNullException">Se o prêmio for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o novo período for inconsistente ou retroagir.</exception>
    /// <exception cref="InvalidOperationException">Se a apólice estiver cancelada.</exception>
    public void Renovar(DateOnly novoInicioVigencia, DateOnly novoFimVigencia, ValorMonetario novoPremio)
    {
        ArgumentNullException.ThrowIfNull(novoPremio);
        if (Situacao == SituacaoApolice.Cancelada)
        {
            throw new InvalidOperationException("Apólice cancelada não pode ser renovada; contrate uma nova.");
        }

        if (novoInicioVigencia < FimVigencia)
        {
            throw new ArgumentOutOfRangeException(
                nameof(novoInicioVigencia),
                $"Renovação não pode retroagir antes do fim da vigência atual ({FimVigencia:yyyy-MM-dd}).");
        }

        if (novoFimVigencia <= novoInicioVigencia)
        {
            throw new ArgumentOutOfRangeException(
                nameof(novoFimVigencia), "Fim da nova vigência deve ser posterior ao novo início.");
        }

        InicioVigencia = novoInicioVigencia;
        FimVigencia = novoFimVigencia;
        Premio = novoPremio;
        Situacao = SituacaoApolice.Vigente;

        RaiseDomainEvent(new ApoliceContratada(Id, VeiculoId, novoFimVigencia));
    }

    /// <summary>
    /// Marca a apólice como vencida quando a vigência expira sem renovação. Idempotente: só transita de
    /// vigente para vencida e apenas se a data de referência for posterior ao fim da vigência.
    /// </summary>
    /// <param name="hoje">Data de referência.</param>
    /// <returns><c>true</c> se a apólice passou a vencida nesta chamada.</returns>
    public bool MarcarVencidaSeExpirou(DateOnly hoje)
    {
        if (Situacao == SituacaoApolice.Vigente && hoje > FimVigencia)
        {
            Situacao = SituacaoApolice.Vencida;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancela a apólice antes do fim da vigência (endosso de cancelamento), estado terminal.
    /// </summary>
    /// <param name="motivo">Motivo do cancelamento.</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a apólice já estiver cancelada.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao == SituacaoApolice.Cancelada)
        {
            throw new InvalidOperationException("Apólice já cancelada.");
        }

        Situacao = SituacaoApolice.Cancelada;
        MotivoCancelamento = motivo;
    }

    /// <summary>Motivo do cancelamento, quando cancelada.</summary>
    public string? MotivoCancelamento { get; private set; }
}
