using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;

/// <summary>Natureza da linha de execução fiscal de Saúde.</summary>
public enum TipoLinhaSaude
{
    /// <summary>Receita-base do mínimo (imposto próprio ou transferência constitucional).</summary>
    ReceitaBaseImpostosTransferencias = 1,

    /// <summary>Despesa executada na função Saúde (a classificar ASPS/não-ASPS).</summary>
    DespesaSaude = 2,
}

/// <summary>
/// <b>S-1 (read model, Via A2).</b> Linha de execução fiscal de Saúde já decomposta por funcional/fonte,
/// projetada da contabilidade (do barramento de Finanças). NÃO duplica o dado contábil — é a projeção de
/// leitura tenant-scoped que o <c>ApuradorAsps</c> consome. A classificação ASPS é aplicada na leitura,
/// contra as regras vigentes — nada hardcoded. Idempotente por <see cref="OrigemHash"/>.
/// <para>
/// A migração para a Via A1 (campos decompostos já carimbados no contrato de Finanças) troca apenas o
/// alimentador desta projeção — o domínio e o apurador não mudam.
/// </para>
/// </summary>
public sealed class LinhaExecucaoSaude : IMustHaveTenant
{
    private LinhaExecucaoSaude()
    {
    }

    private LinhaExecucaoSaude(
        Guid id,
        Guid tenantId,
        int exercicio,
        TipoLinhaSaude tipo,
        string? funcao,
        string? subfuncao,
        string? fonteRecurso,
        decimal valor,
        string origemHash)
    {
        Id = id;
        TenantId = tenantId;
        Exercicio = exercicio;
        Tipo = tipo;
        Funcao = funcao;
        Subfuncao = subfuncao;
        FonteRecurso = fonteRecurso;
        Valor = valor;
        OrigemHash = origemHash;
    }

    /// <summary>Identificador da linha.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (município) dono da linha.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício de execução.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Natureza da linha (receita-base / despesa de Saúde).</summary>
    public TipoLinhaSaude Tipo { get; private set; }

    /// <summary>Função de governo (2 dígitos), quando despesa; <c>null</c> para receita-base.</summary>
    public string? Funcao { get; private set; }

    /// <summary>Subfunção (3 dígitos), quando disponível — chave da classificação ASPS fina.</summary>
    public string? Subfuncao { get; private set; }

    /// <summary>Fonte/destinação de recurso (PCASP), quando disponível.</summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Valor executado da linha (&gt;= 0).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Hash determinístico da origem (idempotência): mesma origem não duplica a projeção.</summary>
    public string OrigemHash { get; private set; } = default!;

    /// <summary>Registra uma linha de receita-base (impostos + transferências constitucionais).</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="valor">Valor da receita (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência.</param>
    /// <returns>Nova linha de receita-base.</returns>
    public static LinhaExecucaoSaude ReceitaBase(Guid tenantId, int exercicio, decimal valor, string origemHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origemHash);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new LinhaExecucaoSaude(
            Guid.NewGuid(), tenantId, exercicio,
            TipoLinhaSaude.ReceitaBaseImpostosTransferencias, null, null, null, valor, origemHash);
    }

    /// <summary>Registra uma linha de despesa de Saúde (a classificar por funcional/fonte).</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="funcao">Função de governo (2 dígitos).</param>
    /// <param name="subfuncao">Subfunção (3 dígitos), opcional.</param>
    /// <param name="fonteRecurso">Fonte de recurso, opcional.</param>
    /// <param name="valor">Valor executado (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência.</param>
    /// <returns>Nova linha de despesa.</returns>
    public static LinhaExecucaoSaude Despesa(
        Guid tenantId, int exercicio, string funcao, string? subfuncao, string? fonteRecurso, decimal valor, string origemHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origemHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(funcao);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new LinhaExecucaoSaude(
            Guid.NewGuid(), tenantId, exercicio,
            TipoLinhaSaude.DespesaSaude, funcao.Trim(),
            string.IsNullOrWhiteSpace(subfuncao) ? null : subfuncao.Trim(),
            string.IsNullOrWhiteSpace(fonteRecurso) ? null : fonteRecurso.Trim(),
            valor, origemHash);
    }
}
