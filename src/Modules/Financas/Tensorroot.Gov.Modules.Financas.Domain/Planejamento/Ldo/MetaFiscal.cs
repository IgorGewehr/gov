using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;

/// <summary>
/// Meta fiscal de um ano do triênio (LRF art. 4º §1º: exercício + 2 seguintes). Compõe o
/// Anexo de Metas Fiscais. Entidade-filha de <see cref="LeiDiretrizes"/>.
/// </summary>
public sealed class MetaFiscal : Entity<MetaFiscalId>
{
    private MetaFiscal()
    {
    }

    private MetaFiscal(
        MetaFiscalId id,
        LdoId ldoId,
        int ano,
        ValorMonetario receitaTotal,
        ValorMonetario despesaTotal,
        decimal resultadoPrimario,
        decimal resultadoNominal,
        ValorMonetario dividaConsolidada)
        : base(id)
    {
        LdoId = ldoId;
        Ano = ano;
        ReceitaTotal = receitaTotal;
        DespesaTotal = despesaTotal;
        ResultadoPrimario = resultadoPrimario;
        ResultadoNominal = resultadoNominal;
        DividaConsolidada = dividaConsolidada;
    }

    /// <summary>LDO à qual a meta fiscal pertence.</summary>
    public LdoId LdoId { get; private set; }

    /// <summary>Ano da meta (exercício, +1 ou +2).</summary>
    public int Ano { get; private set; }

    /// <summary>Receita total prevista.</summary>
    public ValorMonetario ReceitaTotal { get; private set; } = default!;

    /// <summary>Despesa total prevista.</summary>
    public ValorMonetario DespesaTotal { get; private set; } = default!;

    /// <summary>Resultado primário (pode ser negativo — déficit).</summary>
    public decimal ResultadoPrimario { get; private set; }

    /// <summary>Resultado nominal (pode ser negativo).</summary>
    public decimal ResultadoNominal { get; private set; }

    /// <summary>Dívida consolidada prevista.</summary>
    public ValorMonetario DividaConsolidada { get; private set; } = default!;

    /// <summary>Cria uma meta fiscal de um ano do triênio.</summary>
    /// <returns>Nova <see cref="MetaFiscal"/>.</returns>
    internal static MetaFiscal Criar(
        LdoId ldoId,
        int ano,
        ValorMonetario receitaTotal,
        ValorMonetario despesaTotal,
        decimal resultadoPrimario,
        decimal resultadoNominal,
        ValorMonetario dividaConsolidada)
    {
        ArgumentNullException.ThrowIfNull(receitaTotal);
        ArgumentNullException.ThrowIfNull(despesaTotal);
        ArgumentNullException.ThrowIfNull(dividaConsolidada);
        return new MetaFiscal(MetaFiscalId.New(), ldoId, ano, receitaTotal, despesaTotal, resultadoPrimario, resultadoNominal, dividaConsolidada);
    }

    /// <summary>Atualiza os valores da meta fiscal (re-definição idempotente por ano).</summary>
    internal void Atualizar(
        ValorMonetario receitaTotal,
        ValorMonetario despesaTotal,
        decimal resultadoPrimario,
        decimal resultadoNominal,
        ValorMonetario dividaConsolidada)
    {
        ArgumentNullException.ThrowIfNull(receitaTotal);
        ArgumentNullException.ThrowIfNull(despesaTotal);
        ArgumentNullException.ThrowIfNull(dividaConsolidada);
        ReceitaTotal = receitaTotal;
        DespesaTotal = despesaTotal;
        ResultadoPrimario = resultadoPrimario;
        ResultadoNominal = resultadoNominal;
        DividaConsolidada = dividaConsolidada;
    }
}
