namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Resultado de uma baixa FEFO em um lote especifico: o lote consumido, seu numero/validade e a
/// quantidade retirada. Objeto de valor de rastreabilidade — a <see cref="Dispensacao"/> registra a
/// lista de baixas para reconstruir QUAL lote saiu (exigencia de rastreabilidade sanitaria/SNGPC).
/// </summary>
/// <param name="LoteId">Identificador do lote consumido.</param>
/// <param name="NumeroLote">Numero do lote (do fabricante).</param>
/// <param name="Validade">Validade do lote consumido.</param>
/// <param name="Quantidade">Quantidade retirada deste lote.</param>
public sealed record BaixaLote(
    LoteMedicamentoId LoteId,
    string NumeroLote,
    DateOnly Validade,
    decimal Quantidade);
