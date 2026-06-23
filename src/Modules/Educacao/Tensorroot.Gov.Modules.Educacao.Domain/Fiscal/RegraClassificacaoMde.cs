using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="RegraClassificacaoMde"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegraClassificacaoMdeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegraClassificacaoMdeId"/>.</returns>
    public static RegraClassificacaoMdeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Como a despesa de Educação (função 12) é tratada na apuração da MDE (LDB Lei 9.394/1996).
/// </summary>
public enum EfeitoMde
{
    /// <summary>Inclui: a despesa computa na MDE (LDB art. 70).</summary>
    Inclui = 1,

    /// <summary>Exclui: a despesa NÃO computa na MDE (LDB art. 71), embora seja função 12.</summary>
    Exclui = 2,
}

/// <summary>
/// <b>E-1 — Regra de classificação MDE, versionada por tenant+vigência.</b> Refina, para o módulo
/// Educação, o classificador genérico <c>FonteRecursoVinculado</c> do M7.0: em vez de só dizer "função 12
/// ⇒ computa", carrega a <b>lista de inclusões/exclusões finas</b> da LDB — o que o <b>art. 70</b> manda
/// computar como Manutenção e Desenvolvimento do Ensino e, sobretudo, o que o <b>art. 71</b> manda
/// <b>NÃO</b> computar mesmo estando na função 12:
/// <list type="bullet">
///   <item><description>pesquisa não vinculada ao ensino; subvenção a instituições privadas;</description></item>
///   <item><description>programas suplementares de alimentação (merenda), assistência médico-odontológica,
///   farmacêutica e psicológica (subfunções 306/301...);</description></item>
///   <item><description>obras de infraestrutura urbana fora das escolas; formação de quadros especiais
///   alheios ao ensino; pessoal inativo (aposentadorias/pensões da educação).</description></item>
/// </list>
/// A regra casa por <b>(função, subfunção?, fonte de recurso?)</b> e a mais <b>específica</b> vence; o
/// efeito (Inclui/Exclui) decide se entra no numerador dos 25%. Tudo <b>parametrizável</b> — nada de
/// classificação hardcoded (CLAUDE.md §7/§16); a lista é seed default e o tenant versiona por vigência.
/// <para>
/// // TODO(validar-oficial): a tabela fina de inclusões/exclusões MDE (LDB arts. 70/71) e o mapeamento por
/// subfunção/fonte dependem do <b>Manual SIOPE</b> vigente e do mapeamento contas→campos SIOPE — o seed
/// cobre o caso geral; a fidelidade fina (e a aferição oficial) exige o manual oficial.
/// </para>
/// </summary>
public sealed class RegraClassificacaoMde : AggregateRoot<RegraClassificacaoMdeId>, IMustHaveTenant
{
    private RegraClassificacaoMde()
    {
    }

    private RegraClassificacaoMde(
        RegraClassificacaoMdeId id,
        Guid tenantId,
        string funcao,
        string? subfuncao,
        string? fonteRecurso,
        EfeitoMde efeito,
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

    /// <summary>Código da função de governo (2 dígitos; "12" = Educação).</summary>
    public string Funcao { get; private set; } = default!;

    /// <summary>Subfunção (3 dígitos) quando a regra refina por subfunção; <c>null</c> = toda a função.</summary>
    public string? Subfuncao { get; private set; }

    /// <summary>Fonte/destinação de recurso (PCASP) quando a regra refina por fonte; <c>null</c> = qualquer fonte.</summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Efeito na apuração MDE (Inclui art. 70 / Exclui art. 71).</summary>
    public EfeitoMde Efeito { get; private set; }

    /// <summary>Descrição legível da regra (ex.: "Merenda escolar — não MDE, art. 71, IV").</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Início de vigência (inclusive) da regra.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Cria uma regra de classificação MDE versionada.</summary>
    /// <param name="tenantId">Tenant dono da regra.</param>
    /// <param name="efeito">Efeito (Inclui/Exclui).</param>
    /// <param name="descricao">Descrição legível.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="funcao">Função (2 dígitos; default "12").</param>
    /// <param name="subfuncao">Subfunção (3 dígitos), opcional — refina a regra.</param>
    /// <param name="fonteRecurso">Fonte de recurso, opcional — refina a regra.</param>
    /// <returns>Nova <see cref="RegraClassificacaoMde"/>.</returns>
    public static RegraClassificacaoMde Criar(
        Guid tenantId,
        EfeitoMde efeito,
        string descricao,
        DateOnly vigenciaInicio,
        string funcao = CodigoFuncionalEducacao.FuncaoEducacao,
        string? subfuncao = null,
        string? fonteRecurso = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        var codigo = CodigoFuncionalEducacao.De(funcao, subfuncao);

        return new RegraClassificacaoMde(
            RegraClassificacaoMdeId.New(),
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
    public bool Casa(CodigoFuncionalEducacao codigo, string? fonteRecurso)
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
    /// Uma exclusão fina (ex.: subfunção 306 merenda) supera uma inclusão genérica (toda a função 12).
    /// </summary>
    /// <returns>Grau de especificidade.</returns>
    public int Especificidade() => (Subfuncao is null ? 0 : 2) + (FonteRecurso is null ? 0 : 1);
}
