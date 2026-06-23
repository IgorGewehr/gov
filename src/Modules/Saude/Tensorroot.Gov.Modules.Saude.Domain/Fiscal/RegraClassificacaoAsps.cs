using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="RegraClassificacaoAsps"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegraClassificacaoAspsId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegraClassificacaoAspsId"/>.</returns>
    public static RegraClassificacaoAspsId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Como a despesa de Saúde (função 10) é tratada na apuração das ASPS (LC 141/2012).
/// </summary>
public enum EfeitoAsps
{
    /// <summary>Inclui: a despesa computa nas ASPS (LC 141/2012 art. 3º).</summary>
    Inclui = 1,

    /// <summary>Exclui: a despesa NÃO computa nas ASPS (LC 141/2012 art. 4º), embora seja função 10.</summary>
    Exclui = 2,
}

/// <summary>
/// <b>S-1 — Regra de classificação ASPS, versionada por tenant+vigência.</b> Refina, para o módulo
/// Saúde, o classificador genérico <c>FonteRecursoVinculado</c> do M7.0 (que mora em Transparencia): em
/// vez de só dizer "função 10 ⇒ computa", carrega a <b>lista de inclusões/exclusões finas</b> da LC
/// 141/2012 — o que o art. 3º manda computar e, sobretudo, o que o art. 4º manda <b>NÃO</b> computar
/// mesmo estando na função 10:
/// <list type="bullet">
///   <item><description>inativos/pensionistas da saúde (aposentadorias e pensões);</description></item>
///   <item><description>assistência à saúde do servidor (clientela fechada);</description></item>
///   <item><description>saneamento básico de caráter geral (subfunção 512/511 fora de ASPS);</description></item>
///   <item><description>limpeza urbana e remoção de resíduos; merenda escolar; ações financiadas por outra fonte.</description></item>
/// </list>
/// A regra casa por <b>(função, subfunção?, fonte de recurso?)</b> e a mais <b>específica</b> vence; o
/// efeito (Inclui/Exclui) decide se entra no numerador dos 15%. Tudo <b>parametrizável</b> — nada de
/// classificação hardcoded (CLAUDE.md §7/§16); a lista é seed default e o tenant versiona por vigência.
/// <para>
/// // TODO(validar-oficial): a tabela fina de inclusões/exclusões ASPS (LC 141 arts. 3º/4º) e o
/// mapeamento por subfunção/fonte dependem do <b>Manual SIOPS</b> vigente e da redação atual no portal
/// SIOPS — o seed cobre o caso geral; a fidelidade fina exige o manual oficial.
/// </para>
/// </summary>
public sealed class RegraClassificacaoAsps : AggregateRoot<RegraClassificacaoAspsId>, IMustHaveTenant
{
    private RegraClassificacaoAsps()
    {
    }

    private RegraClassificacaoAsps(
        RegraClassificacaoAspsId id,
        Guid tenantId,
        string funcao,
        string? subfuncao,
        string? fonteRecurso,
        EfeitoAsps efeito,
        string descricao,
        DateOnly vigenciaInicio)
        : base(id)
    {
        TenantId = tenantId;
        Funcao = funcao;
        Subfuncao = subfuncao;
        FonteRecurso = fonteRecurso;
        Efeito = efeito;
        Descricao = descricao;
        VigenciaInicio = vigenciaInicio;
    }

    /// <summary>Tenant (município) dono da regra.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Código da função de governo (2 dígitos; "10" = Saúde).</summary>
    public string Funcao { get; private set; } = default!;

    /// <summary>Subfunção (3 dígitos) quando a regra refina por subfunção; <c>null</c> = toda a função.</summary>
    public string? Subfuncao { get; private set; }

    /// <summary>Fonte/destinação de recurso (PCASP) quando a regra refina por fonte; <c>null</c> = qualquer fonte.</summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Efeito na apuração ASPS (Inclui art. 3º / Exclui art. 4º).</summary>
    public EfeitoAsps Efeito { get; private set; }

    /// <summary>Descrição legível da regra (ex.: "Inativos da saúde — art. 4º, I").</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Início de vigência (inclusive) da regra.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Cria uma regra de classificação ASPS versionada.</summary>
    /// <param name="tenantId">Tenant dono da regra.</param>
    /// <param name="efeito">Efeito (Inclui/Exclui).</param>
    /// <param name="descricao">Descrição legível.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="funcao">Função (2 dígitos; default "10").</param>
    /// <param name="subfuncao">Subfunção (3 dígitos), opcional — refina a regra.</param>
    /// <param name="fonteRecurso">Fonte de recurso, opcional — refina a regra.</param>
    /// <returns>Nova <see cref="RegraClassificacaoAsps"/>.</returns>
    public static RegraClassificacaoAsps Criar(
        Guid tenantId,
        EfeitoAsps efeito,
        string descricao,
        DateOnly vigenciaInicio,
        string funcao = CodigoFuncionalSaude.FuncaoSaude,
        string? subfuncao = null,
        string? fonteRecurso = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        var codigo = CodigoFuncionalSaude.De(funcao, subfuncao);

        return new RegraClassificacaoAsps(
            RegraClassificacaoAspsId.New(),
            tenantId,
            codigo.Funcao,
            codigo.Subfuncao,
            string.IsNullOrWhiteSpace(fonteRecurso) ? null : fonteRecurso.Trim(),
            efeito,
            descricao.Trim(),
            vigenciaInicio);
    }

    /// <summary>
    /// Indica se esta regra casa com a funcional/fonte informadas. Casa quando a função é igual, a
    /// subfunção da regra é <c>null</c> (vale para toda a função) ou igual, e a fonte da regra é
    /// <c>null</c> (qualquer fonte) ou igual à informada.
    /// </summary>
    /// <param name="codigo">Funcional resolvida da despesa (função/subfunção).</param>
    /// <param name="fonteRecurso">Fonte de recurso da despesa (opcional).</param>
    /// <returns><c>true</c> se a regra se aplica.</returns>
    public bool Casa(CodigoFuncionalSaude codigo, string? fonteRecurso)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        if (!string.Equals(codigo.Funcao, Funcao, StringComparison.Ordinal))
        {
            return false;
        }

        if (Subfuncao is not null && !string.Equals(Subfuncao, codigo.Subfuncao, StringComparison.Ordinal))
        {
            return false;
        }

        return FonteRecurso is null
            || string.Equals(FonteRecurso, fonteRecurso?.Trim(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Especificidade da regra (maior vence o desempate): +2 quando refina por subfunção, +1 por fonte.
    /// Uma exclusão fina (ex.: subfunção 512 saneamento) supera uma inclusão genérica (toda a função 10).
    /// </summary>
    /// <returns>Grau de especificidade.</returns>
    public int Especificidade() => (Subfuncao is null ? 0 : 2) + (FonteRecurso is null ? 0 : 1);
}
