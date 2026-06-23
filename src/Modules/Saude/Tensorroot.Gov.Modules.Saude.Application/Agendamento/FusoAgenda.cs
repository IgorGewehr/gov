namespace Tensorroot.Gov.Modules.Saude.Application.Agendamento;

/// <summary>
/// Fuso horario aplicado na expansao de slots da agenda. Default: horario de Brasilia (UTC-3). Centralizado
/// (sem numero magico espalhado); a parametrizacao por tenant fica para um refino futuro (CLAUDE.md S7/S16).
/// </summary>
internal static class FusoAgenda
{
    /// <summary>Offset default das vagas (horario de Brasilia, UTC-3).</summary>
    public static readonly TimeSpan OffsetPadrao = TimeSpan.FromHours(-3);
}
