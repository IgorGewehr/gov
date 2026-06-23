namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Inventarios;

/// <summary>Tipo (abrangência/finalidade) do inventário patrimonial (Lei 4.320 art. 96).</summary>
public enum TipoInventario
{
    /// <summary>Levantamento anual obrigatório de todo o acervo.</summary>
    Anual = 1,

    /// <summary>Levantamento restrito a um setor/unidade organizacional.</summary>
    PorSetor = 2,

    /// <summary>Levantamento eventual (extraordinário).</summary>
    Eventual = 3,

    /// <summary>Levantamento para transferência de responsabilidade (troca de gestor/responsável).</summary>
    Transferencia = 4,
}

/// <summary>Situação (estado) do inventário na máquina de estados do levantamento.</summary>
public enum SituacaoInventario
{
    /// <summary>Aberto, comissão designada, ainda sem snapshot do acervo.</summary>
    EmAbertura = 1,

    /// <summary>Snapshot contábil congelado; coleta física (contagem) em andamento.</summary>
    EmContagem = 2,

    /// <summary>Conciliação físico × contábil realizada; divergências apuradas.</summary>
    EmConciliacao = 3,

    /// <summary>Encerrado (terminal) — alimenta as recomendações de movimentação/baixa.</summary>
    Encerrado = 4,

    /// <summary>Cancelado (terminal) sem efeito patrimonial.</summary>
    Cancelado = 5,
}

/// <summary>Situação física encontrada de um item durante a contagem.</summary>
public enum SituacaoEncontrada
{
    /// <summary>Item localizado no setor esperado e em condições.</summary>
    Localizado = 1,

    /// <summary>Item não localizado (candidato a falta).</summary>
    NaoLocalizado = 2,

    /// <summary>Item localizado, porém em setor/localização diferente da esperada.</summary>
    LocalizadoOutroSetor = 3,

    /// <summary>Item localizado, porém inservível (candidato a baixa/reavaliação).</summary>
    Inservivel = 4,
}

/// <summary>Tipo de divergência apurada na conciliação físico × contábil.</summary>
public enum TipoDivergencia
{
    /// <summary>Bem no snapshot contábil e não contado fisicamente.</summary>
    Falta = 1,

    /// <summary>Bem físico encontrado sem tombo/registro contábil (achado).</summary>
    Sobra = 2,

    /// <summary>Bem contado em localização diferente da esperada.</summary>
    DivergenciaLocalizacao = 3,

    /// <summary>Bem com estado de conservação divergente (ex.: inservível).</summary>
    DivergenciaEstado = 4,

    /// <summary>Bem com valor contábil divergente do apurado.</summary>
    DivergenciaValor = 5,
}

/// <summary>Recomendação de efetivação downstream gerada por uma divergência.</summary>
public enum RecomendacaoDivergencia
{
    /// <summary>Recomenda baixa do bem (falta confirmada/inservível).</summary>
    Baixa = 1,

    /// <summary>Recomenda transferência/movimentação (localização divergente).</summary>
    Transferencia = 2,

    /// <summary>Recomenda incorporação de novo bem (sobra/achado).</summary>
    Incorporacao = 3,

    /// <summary>Recomenda reavaliação ou reconhecimento de impairment (valor/estado divergente).</summary>
    Reavaliacao = 4,
}
