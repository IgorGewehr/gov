namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Lrf;

/// <summary>
/// Opções de planejamento sob a LRF, parametrizáveis por tenant (CLAUDE.md: nada hardcoded).
/// Controlam a obrigatoriedade dos anexos AMF/ARF da LDO conforme porte do ente e a posição do TCE.
/// </summary>
public sealed class OpcoesPlanejamentoLrf
{
    /// <summary>Seção de configuração.</summary>
    public const string SecaoConfig = "Financas:PlanejamentoLrf";

    /// <summary>
    /// Exige o Anexo de Metas Fiscais (AMF — LRF art. 4º §1º) para a LDO vigorar.
    /// Default conservador: obrigatório.
    /// </summary>
    // TODO(validar-oficial): posicao vigente do TCE-RS para municipio &lt; 50 mil hab. (porte do piloto).
    public bool ExigirAnexoMetasFiscais { get; set; } = true;

    /// <summary>
    /// Exige o Anexo de Riscos Fiscais (ARF — LRF art. 4º §3º) para a LDO vigorar.
    /// Default conservador: obrigatório.
    /// </summary>
    // TODO(validar-oficial): posicao vigente do TCE-RS para municipio &lt; 50 mil hab. (porte do piloto).
    public bool ExigirAnexoRiscosFiscais { get; set; } = true;
}
