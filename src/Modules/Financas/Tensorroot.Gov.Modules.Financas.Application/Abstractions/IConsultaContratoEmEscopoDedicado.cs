using Tensorroot.Gov.Modules.Administracao.Contracts;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>
/// PORTA que consulta o status PNCP de um contrato (Administracao, via Contracts) num ESCOPO DE DI
/// DEDICADO, para a INVARIANTE DE BLOQUEIO do empenho (W9.1: contrato sem numero de controle PNCP nao
/// empenha — Lei 14.133/2021, art. 94).
/// <para>
/// Motivacao (guarda H5 — <c>ScopeDbContextHolder</c>): o <c>EmpenharHandler</c> ja resolveu o
/// <c>FinancasDbContext</c> no escopo da requisicao; chamar a porta de leitura do Administracao
/// (<see cref="IConsultaContratoParaEmpenho"/>) direto no mesmo escopo resolveria o
/// <c>AdministracaoDbContext</c> lado-a-lado (dois <c>ModuleDbContext</c> no mesmo escopo). Esta porta
/// isola a consulta num escopo proprio (com o tenant corrente reaplicado, preservando o banco dedicado e
/// os Global Query Filters). Implementada no ApiHost (depende de <c>IServiceScopeFactory</c> +
/// <c>TenantOverride</c>), mesmo padrao do <c>IConsultaCidadaoEmEscopoDedicado</c>.
/// </para>
/// </summary>
public interface IConsultaContratoEmEscopoDedicado
{
    /// <summary>
    /// Apura o status de empenho de um contrato no tenant corrente, em escopo dedicado.
    /// </summary>
    /// <param name="contratoId">Identificador do contrato vinculado ao empenho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Veredito de aptidao do contrato para empenho.</returns>
    Task<StatusContratoParaEmpenho> ConsultarStatusAsync(Guid contratoId, CancellationToken cancellationToken);
}
