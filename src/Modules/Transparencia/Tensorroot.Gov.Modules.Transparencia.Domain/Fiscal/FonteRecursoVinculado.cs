using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="FonteRecursoVinculado"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FonteRecursoVinculadoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FonteRecursoVinculadoId"/>.</returns>
    public static FonteRecursoVinculadoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// <b>M7.0.0 (Via A2 — classificador setorial próprio).</b> Mapa, por tenant+vigência, que carimba
/// a despesa por <b>função</b> e/ou <b>fonte de recurso (PCASP)</b> com o <see cref="SetorMinimo"/>
/// (Saúde/Educação) e marca se ela <b>computa</b> no mínimo (LC 141/2012 art. 3º computa / art. 4º
/// não computa para Saúde; análogo MDE para Educação).
/// <para>
/// <b>Não duplica o dado contábil</b>: Finanças (PCASP/MSC/empenho) continua a fonte da verdade; este
/// agregado apenas <b>deriva</b> a classificação setorial que falta no ponto de consumo. A regra
/// primária é a <b>função</b> (10 Saúde, 12 Educação); a <b>fonte</b> refina (ex.: distinguir custeio
/// vinculado de despesa não computável dentro da mesma função). Tudo versionado por
/// <see cref="VigenciaInicio"/> — nada hardcoded (CLAUDE.md §7/§16).
/// </para>
/// // TODO(validar-oficial): classificação computável fina (LC 141 arts. 3º/4º — saneamento, inativos,
/// merenda, limpeza urbana NÃO computam em Saúde) depende do Manual SIOPS vigente; o seed default cobre
/// o caso geral por função.
/// </summary>
public sealed class FonteRecursoVinculado : AggregateRoot<FonteRecursoVinculadoId>, IMustHaveTenant
{
    private FonteRecursoVinculado()
    {
    }

    private FonteRecursoVinculado(
        FonteRecursoVinculadoId id,
        Guid tenantId,
        string funcao,
        string? fonteRecurso,
        SetorMinimo setor,
        bool computaNoMinimo,
        DateOnly vigenciaInicio)
        : base(id)
    {
        TenantId = tenantId;
        Funcao = funcao;
        FonteRecurso = fonteRecurso;
        Setor = setor;
        ComputaNoMinimo = computaNoMinimo;
        VigenciaInicio = vigenciaInicio;
    }

    /// <summary>Tenant (município) dono da regra de classificação.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Código da função de governo (2 dígitos, ex.: "10" Saúde, "12" Educação).</summary>
    public string Funcao { get; private set; } = default!;

    /// <summary>
    /// Fonte/destinação de recurso (PCASP), quando a regra refina por fonte; <c>null</c> = vale para
    /// toda a função independentemente da fonte.
    /// </summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Setor de mínimo ao qual a despesa pertence.</summary>
    public SetorMinimo Setor { get; private set; }

    /// <summary>Se a despesa assim classificada computa no mínimo do setor.</summary>
    public bool ComputaNoMinimo { get; private set; }

    /// <summary>Início de vigência (inclusive) da regra.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Cria uma regra de classificação setorial versionada.</summary>
    /// <param name="tenantId">Tenant dono da regra.</param>
    /// <param name="funcao">Código da função (2 dígitos).</param>
    /// <param name="setor">Setor de mínimo.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="fonteRecurso">Fonte de recurso (opcional; refina a regra).</param>
    /// <param name="computaNoMinimo">Se computa no mínimo (default <c>true</c>).</param>
    /// <returns>Nova <see cref="FonteRecursoVinculado"/>.</returns>
    public static FonteRecursoVinculado Criar(
        Guid tenantId,
        string funcao,
        SetorMinimo setor,
        DateOnly vigenciaInicio,
        string? fonteRecurso = null,
        bool computaNoMinimo = true)
    {
        var codigo = CodigoFuncional.De(funcao);
        if (setor == SetorMinimo.Nenhum)
        {
            throw new ArgumentException("Regra de classificação deve apontar um setor de mínimo.", nameof(setor));
        }

        return new FonteRecursoVinculado(
            FonteRecursoVinculadoId.New(),
            tenantId,
            codigo.Funcao,
            string.IsNullOrWhiteSpace(fonteRecurso) ? null : fonteRecurso.Trim(),
            setor,
            computaNoMinimo,
            vigenciaInicio);
    }

    /// <summary>
    /// Indica se esta regra casa com a função/fonte informadas. Casa quando a função é igual e a fonte
    /// da regra é <c>null</c> (vale para toda a função) ou igual à fonte informada (regra específica).
    /// </summary>
    /// <param name="codigo">Funcional resolvida da despesa.</param>
    /// <param name="fonteRecurso">Fonte de recurso da despesa (opcional).</param>
    /// <returns><c>true</c> se a regra se aplica.</returns>
    public bool Casa(CodigoFuncional codigo, string? fonteRecurso)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        if (!string.Equals(codigo.Funcao, Funcao, StringComparison.Ordinal))
        {
            return false;
        }

        return FonteRecurso is null
            || string.Equals(FonteRecurso, fonteRecurso?.Trim(), StringComparison.Ordinal);
    }

    /// <summary>Especificidade da regra: 1 quando refina por fonte, 0 quando vale para toda a função.</summary>
    /// <returns>Grau de especificidade (maior vence o desempate na classificação).</returns>
    public int Especificidade() => FonteRecurso is null ? 0 : 1;
}
