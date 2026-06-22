namespace Tensorroot.Gov.BuildingBlocks.Application.Assinatura;

/// <summary>
/// Destino/finalidade de uma assinatura digital. Determina os PARAMETROS criptograficos exigidos
/// pelo orgao receptor (A1-DESIGN §4) e e registrado na trilha de auditoria de uso (A1-DESIGN §6).
/// </summary>
public enum DestinoAssinatura
{
    /// <summary>eSocial — XML-DSig Enveloped, RSA-SHA256, C14N INCLUSIVA (Manual Desenvolvedor v1.15 §6.7 — CONFIRMADO).</summary>
    ESocial = 1,

    /// <summary>Remessa TCE-RS (SIAPC/PAD). CMS so se exigido — // TODO(validar-oficial: Manual PAD/Processo Eletronico TCE-RS).</summary>
    TceRs = 2,

    /// <summary>SICONFI/MSC. // TODO(validar-oficial: Manual SICONFI Acesso e Certificacao Digital — A1 vs e-CPF A3).</summary>
    Siconfi = 3,

    /// <summary>Documento no Protocolo (Lei 14.063/2020).</summary>
    Protocolo = 4,

    /// <summary>
    /// Ponto eletronico — AFD/AEJ (Portaria MTP 671/2021): CAdES DETACHED (.p7s) sobre o arquivo
    /// posicional. // TODO(validar-oficial): perfil CAdES exato (algoritmo/atributos assinados/cadeia
    /// ICP-Brasil) conforme o Anexo da 671 e a P-e-R REP gov.br; comprovante do trabalhador = PAdES.
    /// </summary>
    Ponto = 5,
}
