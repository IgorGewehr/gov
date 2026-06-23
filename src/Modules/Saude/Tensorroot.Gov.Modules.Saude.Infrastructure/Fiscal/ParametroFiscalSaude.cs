using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;

/// <summary>
/// Percentual/valor fiscal de Saúde <b>versionado por tenant+vigência</b> (nunca hardcoded — CLAUDE.md
/// §16). Ex.: a chave <c>asps.percentual-minimo</c> guarda o mínimo ASPS (default legal 0,15; a Lei
/// Orgânica municipal pode fixar maior). Mora na Infrastructure (é um registro de configuração, não
/// invariante de domínio).
/// </summary>
public sealed class ParametroFiscalSaude : IMustHaveTenant
{
    private ParametroFiscalSaude()
    {
    }

    private ParametroFiscalSaude(Guid id, Guid tenantId, string chave, DateOnly vigenciaInicio, decimal valor)
    {
        Id = id;
        TenantId = tenantId;
        Chave = chave;
        VigenciaInicio = vigenciaInicio;
        Valor = valor;
    }

    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (município) dono do parâmetro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave do parâmetro (ex.: <c>asps.percentual-minimo</c>).</summary>
    public string Chave { get; private set; } = default!;

    /// <summary>Início de vigência (inclusive).</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Valor vigente (ex.: 0,15 para 15%).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Chave do percentual mínimo de aplicação em ASPS.</summary>
    public const string ChavePercentualMinimoAsps = "asps.percentual-minimo";

    /// <summary>Cria um parâmetro fiscal versionado.</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="chave">Chave do parâmetro.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="valor">Valor vigente.</param>
    /// <returns>Novo <see cref="ParametroFiscalSaude"/>.</returns>
    public static ParametroFiscalSaude Criar(Guid tenantId, string chave, DateOnly vigenciaInicio, decimal valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        return new ParametroFiscalSaude(Guid.NewGuid(), tenantId, chave.Trim(), vigenciaInicio, valor);
    }
}
