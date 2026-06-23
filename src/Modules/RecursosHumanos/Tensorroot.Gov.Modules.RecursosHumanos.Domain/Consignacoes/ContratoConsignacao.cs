using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>Identificador forte do agregado <see cref="ContratoConsignacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ContratoConsignacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ContratoConsignacaoId"/>.</returns>
    public static ContratoConsignacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// CONTRATO DE CONSIGNACAO de um servidor (Lei 14.131/2021): vincula servidor x consignataria x rubrica,
/// com parcela mensal, quantidade de parcelas e ciclo de vida proprio (Averbada -&gt; Suspensa/Quitada/
/// Cancelada). A AVERBACAO so e admitida se a parcela couber na MARGEM DISPONIVEL do balde
/// (<see cref="GrupoMargem"/>) na competencia — invariante critica (a folha jamais estoura a margem).
/// Congela um snapshot do grupo/categoria da rubrica no momento da averbacao (a parametrizacao pode mudar
/// sem reescrever o historico). Reusa a espinha do <c>Afastamento</c>: tipo escolhido pelo usuario, efeito
/// derivado de parametrizacao, gancho deterministico na folha. Raiz de agregado.
/// </summary>
public sealed class ContratoConsignacao : AggregateRoot<ContratoConsignacaoId>, IMustHaveTenant
{
    private ContratoConsignacao()
    {
    }

