using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;

/// <summary>Identificador forte do agregado <see cref="Dam"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DamId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DamId"/>.</returns>
    public static DamId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Documento de Arrecadação Municipal (guia/carnê) de um lançamento: permite cota única OU N
/// parcelas, cada uma com seu vencimento. Reusável por todas as espécies tributárias; nesta fase
/// gera o carnê do IPTU. O nº de parcelas/valor mínimo/vencimentos são definidos pelo calendário
/// fiscal municipal (parametrizado). Ver M6-DESIGN §5.
/// </summary>
public sealed class Dam : AggregateRoot<DamId>, IMustHaveTenant
{
    private readonly List<Parcela> _parcelas = [];

    private Dam()
    {
    }

    private Dam(DamId id, Guid tenantId, LancamentoId lancamentoId, ContribuinteId contribuinteId, ValorMonetario valorTotal)
        : base(id)
    {
        TenantId = tenantId;
        LancamentoId = lancamentoId;
        ContribuinteId = contribuinteId;
        ValorTotal = valorTotal;
        RaiseDomainEvent(new DamGerado(id, tenantId, lancamentoId));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Lançamento de origem.</summary>
    public LancamentoId LancamentoId { get; private set; }

    /// <summary>Contribuinte devedor.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Valor total do documento (soma das parcelas).</summary>
    public ValorMonetario ValorTotal { get; private set; } = default!;

    /// <summary>Parcelas geradas (cota única = 1 parcela).</summary>
    public IReadOnlyList<Parcela> Parcelas => _parcelas;

    /// <summary>Indica se todas as parcelas estão pagas.</summary>
    public bool Quitado => _parcelas.Count > 0 && _parcelas.All(p => p.Paga);

    /// <summary>
    /// Gera o DAM parcelando o valor do lançamento em <paramref name="numeroParcelas"/> parcelas
    /// mensais a partir do primeiro vencimento. O resíduo de arredondamento é somado à 1ª parcela
    /// para que a soma feche exatamente com o total.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="lancamentoId">Lançamento de origem.</param>
    /// <param name="contribuinteId">Contribuinte devedor.</param>
    /// <param name="valorTotal">Valor total a parcelar.</param>
    /// <param name="numeroParcelas">Número de parcelas (1 = cota única).</param>
    /// <param name="primeiroVencimento">Vencimento da 1ª parcela.</param>
    /// <returns>Novo <see cref="Dam"/> com as parcelas geradas.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o número de parcelas for menor que 1.</exception>
    public static Dam Gerar(
        Guid tenantId,
        LancamentoId lancamentoId,
        ContribuinteId contribuinteId,
        ValorMonetario valorTotal,
        int numeroParcelas,
        DateOnly primeiroVencimento)
    {
        ArgumentNullException.ThrowIfNull(valorTotal);
        ArgumentOutOfRangeException.ThrowIfLessThan(numeroParcelas, 1);

        var dam = new Dam(DamId.New(), tenantId, lancamentoId, contribuinteId, valorTotal);

        var valorParcela = ValorMonetario.De(decimal.Round(valorTotal.Valor / numeroParcelas, 2, MidpointRounding.AwayFromZero));
        var residuo = ValorMonetario.De(valorTotal.Valor - (valorParcela.Valor * (numeroParcelas - 1)));

        for (var numero = 1; numero <= numeroParcelas; numero++)
        {
            // A última parcela absorve o resíduo de arredondamento (garante soma exata = total).
            var valor = numero == numeroParcelas ? residuo : valorParcela;
            var vencimento = primeiroVencimento.AddMonths(numero - 1);
            dam._parcelas.Add(Parcela.Criar(dam.Id, numero, valor, vencimento));
        }

        return dam;
    }

    /// <summary>Registra o pagamento de uma parcela.</summary>
    /// <param name="numeroParcela">Número da parcela (1..N).</param>
    /// <param name="dataPagamento">Data do pagamento.</param>
    /// <exception cref="InvalidOperationException">Se a parcela não existir.</exception>
    public void RegistrarPagamentoParcela(int numeroParcela, DateOnly dataPagamento)
    {
        var parcela = _parcelas.FirstOrDefault(p => p.Numero == numeroParcela)
            ?? throw new InvalidOperationException($"Parcela {numeroParcela} inexistente no DAM.");

        parcela.RegistrarPagamento(dataPagamento);

        if (Quitado)
        {
            RaiseDomainEvent(new DamQuitado(Id, TenantId, LancamentoId));
        }
    }
}
