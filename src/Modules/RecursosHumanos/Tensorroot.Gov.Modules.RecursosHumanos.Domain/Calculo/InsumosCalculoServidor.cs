using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;

/// <summary>
/// Verba de entrada do motor de calculo de um servidor: o valor da rubrica e as flags de incidencia
/// resolvidas a partir da <see cref="Rubricas.RubricaFolha"/> parametrizada (somar base por incidencia,
/// nunca por nome — pesquisa-folha-calculo §4). Objeto imutavel de transporte (dominio puro).
/// </summary>
/// <param name="Codigo">Codigo da rubrica (S-1010).</param>
/// <param name="EhProvento"><c>true</c> se provento; <c>false</c> se desconto.</param>
/// <param name="Valor">Valor apurado da verba (maior que zero).</param>
/// <param name="IncideInss">Integra a base de INSS.</param>
/// <param name="IncideRpps">Integra a base de RPPS.</param>
/// <param name="IncideIrrf">Integra a base do IRRF.</param>
public sealed record VerbaCalculo(
    Rubrica Codigo,
    bool EhProvento,
    decimal Valor,
    bool IncideInss,
    bool IncideRpps,
    bool IncideIrrf);

/// <summary>
/// Insumos do calculo da folha de um servidor numa competencia: o regime previdenciario, os
/// dependentes para deducao de IRRF, a pensao alimenticia e as verbas (proventos/descontos
/// manuais) ja resolvidas com suas incidencias. Dominio puro, sem I/O.
/// </summary>
/// <param name="ServidorId">Servidor calculado.</param>
/// <param name="Regime">Regime previdenciario (RPPS/RGPS).</param>
/// <param name="QuantidadeDependentes">Dependentes para deducao de IRRF.</param>
/// <param name="PensaoAlimenticia">Pensao alimenticia dedutivel do IRRF.</param>
/// <param name="Verbas">Verbas de entrada (proventos/descontos) com incidencias.</param>
public sealed record InsumosCalculoServidor(
    Guid ServidorId,
    RegimePrevidenciario Regime,
    int QuantidadeDependentes,
    decimal PensaoAlimenticia,
    IReadOnlyList<VerbaCalculo> Verbas);
