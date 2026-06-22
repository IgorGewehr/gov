using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Lookups;

/// <summary>
/// Projecao persistida (tenant-scoped) das Unidades de Atendimento (CRAS/CREAS/Centro POP) do
/// tenant, usada pela Infraestrutura para satisfazer as consultas de leitura
/// <c>IUnidadeAtendimentoRepository</c> e <c>IUnidadeAtendimentoTipoLookup</c> da Application
/// (validacao de cobertura territorial — I-2 — e compatibilidade servico↔unidade — I-4).
/// O agregado completo <c>UnidadeAtendimento</c> sera gerado em iteracao futura deste mesmo
/// modulo; ate la este read model mantem o isolamento por tenant via Global Query Filter.
/// </summary>
public sealed class UnidadeAtendimentoLookup : IMustHaveTenant
{
    private UnidadeAtendimentoLookup()
    {
    }

    /// <summary>Identificador da unidade de atendimento.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Tipo da unidade (CRAS/CREAS/Centro POP) — define os servicos admissiveis (I-4).</summary>
    public TipoUnidadeAtendimento Tipo { get; private set; }

    /// <summary>Territorio coberto pela unidade (chave de pertencimento da familia — I-2).</summary>
    public string TerritorioCobertura { get; private set; } = default!;
}
