namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

/// <summary>
/// Contexto fatico do requerente usado na avaliacao de elegibilidade (Beneficio I-2/I-3/I-4).
/// Projetado a partir do read model do CadUnico/Familia (intra-modulo). Nao trafega dado
/// sensivel identificavel no barramento — somente subsidia a decisao do dominio.
/// </summary>
/// <param name="Idade">Idade do requerente em anos (criterio BPC idoso, I-2).</param>
/// <param name="PossuiDeficiencia">Indica deficiencia (PCD) declarada.</param>
/// <param name="PossuiAvaliacaoBiopsicossocial">Avaliacao biopsicossocial concluida (requisito BPC/PCD, I-2).</param>
/// <param name="AcumulaSeguridadeSocial">Indica beneficio concomitante da Seguridade (veda BPC, I-3).</param>
/// <param name="InscritoCadUnico">Indica inscricao no CadUnico (preferencia em eventual, I-4).</param>
/// <param name="RendaFamiliar">Renda familiar mensal total.</param>
/// <param name="MembrosFamilia">Numero de membros da familia (&gt;= 1).</param>
public readonly record struct DadosElegibilidade(
    int Idade,
    bool PossuiDeficiencia,
    bool PossuiAvaliacaoBiopsicossocial,
    bool AcumulaSeguridadeSocial,
    bool InscritoCadUnico,
    decimal RendaFamiliar,
    int MembrosFamilia);
