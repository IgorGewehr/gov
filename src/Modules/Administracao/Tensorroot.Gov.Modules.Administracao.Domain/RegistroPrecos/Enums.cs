namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Situacao da Ata de Registro de Precos (ARP) no seu ciclo de vida (art. 82-86, Lei 14.133/2021).</summary>
public enum SituacaoAta
{
    /// <summary>Ata vigente: itens disponiveis para contratacao e adesao.</summary>
    Vigente = 1,

    /// <summary>Ata encerrada por decurso do prazo de vigencia.</summary>
    Encerrada = 2,

    /// <summary>Ata cancelada por ato administrativo (descumprimento, interesse publico, etc.).</summary>
    Cancelada = 3,
}
