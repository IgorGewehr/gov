using System.Text.Json;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Infrastructure.Persistence.Configurations;

/// <summary>
/// Conversores JSON deterministicos para os VOs ricos do dominio Convenios persistidos em coluna unica
/// (TEXT/nvarchar(max)). Mantem o mapeamento simples e portavel entre SQLite (dev) e SqlServer (prod), sem
/// owned-collections aninhadas. VOs reidratados pelas factories <c>Reidratar</c> (sem reler o calendario:
/// o vencimento persistido e a fonte de verdade do ato praticado).
/// </summary>
internal static class Serializadores
{
    private static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.General);

    // ===== PrazoLegal (SharedKernel) =====

    private sealed record PrazoLegalDto(DateOnly Inicio, int Quantidade, UnidadePrazo Unidade, DateOnly Vencimento, string NormaFonte);

    /// <summary>Serializa um <see cref="PrazoLegal"/> (nullable) para JSON.</summary>
    public static string? SerializarPrazo(PrazoLegal? prazo)
        => prazo is null ? null : JsonSerializer.Serialize(
            new PrazoLegalDto(prazo.Inicio, prazo.Quantidade, prazo.Unidade, prazo.Vencimento, prazo.NormaFonte), Opcoes);

    /// <summary>Reidrata um <see cref="PrazoLegal"/> (nullable) do JSON (sem reler calendario).</summary>
    public static PrazoLegal? DesserializarPrazo(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<PrazoLegalDto>(valor, Opcoes)!;
        return PrazoLegal.Reidratar(dto.Inicio, dto.Quantidade, dto.Unidade, dto.Vencimento, dto.NormaFonte);
    }

    // ===== Vigencia =====

    private sealed record VigenciaDto(DateOnly Inicio, DateOnly FimOriginal, List<ProrrogacaoVigencia> Prorrogacoes);

    /// <summary>Serializa uma <see cref="Vigencia"/> (nullable) para JSON.</summary>
    public static string? SerializarVigencia(Vigencia? vigencia)
        => vigencia is null ? null : JsonSerializer.Serialize(
            new VigenciaDto(vigencia.Inicio, vigencia.FimOriginal, [.. vigencia.Prorrogacoes]), Opcoes);

    /// <summary>Reidrata uma <see cref="Vigencia"/> (nullable) do JSON.</summary>
    public static Vigencia? DesserializarVigencia(string? valor)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<VigenciaDto>(valor, Opcoes)!;
        return Vigencia.Reidratar(dto.Inicio, dto.FimOriginal, dto.Prorrogacoes);
    }
}
