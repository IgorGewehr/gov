using Tensorroot.Gov.Modules.Financas.Domain.Receitas;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do read model <see cref="ReceitaArrecadada"/>.</summary>
public interface IReceitaArrecadadaRepository
{
    /// <summary>Marca uma nova receita arrecadada para inserção.</summary>
    /// <param name="receita">Receita a adicionar.</param>
    void Adicionar(ReceitaArrecadada receita);
}
