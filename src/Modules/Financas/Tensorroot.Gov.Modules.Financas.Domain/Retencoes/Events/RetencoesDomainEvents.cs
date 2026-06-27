using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Domain.Retencoes.Events;

/// <summary>
/// Retenção/consignação apurada sobre uma liquidação (IRRF/INSS/ISS/caução). Gancho para a
/// contabilização do passivo extra-orçamentário a recolher (consignação — Lei 4.320/64).
/// </summary>
/// <param name="LiquidacaoId">Liquidação onerada.</param>
/// <param name="RetencaoId">Retenção apurada.</param>
/// <param name="Natureza">Natureza do tributo/consignação.</param>
/// <param name="Valor">Valor retido.</param>
/// <param name="Data">Data de competência (data da liquidação).</param>
public sealed record RetencaoApurada(
    LiquidacaoId LiquidacaoId,
    RetencaoId RetencaoId,
    NaturezaRetencao Natureza,
    decimal Valor,
    DateOnly Data) : IDomainEvent;

/// <summary>
/// Guia de recolhimento de retenções emitida (DARF/GPS/guia municipal). Reúne consignações a recolher
/// a um mesmo favorecido/código de receita.
/// </summary>
/// <param name="GuiaRecolhimentoId">Guia emitida.</param>
/// <param name="Natureza">Natureza recolhida.</param>
/// <param name="Valor">Valor total da guia.</param>
public sealed record GuiaRecolhimentoEmitida(
    GuiaRecolhimentoId GuiaRecolhimentoId,
    NaturezaRetencao Natureza,
    decimal Valor) : IDomainEvent;

/// <summary>
/// Recolhimento efetivado: baixa do passivo extra-orçamentário (dispêndio extra-orçamentário).
/// </summary>
/// <param name="GuiaRecolhimentoId">Guia recolhida.</param>
/// <param name="Natureza">Natureza recolhida.</param>
/// <param name="Valor">Valor recolhido.</param>
/// <param name="Data">Data do recolhimento.</param>
public sealed record RecolhimentoEfetuado(
    GuiaRecolhimentoId GuiaRecolhimentoId,
    NaturezaRetencao Natureza,
    decimal Valor,
    DateOnly Data) : IDomainEvent;
