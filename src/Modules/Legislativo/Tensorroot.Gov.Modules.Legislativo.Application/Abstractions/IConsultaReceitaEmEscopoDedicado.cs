using Tensorroot.Gov.Modules.Financas.Contracts;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;

/// <summary>
/// PORTA que consulta a base do art. 29-A em Financas (via <see cref="IConsultaReceitaParaLimiteLegislativo"/>)
/// num ESCOPO DE DI DEDICADO, para a apuracao do limite de despesa da Camara (W9.5).
/// <para>
/// Motivacao (guarda H5 — <c>ScopeDbContextHolder</c>): o handler da apuracao ja resolveu o
/// <c>LegislativoDbContext</c> no escopo da requisicao; chamar a porta de leitura de Financas
/// (<see cref="IConsultaReceitaParaLimiteLegislativo"/>) direto no mesmo escopo resolveria o
/// <c>FinancasDbContext</c> lado-a-lado (dois <c>ModuleDbContext</c> no mesmo escopo). Esta porta isola a
/// consulta num escopo proprio (com o tenant corrente reaplicado, preservando o banco dedicado e os
/// Global Query Filters). Implementada no ApiHost (depende de <c>IServiceScopeFactory</c> +
/// <c>TenantOverride</c>), mesmo padrao do <c>IConsultaContratoEmEscopoDedicado</c>.
/// </para>
/// <para>
/// MULTI-TENANT: Executivo e Camara sao tenants distintos. Quando o tenant corrente (Camara) NAO detem a
/// contabilidade municipal, a porta retorna <c>null</c> e a base e informada manualmente na apuracao
/// (entrada auditada). A integracao cross-tenant oficial (leitura da receita do Executivo a partir da
/// Camara) e // TODO(M10): depende do contrato de federacao de dados entre os tenants do mesmo municipio.
/// </para>
/// </summary>
public interface IConsultaReceitaEmEscopoDedicado
{
    /// <summary>
    /// Apura a base do art. 29-A (receita tributaria + transferencias do exercicio) no tenant corrente,
    /// em escopo dedicado. Retorna <c>null</c> quando o tenant nao detem a receita do exercicio.
    /// </summary>
    /// <param name="exercicio">Exercicio (ano) da arrecadacao (o "exercicio anterior" do caput).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A base discriminada, ou <c>null</c>.</returns>
    Task<ReceitaArt29ADto?> ConsultarBaseAsync(int exercicio, CancellationToken cancellationToken);
}
