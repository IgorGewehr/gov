using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;

/// <summary>
/// Parâmetro fiscal versionado por vigência, parametrizável por tenant — nunca hardcoded
/// (CLAUDE.md §7/§16). Mesmo padrão chave+vigência do <c>ParametroVigente</c> da Assistência. Usado
/// para os percentuais mínimos (ex.: chave "MinimoSaudeAsps" = 0,15; "MinimoEducacaoMde" = 0,25),
/// consultados pela vigência mais recente que não ultrapassa a referência do exercício.
/// </summary>
public sealed class ParametroFiscalVigente : IMustHaveTenant
{
    private ParametroFiscalVigente()
    {
    }

    /// <summary>Identificador do registro de parâmetro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (município) dono do parâmetro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave do parâmetro (ex.: "MinimoSaudeAsps").</summary>
    public string Chave { get; private set; } = default!;

    /// <summary>Início de vigência (inclusive) do valor parametrizado.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>Valor numérico do parâmetro na vigência (ex.: 0,15).</summary>
    public decimal Valor { get; private set; }

    /// <summary>Cria um parâmetro fiscal versionado.</summary>
    /// <param name="tenantId">Tenant dono do parâmetro.</param>
    /// <param name="chave">Chave do parâmetro.</param>
    /// <param name="vigenciaInicio">Início de vigência.</param>
    /// <param name="valor">Valor (ex.: 0,15).</param>
    /// <returns>Novo <see cref="ParametroFiscalVigente"/>.</returns>
    public static ParametroFiscalVigente Criar(Guid tenantId, string chave, DateOnly vigenciaInicio, decimal valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        ArgumentOutOfRangeException.ThrowIfNegative(valor);

        return new ParametroFiscalVigente
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Chave = chave.Trim(),
            VigenciaInicio = vigenciaInicio,
            Valor = valor,
        };
    }
}
