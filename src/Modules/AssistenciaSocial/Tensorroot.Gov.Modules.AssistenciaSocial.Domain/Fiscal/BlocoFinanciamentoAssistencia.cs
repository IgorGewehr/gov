namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Fiscal;

/// <summary>
/// <b>A-1 — Bloco de cofinanciamento federal do SUAS</b> (Portaria MDS nº 1.043/2024, que revogou a
/// Port. 113/2015 e reorganizou as transferencias fundo a fundo FNAS → FMAS). O recurso chega
/// <b>carimbado por bloco</b>, com execucao segregada (espelha os 2 blocos da Saude, mas aqui sao 4).
/// <para>
/// // TODO(validar-oficial): os codigos/leiaute exatos das parcelas FNAS por bloco/piso dependem do
/// extrato do SUASWeb/AgilizaSUAS e da tabela de pisos da NOB-SUAS — a estrutura de 4 blocos e o eixo
/// confirmado da Port. 1.043/2024 (pesquisa-assistencia §4).
/// </para>
/// </summary>
public enum BlocoFinanciamentoAssistencia
{
    /// <summary>Protecao Social Basica (PSB) — PAIF/CRAS, SCFV (Port. 1.043/2024).</summary>
    ProtecaoSocialBasica = 1,

    /// <summary>Protecao Social Especial de Media Complexidade (PSE-MC) — PAEFI/CREAS, abordagem, populacao de rua.</summary>
    ProtecaoSocialEspecialMediaComplexidade = 2,

    /// <summary>Protecao Social Especial de Alta Complexidade (PSE-AC) — acolhimento institucional/familiar.</summary>
    ProtecaoSocialEspecialAltaComplexidade = 3,

    /// <summary>Gestao do SUAS / Indice de Gestao Descentralizada (IGD-SUAS e IGD-PBF/CadUnico).</summary>
    GestaoSuasIgd = 4,
}

/// <summary>
/// <b>A-1 — Piso de cofinanciamento</b> dentro de um <see cref="BlocoFinanciamentoAssistencia"/>
/// (Tipificacao Nacional, Res. CNAS 109/2009; NOB-SUAS/2012). Segrega o repasse por servico tipificado.
/// // TODO(validar-oficial): valores de referencia por piso = tabela de pisos/NOB-SUAS vigente.
/// </summary>
public enum PisoAssistencia
{
    /// <summary>Piso Basico Fixo (PSB) — financia o PAIF nos CRAS.</summary>
    BasicoFixo = 1,

    /// <summary>Piso Basico Variavel (PSB) — SCFV e demais servicos da basica.</summary>
    BasicoVariavel = 2,

    /// <summary>Piso Fixo de Media Complexidade (PSE-MC) — PAEFI/CREAS, abordagem social, populacao de rua.</summary>
    FixoMediaComplexidade = 3,

    /// <summary>Pisos de Acolhimento (PSE-AC) — acolhimento institucional/familiar (alta complexidade).</summary>
    Acolhimento = 4,

    /// <summary>Componente de Gestao (IGD-SUAS / IGD-PBF-CadUnico) — incentivo de gestao.</summary>
    Gestao = 5,
}
