namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>
/// Motivo LEGAL da baixa de um bem do acervo (MCASP; Lei 4.320/1964; Lei 14.133/2021 art. 76). Tipar
/// como enum (em vez de <c>int</c>) faz o <c>IsInEnum()</c> do validador realmente barrar valores
/// inválidos e grava o motivo LEGÍVEL na trilha de auditoria (em vez de um inteiro cru como "999").
/// [revisao-humana-juridica] — confirmar a taxonomia oficial de motivos do ente.
/// </summary>
public enum MotivoBaixa
{
    /// <summary>Alienação (venda/leilão/doação/permuta) — Lei 14.133/2021 art. 76.</summary>
    Alienacao = 1,

    /// <summary>Inservibilidade (obsolescência, deterioração, antieconômico).</summary>
    Inservibilidade = 2,

    /// <summary>Perda apurada (furto, roubo, sinistro, extravio).</summary>
    Perda = 3,

    /// <summary>Cessão/transferência definitiva a outro ente ou órgão.</summary>
    Cessao = 4,
}
