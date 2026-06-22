using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure;

/// <summary>
/// Unidade de trabalho COMPARTILHADA: confirma as alterações do <see cref="ModuleDbContext"/> ativo
/// no escopo da requisição (resolvido pelos repositórios do módulo via <see cref="ScopeDbContextHolder"/>).
/// <para>
/// Substitui o registro de <c>IUnitOfWork</c> por módulo — que colidia no contêiner (a última
/// registração vencia) e fazia o <c>SaveChanges</c> atingir o DbContext de OUTRO módulo (vazio),
/// descartando silenciosamente as gravações.
/// </para>
/// </summary>
public sealed class ModuleUnitOfWork(ScopeDbContextHolder holder) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var contexto = holder.Atual
            ?? throw new InvalidOperationException(
                "Nenhum DbContext de módulo foi resolvido neste escopo; a unidade de trabalho não tem o que confirmar.");
        return contexto.SaveChangesAsync(cancellationToken);
    }
}
