namespace Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

/// <summary>Situacao (estado) do atendimento no ciclo clinico.</summary>
public enum SituacaoAtendimento
{
    /// <summary>Registro aberto, ainda editavel (estado inicial).</summary>
    EmAndamento = 1,

    /// <summary>Assinado em ICP-Brasil — imutavel (somente adendo).</summary>
    Assinado = 2,

    /// <summary>RES enviado e aceito na RNDS.</summary>
    Compartilhado = 3,

    /// <summary>Cancelado antes da assinatura — terminal.</summary>
    Cancelado = 4,
}

/// <summary>Nivel de garantia de seguranca do prontuario (CFM 1.821/2007).</summary>
public enum NivelGarantia
{
    /// <summary>Nivel de garantia 1 (nao elimina papel).</summary>
    NGS1 = 1,

    /// <summary>Nivel de garantia 2 (ICP-Brasil) — habilita eliminacao do papel.</summary>
    NGS2 = 2,
}

/// <summary>Modalidade do atendimento (Lei 14.510/2022).</summary>
public enum ModalidadeAtendimento
{
    /// <summary>Atendimento presencial.</summary>
    Presencial = 1,

    /// <summary>Atendimento remoto regulado (teleconsulta).</summary>
    Teleconsulta = 2,
}
