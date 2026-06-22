namespace Tensorroot.Gov.BuildingBlocks.Infrastructure;

/// <summary>
/// Mantém, POR ESCOPO de requisição, o <see cref="ModuleDbContext"/> ativo — o contexto do módulo
/// que está atendendo à requisição atual. É populado pelo próprio contexto ao ser construído (via DI/EF).
/// <para>
/// Permite que a unidade de trabalho compartilhada (<see cref="ModuleUnitOfWork"/>) confirme o
/// contexto CORRETO, eliminando a ambiguidade de múltiplos módulos registrarem <c>IUnitOfWork</c>
/// no mesmo contêiner (onde a última registração vencia e o SaveChanges atingia o contexto errado).
/// </para>
/// </summary>
public sealed class ScopeDbContextHolder
{
    /// <summary>Contexto de módulo ativo no escopo atual (nulo até o primeiro ser resolvido).</summary>
    public ModuleDbContext? Atual { get; private set; }

    /// <summary>
    /// Define o contexto ativo do escopo (chamado pelo próprio contexto ao ser construído).
    /// <para>
    /// FALHA-ALTO (achado H5): se um SEGUNDO <see cref="ModuleDbContext"/> DIVERGENTE for definido no
    /// mesmo escopo, LANÇA em vez de aplicar last-writer-wins — que descartaria silenciosamente as
    /// mutações do primeiro contexto ao confirmar o segundo. Definir o MESMO contexto de novo é no-op
    /// idempotente (o EF pode reconstruir/reinjetar a mesma instância). O fluxo legítimo de drenagem
    /// (<c>OutboxBackgroundService</c>) já usa um escopo POR MÓDULO, então nunca aciona esta guarda.
    /// </para>
    /// </summary>
    /// <param name="contexto">Contexto do módulo resolvido neste escopo.</param>
    /// <exception cref="InvalidOperationException">
    /// Quando um segundo contexto de módulo DISTINTO é definido no mesmo escopo.
    /// </exception>
    public void Definir(ModuleDbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        if (Atual is not null && !ReferenceEquals(Atual, contexto))
        {
            throw new InvalidOperationException(
                $"Dois ModuleDbContext distintos foram resolvidos no mesmo escopo ('{Atual.GetType().Name}' e " +
                $"'{contexto.GetType().Name}'). A unidade de trabalho compartilhada confirma um único contexto " +
                "por escopo; mutações de contextos múltiplos seriam descartadas silenciosamente (H5). " +
                "Use um escopo dedicado por módulo (como o OutboxBackgroundService já faz).");
        }

        Atual = contexto;
    }
}
