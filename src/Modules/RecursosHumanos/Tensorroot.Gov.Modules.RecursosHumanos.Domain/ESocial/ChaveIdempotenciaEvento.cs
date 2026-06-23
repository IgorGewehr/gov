using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

/// <summary>
/// Chave de negocio que torna a GERACAO de um evento eSocial idempotente (ESOCIAL-SPEC §4.3): o mesmo
/// evento de negocio (ex.: um unico S-1200 por (servidor, competencia)) nunca e gerado/transmitido em
/// duplicidade. Composta por <c>(tipoEvento, idNegocio, competencia)</c> — o <c>tenantId</c> ja isola
/// por banco/Global Query Filter. <c>competencia</c> e nula para eventos de tabela/nao-periodicos sem
/// competencia (S-1000/S-1005/S-1010/S-2200/S-2299).
/// </summary>
public sealed class ChaveIdempotenciaEvento : ValueObject
{
    private ChaveIdempotenciaEvento(TipoEventoESocial tipoEvento, string idNegocio, string? competencia)
    {
        TipoEvento = tipoEvento;
        IdNegocio = idNegocio;
        Competencia = competencia;
    }

    /// <summary>Tipo do evento (S-1000, S-1200, ...).</summary>
    public TipoEventoESocial TipoEvento { get; }

    /// <summary>
    /// Identificador de negocio da origem (ex.: id do Servidor, codigo da rubrica, CNPJ do ente,
    /// id da folha). Estabiliza a unicidade por dominio, nao por instante.
    /// </summary>
    public string IdNegocio { get; }

    /// <summary>Competencia (<c>AAAA-MM</c>) quando aplicavel; nula para eventos sem competencia.</summary>
    public string? Competencia { get; }

    /// <summary>Cria a chave de idempotencia.</summary>
    /// <param name="tipoEvento">Tipo do evento.</param>
    /// <param name="idNegocio">Identificador de negocio da origem (nao vazio).</param>
    /// <param name="competencia">Competencia <c>AAAA-MM</c> ou nula.</param>
    /// <returns>Instancia de <see cref="ChaveIdempotenciaEvento"/>.</returns>
    /// <exception cref="ArgumentException">Se <paramref name="idNegocio"/> for vazio.</exception>
    public static ChaveIdempotenciaEvento Criar(TipoEventoESocial tipoEvento, string idNegocio, string? competencia = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idNegocio);
        return new ChaveIdempotenciaEvento(tipoEvento, idNegocio.Trim(), string.IsNullOrWhiteSpace(competencia) ? null : competencia.Trim());
    }

    /// <inheritdoc />
    public override string ToString()
        => Competencia is null ? $"{(int)TipoEvento}:{IdNegocio}" : $"{(int)TipoEvento}:{IdNegocio}:{Competencia}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TipoEvento;
        yield return IdNegocio;
        yield return Competencia;
    }
}
