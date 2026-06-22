using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Receitas;

namespace Tensorroot.Gov.Modules.Financas.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de receitas arrecadadas.</summary>
public sealed class ReceitaArrecadadaRepository(FinancasDbContext context) : IReceitaArrecadadaRepository
{
    /// <inheritdoc />
    public void Adicionar(ReceitaArrecadada receita)
    {
        ArgumentNullException.ThrowIfNull(receita);
        context.ReceitasArrecadadas.Add(receita);
    }
}
