using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;

/// <summary>Natureza da linha de execução fiscal de Educação.</summary>
public enum TipoLinhaEducacao
{
    /// <summary>Receita-base do mínimo (imposto próprio ou transferência constitucional).</summary>
    ReceitaBaseImpostosTransferencias = 1,

    /// <summary>Despesa executada na função Educação (a classificar MDE/não-MDE).</summary>
    DespesaEducacao = 2,
}

/// <summary>
/// <b>E-1 (read model, Via A2).</b> Linha de execução fiscal de Educação já decomposta por funcional/fonte,
/// projetada da contabilidade (do barramento de Finanças). NÃO duplica o dado contábil — é a projeção de
/// leitura tenant-scoped que o <c>ApuradorMde</c> consome. A classificação MDE é aplicada na leitura,
/// contra as regras vigentes — nada hardcoded. Idempotente por <see cref="OrigemHash"/>. Espelha a
/// <c>LinhaExecucaoSaude</c> da Saúde.
/// <para>
/// A migração para a Via A1 (campos decompostos já carimbados no contrato de Finanças) troca apenas o
/// alimentador desta projeção — o domínio e o apurador não mudam.
/// </para>
/// </summary>
public sealed class LinhaExecucaoEducacao : IMustHaveTenant
{
    private LinhaExecucaoEducacao()
    {
    }

    private LinhaExecucaoEducacao(
        Guid id,
        Guid tenantId,
        int exercicio,
        TipoLinhaEducacao tipo,
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

    /// <summary>Natureza da linha (receita-base / despesa de Educação).</summary>
    public TipoLinhaEducacao Tipo { get; private set; }

    /// <summary>Função de governo (2 dígitos), quando despesa; <c>null</c> para receita-base.</summary>
    public string? Funcao { get; private set; }

    /// <summary>Subfunção (3 dígitos), quando disponível — chave da classificação MDE fina.</summary>
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
    public static LinhaExecucaoEducacao ReceitaBase(Guid tenantId, int exercicio, decimal valor, string origemHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origemHash);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new LinhaExecucaoEducacao(
            Guid.NewGuid(), tenantId, exercicio,
            TipoLinhaEducacao.ReceitaBaseImpostosTransferencias, null, null, null, valor, origemHash);
    }

    /// <summary>Registra uma linha de despesa de Educação (a classificar por funcional/fonte).</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="funcao">Função de governo (2 dígitos).</param>
    /// <param name="subfuncao">Subfunção (3 dígitos), opcional.</param>
    /// <param name="fonteRecurso">Fonte de recurso, opcional.</param>
    /// <param name="valor">Valor executado (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência.</param>
    /// <returns>Nova linha de despesa.</returns>
    public static LinhaExecucaoEducacao Despesa(
        Guid tenantId, int exercicio, string funcao, string? subfuncao, string? fonteRecurso, decimal valor, string origemHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origemHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(funcao);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new LinhaExecucaoEducacao(
            Guid.NewGuid(), tenantId, exercicio,
            TipoLinhaEducacao.DespesaEducacao, funcao.Trim(),
            string.IsNullOrWhiteSpace(subfuncao) ? null : subfuncao.Trim(),
            string.IsNullOrWhiteSpace(fonteRecurso) ? null : fonteRecurso.Trim(),
            valor, origemHash);
    }
}
