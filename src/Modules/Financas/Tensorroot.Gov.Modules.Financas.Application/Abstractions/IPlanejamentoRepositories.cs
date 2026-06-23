using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

namespace Tensorroot.Gov.Modules.Financas.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="PlanoPlurianual"/> (PPA).</summary>
public interface IPpaRepository
{
    /// <summary>Marca um novo PPA para inserção.</summary>
    void Adicionar(PlanoPlurianual ppa);

    /// <summary>Obtém um PPA por identificador (com a árvore Programa/Ação/Meta).</summary>
    Task<PlanoPlurianual?> ObterPorIdAsync(PpaId id, CancellationToken cancellationToken);

    /// <summary>Obtém o PPA vigente que cobre o exercício informado, se houver.</summary>
    Task<PlanoPlurianual?> ObterVigentePorExercicioAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Lista os PPAs do tenant.</summary>
    Task<IReadOnlyList<PlanoPlurianual>> ListarAsync(CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="LeiDiretrizes"/> (LDO).</summary>
public interface ILdoRepository
{
    /// <summary>Marca uma nova LDO para inserção.</summary>
    void Adicionar(LeiDiretrizes ldo);

    /// <summary>Obtém uma LDO por identificador (com prioridades/metas/anexos).</summary>
    Task<LeiDiretrizes?> ObterPorIdAsync(LdoId id, CancellationToken cancellationToken);

    /// <summary>Obtém a LDO vigente do exercício, se houver.</summary>
    Task<LeiDiretrizes?> ObterVigentePorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="LeiOrcamentariaAnual"/> (LOA).</summary>
public interface ILoaRepository
{
    /// <summary>Marca uma nova LOA para inserção.</summary>
    void Adicionar(LeiOrcamentariaAnual loa);

    /// <summary>Obtém uma LOA por identificador (com receitas/itens do QDD).</summary>
    Task<LeiOrcamentariaAnual?> ObterPorIdAsync(LoaId id, CancellationToken cancellationToken);

    /// <summary>Obtém a LOA do exercício, se houver.</summary>
    Task<LeiOrcamentariaAnual?> ObterPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="CreditoAdicional"/>.</summary>
public interface ICreditoAdicionalRepository
{
    /// <summary>Marca um novo crédito adicional para inserção.</summary>
    void Adicionar(CreditoAdicional credito);

    /// <summary>Obtém um crédito adicional por identificador.</summary>
    Task<CreditoAdicional?> ObterPorIdAsync(CreditoAdicionalId id, CancellationToken cancellationToken);

    /// <summary>
    /// Soma os valores de créditos SUPLEMENTARES por decreto já abertos para a LOA
    /// (base para o limite de suplementação do art. 7º / CF 167, V).
    /// </summary>
    Task<decimal> SomarSuplementacoesPorDecretoAsync(LoaId loaId, CancellationToken cancellationToken);
}
