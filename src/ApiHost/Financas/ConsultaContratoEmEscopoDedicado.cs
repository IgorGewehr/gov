using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Infrastructure.Multitenancy;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;

namespace Tensorroot.Gov.ApiHost.Financas;

/// <summary>
/// Implementacao de <see cref="IConsultaContratoEmEscopoDedicado"/>: consulta o status PNCP de um contrato
/// (Administracao, via <see cref="IConsultaContratoParaEmpenho"/>) num ESCOPO DE DI proprio, evitando o
/// gatilho da guarda H5 (<c>ScopeDbContextHolder</c>) — o <c>EmpenharHandler</c> ja resolveu o
/// <c>FinancasDbContext</c> no escopo da requisicao, entao a porta do Administracao resolveria um SEGUNDO
/// <c>ModuleDbContext</c> (<c>AdministracaoDbContext</c>) lado-a-lado.
/// <para>
/// Abre um escopo novo, reaplica o <see cref="TenantOverride"/> com o tenant corrente (preserva o banco
/// dedicado e os Global Query Filters) e resolve a porta do Administracao ali. Mesmo padrao do
/// <c>ConsultaCidadaoEmEscopoDedicado</c>/<c>AssinaturaEmEscopoDedicado</c>. Vive no ApiHost porque depende
/// de <see cref="IServiceScopeFactory"/> + <see cref="TenantOverride"/> (camada superior ao BuildingBlocks).
/// </para>
/// </summary>
internal sealed class ConsultaContratoEmEscopoDedicado(
    IServiceScopeFactory scopeFactory,
    ITenantContext tenantContext) : IConsultaContratoEmEscopoDedicado
{
    /// <inheritdoc />
    public async Task<StatusContratoParaEmpenho> ConsultarStatusAsync(Guid contratoId, CancellationToken cancellationToken)
    {
        // Captura o tenant ANTES de abrir o novo escopo (o ITenantContext do novo escopo nao tem HTTP).
        var tenantId = tenantContext.TenantId;

        await using var escopo = scopeFactory.CreateAsyncScope();
        escopo.ServiceProvider.GetRequiredService<TenantOverride>().TenantId = tenantId;

        var consulta = escopo.ServiceProvider.GetRequiredService<IConsultaContratoParaEmpenho>();
        return await consulta.ConsultarAsync(contratoId, cancellationToken).ConfigureAwait(false);
    }
}