    private ContratoConsignacao(
        ContratoConsignacaoId id,
        Guid tenantId,
        ServidorId servidorId,
        ConsignatariaId consignatariaId,
        string codigoRubrica,
        CategoriaConsignavel categoria,
        GrupoMargem grupoMargem,
        string? numeroContratoExterno,
        decimal valorParcela,
        int quantidadeParcelas,
        DateOnly dataAverbacao)
        : base(id)
    {
        TenantId = tenantId;
        ServidorId = servidorId;
        ConsignatariaId = consignatariaId;
        CodigoRubrica = codigoRubrica;
        Categoria = categoria;
        GrupoMargem = grupoMargem;
        NumeroContratoExterno = numeroContratoExterno;
        ValorParcela = valorParcela;
        QuantidadeParcelas = quantidadeParcelas;
        ParcelasPagas = 0;
        DataAverbacao = dataAverbacao;
        Situacao = SituacaoConsignacao.Averbada;
        RaiseDomainEvent(new ConsignacaoAverbada(id, servidorId, grupoMargem, valorParcela));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Servidor consignante (referencia por Id ao agregado <see cref="Servidor"/>).</summary>
    public ServidorId ServidorId { get; private set; }

    /// <summary>Consignataria (banco/entidade) destinataria do desconto.</summary>
    public ConsignatariaId ConsignatariaId { get; private set; }

    /// <summary>Snapshot do codigo da rubrica consignavel (referencia S-1010) usada no lancamento da folha.</summary>
    public string CodigoRubrica { get; private set; } = default!;

    /// <summary>Snapshot da categoria (prioridade no corte por margem).</summary>
    public CategoriaConsignavel Categoria { get; private set; }

    /// <summary>Snapshot do balde de margem consumido (reserva legal).</summary>
    public GrupoMargem GrupoMargem { get; private set; }

    /// <summary>Numero do contrato no sistema da consignataria (referencia externa; opcional).</summary>
    public string? NumeroContratoExterno { get; private set; }

    /// <summary>Valor mensal da parcela consignada.</summary>
    public decimal ValorParcela { get; private set; }

    /// <summary>Quantidade total de parcelas do contrato.</summary>
    public int QuantidadeParcelas { get; private set; }

    /// <summary>Quantidade de parcelas ja pagas (incrementada a cada folha).</summary>
    public int ParcelasPagas { get; private set; }

    /// <summary>Data da averbacao.</summary>
    public DateOnly DataAverbacao { get; private set; }

    /// <summary>Situacao no ciclo de vida do contrato.</summary>
    public SituacaoConsignacao Situacao { get; private set; }

    /// <summary>Indica se o contrato esta averbado (vigente para lancamento e consumo de margem).</summary>
    public bool EstaAverbada => Situacao == SituacaoConsignacao.Averbada;

    /// <summary>Parcelas restantes (nunca negativa).</summary>
    public int ParcelasRestantes => Math.Max(0, QuantidadeParcelas - ParcelasPagas);

    /// <summary>
    /// Averba um contrato de consignacao — so admitida se a parcela couber na MARGEM DISPONIVEL do balde.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="servidorId">Servidor consignante.</param>
    /// <param name="consignatariaId">Consignataria destinataria.</param>
    /// <param name="consignatariaAtiva">Se a consignataria esta ativa (pre-validado na Application).</param>
    /// <param name="rubrica">Rubrica consignavel (fonte do grupo/categoria — snapshot).</param>
    /// <param name="numeroContratoExterno">Numero do contrato externo (opcional).</param>
    /// <param name="valorParcela">Valor mensal da parcela (&gt; 0).</param>
    /// <param name="quantidadeParcelas">Quantidade de parcelas (&gt; 0).</param>
    /// <param name="dataAverbacao">Data da averbacao.</param>
    /// <param name="margem">Margem do servidor na competencia (limite/comprometido/disponivel por balde).</param>
    /// <returns>Novo <see cref="ContratoConsignacao"/> averbado.</returns>
    /// <exception cref="ArgumentNullException">Se a rubrica ou a margem forem nulas.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se valor/quantidade nao forem positivos.</exception>
    /// <exception cref="ConsignacaoException">Se a consignataria estiver inativa ou a parcela estourar a margem do balde.</exception>
    public static ContratoConsignacao Averbar(
        Guid tenantId,
        ServidorId servidorId,
        ConsignatariaId consignatariaId,
        bool consignatariaAtiva,
        RubricaConsignavel rubrica,
        string? numeroContratoExterno,
        decimal valorParcela,
        int quantidadeParcelas,
        DateOnly dataAverbacao,
        MargemConsignavel margem)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(margem);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valorParcela);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidadeParcelas);

        if (!consignatariaAtiva)
        {
            throw new ConsignacaoException("Consignataria suspensa/inativa nao pode receber novas averbacoes.");
        }

        // INVARIANTE CRITICA (Lei 14.131/2021): a parcela tem de caber na margem disponivel do balde —
        // a folha jamais estoura a margem. A margem ja desconta o que esta comprometido pelos contratos
        // Averbados vigentes do servidor (reserva legal por balde, baldes independentes).
        if (!margem.ComportaParcela(rubrica.GrupoMargem, valorParcela))
        {
            throw new ConsignacaoException(
                $"Parcela de {valorParcela:N2} excede a margem disponivel do balde {rubrica.GrupoMargem} " +
                $"({margem.Disponivel(rubrica.GrupoMargem):N2} de limite {margem.Limite(rubrica.GrupoMargem):N2}).");
        }

        return new ContratoConsignacao(
            ContratoConsignacaoId.New(),
            tenantId,
            servidorId,
            consignatariaId,
            rubrica.Codigo,
            rubrica.Categoria,
            rubrica.GrupoMargem,
            string.IsNullOrWhiteSpace(numeroContratoExterno) ? null : numeroContratoExterno.Trim(),
            valorParcela,
            quantidadeParcelas,
            dataAverbacao);
    }

    /// <summary>Suspende a consignacao (deixa de lancar na folha; libera margem). Reversivel via <see cref="Reativar"/>.</summary>
    /// <param name="motivo">Motivo da suspensao (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver averbada.</exception>
    public void Suspender(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao != SituacaoConsignacao.Averbada)
        {
            throw new InvalidOperationException($"So consignacao Averbada pode ser suspensa. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoConsignacao.Suspensa;
        RaiseDomainEvent(new ConsignacaoSuspensa(Id, ServidorId));
    }

    /// <summary>
    /// Reativa uma consignacao suspensa — re-checa a margem (a base pode ter caido por afastamento).
    /// </summary>
    /// <param name="margem">Margem do servidor na competencia de reativacao.</param>
    /// <exception cref="ArgumentNullException">Se a margem for nula.</exception>
    /// <exception cref="InvalidOperationException">Se nao estiver suspensa.</exception>
    /// <exception cref="ConsignacaoException">Se a parcela nao couber mais na margem do balde.</exception>
    public void Reativar(MargemConsignavel margem)
    {
        ArgumentNullException.ThrowIfNull(margem);
        if (Situacao != SituacaoConsignacao.Suspensa)
        {
            throw new InvalidOperationException($"So consignacao Suspensa pode ser reativada. Situacao atual: {Situacao}.");
        }

        if (!margem.ComportaParcela(GrupoMargem, ValorParcela))
        {
            throw new ConsignacaoException(
                $"Reativacao impedida: parcela de {ValorParcela:N2} excede a margem disponivel do balde {GrupoMargem} " +
                $"({margem.Disponivel(GrupoMargem):N2}).");
        }

        Situacao = SituacaoConsignacao.Averbada;
        RaiseDomainEvent(new ConsignacaoAverbada(Id, ServidorId, GrupoMargem, ValorParcela));
    }

    /// <summary>
    /// Registra o pagamento de uma parcela (a cada folha que lancou o desconto). Ao atingir
    /// <see cref="QuantidadeParcelas"/>, quita o contrato. Idempotente apos a quitacao (nao incrementa mais).
    /// </summary>
    /// <exception cref="InvalidOperationException">Se nao estiver averbada.</exception>
    public void RegistrarParcelaPaga()
    {
        if (Situacao != SituacaoConsignacao.Averbada)
        {
            throw new InvalidOperationException($"So consignacao Averbada acumula parcelas pagas. Situacao atual: {Situacao}.");
        }

        if (ParcelasPagas >= QuantidadeParcelas)
        {
            return;
        }

        ParcelasPagas++;
        if (ParcelasPagas >= QuantidadeParcelas)
        {
            Situacao = SituacaoConsignacao.Quitada;
            RaiseDomainEvent(new ConsignacaoQuitada(Id, ServidorId));
        }
    }

    /// <summary>Cancela a consignacao antes da quitacao (libera margem). Nao reativavel.</summary>
    /// <param name="motivo">Motivo do cancelamento (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se ja estiver quitada/cancelada.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (Situacao is SituacaoConsignacao.Quitada or SituacaoConsignacao.Cancelada)
        {
            throw new InvalidOperationException($"Consignacao {Situacao} nao pode ser cancelada.");
        }

        Situacao = SituacaoConsignacao.Cancelada;
        RaiseDomainEvent(new ConsignacaoCancelada(Id, ServidorId));
    }
}
