namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;

/// <summary>
/// Resultado deterministico e auditavel do calculo da folha de um servidor: bases apuradas, descontos
/// legais (INSS/RPPS/IRRF) e o liquido. Imutavel; reflete exatamente as tabelas parametrizadas usadas.
/// </summary>
/// <param name="ServidorId">Servidor calculado.</param>
/// <param name="TotalProventos">Soma dos proventos.</param>
/// <param name="BaseInss">Base de incidencia do INSS.</param>
/// <param name="DescontoInss">INSS retido (RGPS).</param>
/// <param name="BaseRpps">Base de incidencia do RPPS.</param>
/// <param name="DescontoRpps">RPPS retido (servidor efetivo).</param>
/// <param name="BaseIrrf">Base bruta tributavel do IRRF (antes das deducoes legais).</param>
/// <param name="DescontoIrrf">IRRF retido.</param>
/// <param name="OutrosDescontos">Demais descontos informados manualmente (consignados/pensao/etc.).</param>
/// <param name="TotalDescontos">Soma de todos os descontos (legais + outros).</param>
/// <param name="Liquido">Liquido a pagar (proventos menos descontos), nao-negativo.</param>
public sealed record ResultadoCalculoServidor(
    Guid ServidorId,
    decimal TotalProventos,
    decimal BaseInss,
    decimal DescontoInss,
    decimal BaseRpps,
    decimal DescontoRpps,
    decimal BaseIrrf,
    decimal DescontoIrrf,
    decimal OutrosDescontos,
    decimal TotalDescontos,
    decimal Liquido)
{
    /// <summary>Desconto previdenciario efetivo (INSS para RGPS, RPPS para efetivo).</summary>
    public decimal DescontoPrevidenciario => DescontoInss + DescontoRpps;
}
