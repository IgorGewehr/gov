using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Tests;

/// <summary>
/// Fake de <see cref="IDataHojeTenant"/> para testes que constroem handlers diretamente (sem DI):
/// converte o instante de um <see cref="TimeProvider"/> para a data/hora no fuso informado
/// (default America/Sao_Paulo, UTC-3 — o mesmo default de producao).
/// </summary>
internal sealed class DataHojeTenantFake(TimeProvider timeProvider, string timeZoneId = "America/Sao_Paulo")
    : IDataHojeTenant
{
    private readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public DateOnly Hoje() => DateOnly.FromDateTime(Agora().DateTime);

    public DateTimeOffset Agora() => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _fuso);
}
