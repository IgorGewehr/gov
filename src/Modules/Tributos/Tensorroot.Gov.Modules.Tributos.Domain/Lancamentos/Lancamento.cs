using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

/// <summary>Identificador forte do agregado <see cref="Lancamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LancamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LancamentoId"/>.</returns>
    public static LancamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Espécie tributária do lançamento.</summary>
public enum TipoTributo
{
    /// <summary>Imposto Predial e Territorial Urbano.</summary>
    Iptu = 1,

    /// <summary>Imposto Sobre Serviços de Qualquer Natureza.</summary>
    Iss = 2,

    /// <summary>Imposto sobre Transmissão de Bens Imóveis.</summary>
    Itbi = 3,

    /// <summary>Taxa (poder de polícia ou serviço).</summary>
    Taxa = 4,

    /// <summary>Contribuição de Melhoria.</summary>
    ContribuicaoMelhoria = 5,

    /// <summary>Contribuição para Custeio da Iluminação Pública.</summary>
    Cosip = 6,
}

/// <summary>Situação (estado) do lançamento tributário.</summary>
public enum SituacaoLancamento
{
    /// <summary>Em aberto (exigível, ainda não pago).</summary>
    Aberto = 1,

    /// <summary>Pago/quitado.</summary>
    Pago = 2,

    /// <summary>Inscrito em Dívida Ativa.</summary>
    InscritoEmDividaAtiva = 3,

    /// <summary>Cancelado.</summary>
    Cancelado = 4,
}

/// <summary>
/// Lançamento tributário (crédito tributário constituído — CTN art. 142): o ato que
/// torna a obrigação líquida e exigível contra o contribuinte.
/// </summary>
public sealed class Lancamento : AggregateRoot<LancamentoId>, IMustHaveTenant
{
    private Lancamento()
    {
    }

    private Lancamento(
        LancamentoId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        TipoTributo tipoTributo,
        Competencia competencia,
        ValorMonetario valorPrincipal,
        DateOnly vencimento,
        ImovelId? imovelId)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        TipoTributo = tipoTributo;
        Competencia = competencia;
        ValorPrincipal = valorPrincipal;
        Vencimento = vencimento;
        ImovelId = imovelId;
        Situacao = SituacaoLancamento.Aberto;
        RaiseDomainEvent(new CreditoTributarioLancado(id, contribuinteId, valorPrincipal.Valor));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte devedor.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Espécie tributária.</summary>
    public TipoTributo TipoTributo { get; private set; }

    /// <summary>Competência fiscal.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Valor principal lançado.</summary>
    public ValorMonetario ValorPrincipal { get; private set; } = default!;

    /// <summary>Data de vencimento.</summary>
    public DateOnly Vencimento { get; private set; }

    /// <summary>
    /// Imóvel de origem, quando o lançamento decorre do cadastro imobiliário (IPTU). Nulo para
    /// lançamentos não vinculados a imóvel (ex.: ISS).
    /// </summary>
    public ImovelId? ImovelId { get; private set; }

    /// <summary>Situação atual.</summary>
    public SituacaoLancamento Situacao { get; private set; }

    /// <summary>Constitui (lança) o crédito tributário.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte devedor.</param>
    /// <param name="tipoTributo">Espécie tributária.</param>
    /// <param name="competencia">Competência fiscal.</param>
    /// <param name="valorPrincipal">Valor principal.</param>
    /// <param name="vencimento">Data de vencimento.</param>
    /// <returns>Novo <see cref="Lancamento"/> em aberto.</returns>
    public static Lancamento Lancar(
        Guid tenantId,
        ContribuinteId contribuinteId,
        TipoTributo tipoTributo,
        Competencia competencia,
        ValorMonetario valorPrincipal,
        DateOnly vencimento)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentNullException.ThrowIfNull(valorPrincipal);
        return new Lancamento(LancamentoId.New(), tenantId, contribuinteId, tipoTributo, competencia, valorPrincipal, vencimento, imovelId: null);
    }

    /// <summary>
    /// Lança o IPTU anual de ofício (CTN art. 142/149) de um imóvel: uma constituição por exercício,
    /// vinculada ao imóvel de origem. A competência usa o mês 1 (lançamento anual).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte (proprietário do imóvel).</param>
    /// <param name="imovelId">Imóvel de origem.</param>
    /// <param name="exercicio">Exercício fiscal (ano).</param>
    /// <param name="valorPrincipal">IPTU devido apurado.</param>
    /// <param name="vencimento">Vencimento da cota única / 1ª parcela.</param>
    /// <returns>Novo <see cref="Lancamento"/> de IPTU em aberto.</returns>
    public static Lancamento LancarIptu(
        Guid tenantId,
        ContribuinteId contribuinteId,
        ImovelId imovelId,
        int exercicio,
        ValorMonetario valorPrincipal,
        DateOnly vencimento)
    {
        ArgumentNullException.ThrowIfNull(valorPrincipal);
        return new Lancamento(
            LancamentoId.New(),
            tenantId,
            contribuinteId,
            TipoTributo.Iptu,
            Competencia.De(exercicio, 1),
            valorPrincipal,
            vencimento,
            imovelId);
    }

    /// <summary>Registra a quitação do lançamento.</summary>
    /// <exception cref="InvalidOperationException">Se o lançamento não estiver em aberto.</exception>
    public void RegistrarPagamento()
    {
        if (Situacao != SituacaoLancamento.Aberto)
        {
            throw new InvalidOperationException($"Só é possível pagar um lançamento em aberto. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoLancamento.Pago;
        RaiseDomainEvent(new PagamentoRegistrado(Id));
    }

    /// <summary>Marca o lançamento como inscrito em Dívida Ativa (somente se vencido e em aberto).</summary>
    /// <param name="hoje">Data de referência.</param>
    /// <exception cref="InvalidOperationException">Se não estiver em aberto ou ainda não vencido.</exception>
    public void InscreverEmDividaAtiva(DateOnly hoje)
    {
        if (Situacao != SituacaoLancamento.Aberto)
        {
            throw new InvalidOperationException($"Apenas lançamentos em aberto podem ser inscritos em dívida ativa. Situação atual: {Situacao}.");
        }

        if (hoje <= Vencimento)
        {
            throw new InvalidOperationException("O lançamento ainda não está vencido.");
        }

        Situacao = SituacaoLancamento.InscritoEmDividaAtiva;
        RaiseDomainEvent(new LancamentoInscritoEmDividaAtiva(Id, ContribuinteId));
    }

    /// <summary>Cancela o lançamento (vedado se já pago).</summary>
    /// <exception cref="InvalidOperationException">Se o lançamento já estiver pago.</exception>
    public void Cancelar()
    {
        if (Situacao == SituacaoLancamento.Pago)
        {
            throw new InvalidOperationException("Não é possível cancelar um lançamento já pago.");
        }

        Situacao = SituacaoLancamento.Cancelado;
    }
}
