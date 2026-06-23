namespace Tensorroot.Gov.Modules.Saude.Domain.Farmacia;

/// <summary>
/// Classe de controle especial de um medicamento, conforme a Portaria SVS/MS 344/1998 (que
/// disciplina substancias e medicamentos sujeitos a controle especial — base do SNGPC/ANVISA).
/// Determina a exigencia de retencao de receita e a escrituracao no livro/SNGPC.
/// </summary>
public enum TipoControleSngpc
{
    /// <summary>Medicamento comum (venda livre/comum, sem controle especial).</summary>
    SemControle = 0,

    /// <summary>Antimicrobianos (RDC 471/2021) — receita de controle especial em 2 vias, escrituracao.</summary>
    Antimicrobiano = 1,

    /// <summary>Lista C1 (outras substancias sujeitas a controle especial) — receita em 2 vias.</summary>
    ControleEspecialC1 = 2,

    /// <summary>Listas A1/A2/A3 (entorpecentes/psicotropicos) — Notificacao de Receita "A" (amarela).</summary>
    EntorpecentePsicotropicoA = 3,

    /// <summary>Listas B1/B2 (psicotropicos) — Notificacao de Receita "B" (azul).</summary>
    PsicotropicoB = 4,
}

/// <summary>Forma farmaceutica/apresentacao (subconjunto operacional).</summary>
public enum FormaFarmaceutica
{
    /// <summary>Comprimido.</summary>
    Comprimido = 1,

    /// <summary>Capsula.</summary>
    Capsula = 2,

    /// <summary>Solucao/xarope oral (volume).</summary>
    SolucaoOral = 3,

    /// <summary>Injetavel (ampola/frasco).</summary>
    Injetavel = 4,

    /// <summary>Pomada/creme (uso topico).</summary>
    PomadaCreme = 5,

    /// <summary>Outra apresentacao.</summary>
    Outra = 99,
}

/// <summary>Unidade de medida da dispensacao/estoque do medicamento.</summary>
public enum UnidadeMedidaMedicamento
{
    /// <summary>Unidade (comprimido, capsula, ampola, frasco).</summary>
    Unidade = 1,

    /// <summary>Mililitro (solucoes/injetaveis por volume).</summary>
    Mililitro = 2,

    /// <summary>Grama (pos/cremes por massa).</summary>
    Grama = 3,

    /// <summary>Caixa/embalagem.</summary>
    Caixa = 4,
}

/// <summary>Situacao do registro de dispensacao ao paciente.</summary>
public enum SituacaoDispensacao
{
    /// <summary>Dispensacao efetivada (baixa de estoque realizada).</summary>
    Efetivada = 1,

    /// <summary>Dispensacao estornada/cancelada (estoque devolvido).</summary>
    Estornada = 2,
}
