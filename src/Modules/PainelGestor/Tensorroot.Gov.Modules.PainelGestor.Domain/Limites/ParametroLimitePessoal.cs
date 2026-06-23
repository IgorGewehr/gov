using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Limites;

/// <summary>
/// Parâmetro VERSIONADO por tenant+vigência dos limites da Despesa com Pessoal (LRF). Cada registro vale
/// a partir de <see cref="VigenciaInicio"/>; o provedor seleciona a vigência mais recente ≤ referência do
/// exercício (reprodutível, sem relógio). Existe para que os percentuais NUNCA sejam hardcoded
/// (CLAUDE.md §7): a Lei Orgânica/o TCE-RS pode exigir limites distintos, e a apuração mudou com a
/// LC 178/2021. Na ausência de registro, o provedor usa o fallback legal de referência.
/// </summary>
public sealed class ParametroLimitePessoal : IMustHaveTenant
{
    private ParametroLimitePessoal()
    {
    }

    private ParametroLimitePessoal(
        Guid id,
        Guid tenantId,
        DateOnly vigenciaInicio,
        decimal limiteLegal,
        decimal fatorPrudencial,
        decimal fatorAlerta)
    {
        Id = id;
        TenantId = tenantId;
        VigenciaInicio = vigenciaInicio;
        LimiteLegal = limiteLegal;
        FatorPrudencial = fatorPrudencial;
        FatorAlerta = fatorAlerta;
    }

    /// <summary>Identidade do parâmetro.</summary>
    public Guid Id { get; private init; }

    /// <summary>Tenant (ente público) dono do registro. <see cref="IMustHaveTenant"/>.</summary>
    public Guid TenantId { get; private init; }

    /// <summary>Data de início de vigência do parâmetro.</summary>
    public DateOnly VigenciaInicio { get; private init; }

    /// <summary>Teto legal da despesa de pessoal sobre a RCL (fração 0..1).</summary>
    public decimal LimiteLegal { get; private set; }

    /// <summary>Fator do limite prudencial sobre o legal (ex.: 0,95).</summary>
    public decimal FatorPrudencial { get; private set; }

    /// <summary>Fator do limite de alerta sobre o legal (ex.: 0,90).</summary>
    public decimal FatorAlerta { get; private set; }

    /// <summary>Cria um parâmetro de limite de pessoal versionado.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="limiteLegal">Teto legal (0..1).</param>
    /// <param name="fatorPrudencial">Fator prudencial (0..1).</param>
    /// <param name="fatorAlerta">Fator de alerta (0..1).</param>
    /// <returns>Novo parâmetro.</returns>
    public static ParametroLimitePessoal Criar(
        Guid tenantId,
        DateOnly vigenciaInicio,
        decimal limiteLegal,
        decimal fatorPrudencial,
        decimal fatorAlerta)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId é obrigatório.", nameof(tenantId));
        }

        ValidarFracao(limiteLegal, nameof(limiteLegal));
        ValidarFracao(fatorPrudencial, nameof(fatorPrudencial));
        ValidarFracao(fatorAlerta, nameof(fatorAlerta));

        return new ParametroLimitePessoal(Guid.NewGuid(), tenantId, vigenciaInicio, limiteLegal, fatorPrudencial, fatorAlerta);
    }

    /// <summary>Projeta o parâmetro como objeto de valor de limites do domínio.</summary>
    /// <returns>Os limites correspondentes.</returns>
    public LimitesPessoalLrf ParaLimites() => new(LimiteLegal, FatorPrudencial, FatorAlerta);

    private static void ValidarFracao(decimal valor, string nome)
    {
        if (valor is <= 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nome, "Fração deve estar em (0, 1].");
        }
    }
}
