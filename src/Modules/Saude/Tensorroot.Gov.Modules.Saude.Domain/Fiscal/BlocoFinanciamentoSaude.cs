namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>
/// Bloco de financiamento federal da Saúde (Portaria GM/MS nº 3.992/2017, EC pós-2017): a Portaria
/// reorganizou os antigos 6 blocos em <b>dois</b>, e o recurso fundo a fundo chega <b>carimbado por
/// bloco</b>, com execução segregada e vedada a transposição livre custeio↔investimento.
/// <para>
/// // TODO(validar-oficial): a taxonomia exata dos blocos/componentes (Port. Consolidação GM/MS nº
/// 6/2017 consolidada + 3.992/2017 + componentes APS da Port. 3.493/2024) deve ser confirmada no manual
/// vigente; os 2 blocos abaixo são o eixo confirmado pós-2017.
/// </para>
/// </summary>
public enum BlocoFinanciamentoSaude
{
    /// <summary>Custeio — Bloco de Manutenção das Ações e Serviços Públicos de Saúde (Port. 3.992/2017).</summary>
    Custeio = 1,

    /// <summary>Investimento — Bloco de Estruturação da Rede de Serviços Públicos de Saúde (Port. 3.992/2017).</summary>
    Investimento = 2,
}
