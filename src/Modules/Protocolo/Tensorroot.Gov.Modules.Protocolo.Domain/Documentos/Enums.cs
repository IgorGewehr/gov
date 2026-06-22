namespace Tensorroot.Gov.Modules.Protocolo.Domain.Documentos;

/// <summary>
/// Nivel exigido de assinatura por criticidade do ato (Lei 14.063/2020 + Decreto 10.543/2020).
/// Ordem numerica crescente representa criticidade crescente (usada na comparacao "maior ou igual").
/// </summary>
public enum TipoAssinatura
{
    /// <summary>Identificacao por meio eletronico (gov.br bronze).</summary>
    AssinaturaSimples = 1,

    /// <summary>Provedor credenciado com vinculo do signatario (gov.br prata/ouro; Lei 14.063/2020).</summary>
    AssinaturaAvancada = 2,

    /// <summary>ICP-Brasil (certificado e-CPF/e-CNPJ) — MP 2.200-2/2001.</summary>
    AssinaturaQualificada = 3,
}

/// <summary>
/// Grau de criticidade do ato, que determina o nivel minimo de assinatura exigido
/// (Decreto 10.543/2020).
/// </summary>
public enum CriticidadeAto
{
    /// <summary>Requerimento do cidadao — exige assinatura simples.</summary>
    Baixa = 1,

    /// <summary>Ato interno de media criticidade — exige assinatura avancada.</summary>
    Media = 2,

    /// <summary>Ato do dirigente maximo / bens imoveis — exige assinatura qualificada (ICP-Brasil).</summary>
    Alta = 3,
}

/// <summary>Nivel de acesso (visibilidade) do documento.</summary>
public enum NivelDeAcesso
{
    /// <summary>Acesso publico.</summary>
    Publico = 1,

    /// <summary>Acesso restrito (papel adicional).</summary>
    Restrito = 2,

    /// <summary>Acesso sigiloso (papel de sigilo; tentativa nao autorizada e auditada).</summary>
    Sigiloso = 3,
}

/// <summary>Situacao (estado) do documento no ciclo de vida.</summary>
public enum SituacaoDocumento
{
    /// <summary>Criado, ainda nao juntado (mutavel).</summary>
    Rascunho = 1,

    /// <summary>Juntado ao processo (imutavel).</summary>
    Juntado = 2,

    /// <summary>Juntado e assinado conforme criticidade.</summary>
    Assinado = 3,

    /// <summary>Tornado sem efeito (terminal); permanece na trilha documental.</summary>
    SemEfeito = 4,
}
