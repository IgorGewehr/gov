namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;

/// <summary>Situação (máquina de estados) da Lei de Diretrizes Orçamentárias.</summary>
public enum SituacaoLdo
{
    /// <summary>Em elaboração — editável.</summary>
    Elaboracao = 1,

    /// <summary>Em tramitação no Legislativo — congelada.</summary>
    EmTramitacao = 2,

    /// <summary>Vigente (lei sancionada) — habilita a LOA do exercício.</summary>
    Vigente = 3,

    /// <summary>Encerrada.</summary>
    Encerrada = 4,
}

/// <summary>Tipo de anexo obrigatório da LDO pela LRF (LC 101/2000 art. 4º §§ 1º-3º).</summary>
public enum TipoAnexoLdo
{
    /// <summary>Anexo de Metas Fiscais (AMF) — LRF art. 4º §1º/§2º.</summary>
    AnexoMetasFiscais = 1,

    /// <summary>Anexo de Riscos Fiscais (ARF) — LRF art. 4º §3º.</summary>
    AnexoRiscosFiscais = 2,
}
