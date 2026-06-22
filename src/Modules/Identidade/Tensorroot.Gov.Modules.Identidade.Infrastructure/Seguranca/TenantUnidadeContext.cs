using Microsoft.Extensions.DependencyInjection;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Identidade.Infrastructure.Seguranca;

/// <summary>
/// Resolve, POR REQUISICAO, o escopo de Unidade Organizacional (UO) do sujeito atual — alimenta o
/// filtro de leitura por UO do <c>ModuleDbContext</c> (MODELO §5/§10.3). A resolucao e PREGUICOSA e
/// cacheada no escopo: so ocorre na primeira leitura filtrada por UO, e obtem a porta de Aplicacao
/// (<see cref="IResolvedorEscopoUnidade"/>) pelo <see cref="IServiceProvider"/> (evita ciclo de
/// construcao com o proprio <c>IdentidadeDbContext</c>, do qual a porta depende transitivamente).
/// </summary>
/// <remarks>
/// Igual a logica do <c>TenantOverride</c>: SEM sujeito resolvido (Workers, provisionamento,
/// drenagem de Outbox), <see cref="DeveFiltrarPorUnidade"/> e <c>false</c> e o filtro NAO e aplicado.
/// COM sujeito, filtra pela UNIAO das UOs do escopo efetivo (qualquer permissao) — para LEITURA, o
/// sujeito enxerga toda UO em que detem qualquer papel; o command-side faz a checagem fina por
/// permissao+UO (guards). ADIADO p/ M1.x: cache cross-request + invalidacao na revogacao (I10).
/// </remarks>
public sealed class TenantUnidadeContext(IServiceProvider serviceProvider, ICurrentUser currentUser) : ITenantUnidadeContext
{
    private bool _resolvido;
    private bool _deveFiltrar;
    private IReadOnlyCollection<Guid> _unidades = [];

    /// <inheritdoc />
    public bool DeveFiltrarPorUnidade
    {
        get
        {
            Resolver();
            return _deveFiltrar;
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> UnidadesPermitidas
    {
        get
        {
            Resolver();
            return _unidades;
        }
    }

    private void Resolver()
    {
        if (_resolvido)
        {
            return;
        }

        _resolvido = true;

        // Sem sujeito identificavel (job/sistema) → nao filtra (igual ao override de tenant).
        if (!Guid.TryParse(currentUser.UserId, out var usuarioGuid))
        {
            _deveFiltrar = false;
            return;
        }

        var resolvedor = serviceProvider.GetRequiredService<IResolvedorEscopoUnidade>();
        var clock = serviceProvider.GetRequiredService<TimeProvider>();

        // Resolucao sincrona controlada no escopo da requisicao. Sujeito autenticado mas sem UOs
        // (inexistente/sem atribuicao) → filtra com conjunto vazio (deny-by-default, I2).
        _unidades = resolvedor
            .ResolverUnidadesLegiveisAsync(usuarioGuid, clock.GetUtcNow(), CancellationToken.None)
            .GetAwaiter().GetResult();
        _deveFiltrar = true;
    }
}
