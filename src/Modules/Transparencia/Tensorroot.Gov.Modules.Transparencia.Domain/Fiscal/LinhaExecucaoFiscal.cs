using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="LinhaExecucaoFiscal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct LinhaExecucaoFiscalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="LinhaExecucaoFiscalId"/>.</returns>
    public static LinhaExecucaoFiscalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Natureza da linha de execução fiscal apurada.</summary>
public enum TipoLinhaExecucao
{
    /// <summary>
    /// Receita-base do mínimo: imposto próprio ou transferência constitucional (compõe os 15%/25%).
    /// </summary>
    ReceitaBaseImpostosTransferencias = 1,

    /// <summary>Despesa setorial executada (a classificar por função/fonte em setor de mínimo).</summary>
    DespesaSetorial = 2,
}

/// <summary>
/// <b>M7.0.0 (read model).</b> Linha de execução fiscal já <b>decomposta</b> por função/fonte,
/// derivada da contabilidade (Via A2: da <c>MSCGeradaIntegrationEvent</c>, que carrega FR/FS por conta;
/// Via A1 amanhã: dos campos decompostos de <c>DespesaEmpenhadaIntegrationEvent</c>). NÃO duplica o dado
/// contábil — é a <b>projeção de leitura</b> tenant-scoped que o <c>ApuradorMinimo</c> consome. A
/// classificação setorial (computa em Saúde/Educação) é aplicada na leitura, contra
/// <see cref="FonteRecursoVinculado"/> vigente — nada hardcoded.
/// </summary>
public sealed class LinhaExecucaoFiscal : Entity<LinhaExecucaoFiscalId>, IMustHaveTenant
{
    private LinhaExecucaoFiscal()
    {
    }

    private LinhaExecucaoFiscal(
        LinhaExecucaoFiscalId id,
        Guid tenantId,
        int exercicio,
        TipoLinhaExecucao tipo,
        string? funcao,
        string? fonteRecurso,
        decimal valor,
        string origemHash)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        Tipo = tipo;
        Funcao = funcao;
        FonteRecurso = fonteRecurso;
        Valor = valor;
        OrigemHash = origemHash;
    }

    /// <summary>Tenant (município) dono da linha.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício de execução.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Natureza da linha (receita-base / despesa setorial).</summary>
    public TipoLinhaExecucao Tipo { get; private set; }

    /// <summary>Função de governo (2 dígitos), quando despesa; <c>null</c> para receita-base.</summary>
    public string? Funcao { get; private set; }

    /// <summary>Fonte/destinação de recurso (PCASP), quando disponível.</summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Valor executado da linha (&gt;= 0).</summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Hash determinístico da origem (idempotência): mesmas origem+linha não duplicam a projeção.
    /// </summary>
    public string OrigemHash { get; private set; } = default!;

    /// <summary>Registra uma linha de receita-base (impostos + transferências constitucionais).</summary>
    /// <param name="tenantId">Tenant dono da linha.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="valor">Valor da receita (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência da origem.</param>
    /// <returns>Nova <see cref="LinhaExecucaoFiscal"/> de receita-base.</returns>
    public static LinhaExecucaoFiscal ReceitaBase(Guid tenantId, int exercicio, decimal valor, string origemHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origemHash);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        return new LinhaExecucaoFiscal(
            LinhaExecucaoFiscalId.New(), tenantId, exercicio,
            TipoLinhaExecucao.ReceitaBaseImpostosTransferencias, null, null, valor, origemHash);
    }

    /// <summary>Registra uma linha de despesa setorial (a classificar por função/fonte).</summary>
    /// <param name="tenantId">Tenant dono da linha.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="funcao">Função de governo (2 dígitos).</param>
    /// <param name="fonteRecurso">Fonte de recurso (opcional).</param>
    /// <param name="valor">Valor executado (&gt;= 0).</param>
    /// <param name="origemHash">Hash de idempotência da origem.</param>
    /// <returns>Nova <see cref="LinhaExecucaoFiscal"/> de despesa.</returns>
    public static LinhaExecucaoFiscal Despesa(
        Guid tenantId,
        int exercicio,
        string funcao,
        string? fonteRecurso,
        decimal valor,
        string origemHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origemHash);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        var codigo = CodigoFuncional.De(funcao);
        return new LinhaExecucaoFiscal(
            LinhaExecucaoFiscalId.New(), tenantId, exercicio,
            TipoLinhaExecucao.DespesaSetorial, codigo.Funcao,
            string.IsNullOrWhiteSpace(fonteRecurso) ? null : fonteRecurso.Trim(), valor, origemHash);
    }
}
