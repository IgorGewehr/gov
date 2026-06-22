using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

/// <summary>
/// Parametro socioassistencial versionado por vigencia (ex.: salario minimo vigente),
/// parametrizavel por tenant — nunca hardcoded (Beneficio I-1; CLAUDE.md secao 7). Persistido e
/// isolado por tenant via Global Query Filter; consultado pela vigencia inicial mais recente que
/// nao ultrapassa a competencia de referencia.
/// </summary>
public sealed class ParametroVigente : IMustHaveTenant
{
    private ParametroVigente()
    {
    }

    /// <summary>Identificador do registro de parametro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (municipio) dono do parametro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave do parametro (ex.: "SalarioMinimo").</summary>
    public string Chave { get; private set; } = default!;

    /// <summary>Inicio de vigencia (inclusive) do valor parametrizado.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Valor numerico do parametro na vigencia.</summary>
    public decimal Valor { get; private set; }
}
