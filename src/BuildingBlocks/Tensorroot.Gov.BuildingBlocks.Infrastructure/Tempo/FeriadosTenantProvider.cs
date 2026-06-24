using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.BuildingBlocks.Infrastructure.Tempo;

/// <summary>
/// Fonte de feriados POR TENANT em 3 camadas: (i) nacionais FIXOS de lei (constantes nomeadas, nao
/// magicas), (ii) nacionais/religiosos MOVEIS via <see cref="FeriadosMoveis"/> (Computus, sem hardcode
/// de ano) e (iii) municipais + pontos facultativos adotados, parametrizados na configuracao do tenant
/// (secao <see cref="OpcoesFeriadosTenant.SecaoConfiguracao"/>), resolvendo o tenant via
/// <see cref="ITenantContext"/> (mesma mecanica de <c>RegraAfastamentoProvider</c>). Resultado puro por
/// (tenant, ano) → cacheado em <see cref="IMemoryCache"/>. Evolucao natural: trocar a config pela tabela
/// <c>core.CalendarioFeriado</c> sem alterar o contrato nem o dominio.
/// </summary>
public sealed class FeriadosTenantProvider(
    IConfiguration configuration,
    ITenantContext tenant,
    IMemoryCache cache)
    : IFeriadosTenantProvider
{
    /// <summary>
    /// Feriados nacionais de data FIXA (mes, dia) — Lei 662/1949, Lei 6.802/1980 e
    /// Lei 14.759/2023 (Consciencia Negra, feriado nacional desde 2024).
    /// </summary>
    private static readonly (int Mes, int Dia)[] FeriadosNacionaisFixos =
    [
        (1, 1),   // Confraternizacao Universal
        (4, 21),  // Tiradentes
        (5, 1),   // Dia do Trabalho
        (9, 7),   // Independencia
        (10, 12), // Nossa Senhora Aparecida
        (11, 2),  // Finados
        (11, 15), // Proclamacao da Republica
        (11, 20), // Consciencia Negra (Lei 14.759/2023)
        (12, 25), // Natal
    ];

    /// <inheritdoc />
    public IReadOnlySet<DateOnly> FeriadosDoAno(int ano)
    {
        var chave = $"feriados:{tenant.TenantId}:{ano}";

        // GetOrCreate so devolve null se a factory devolver null; Calcular nunca o faz — o fallback
        // preserva o nao-nulo do contrato sem suprimir nullability (CLAUDE.md S6).
        return cache.GetOrCreate(chave, _ => Calcular(ano)) ?? Calcular(ano);
    }

    private HashSet<DateOnly> Calcular(int ano)
    {
        var opcoes = configuration
            .GetSection(OpcoesFeriadosTenant.SecaoConfiguracao)
            .Get<OpcoesFeriadosTenant>() ?? new OpcoesFeriadosTenant();

        var datas = new HashSet<DateOnly>();

        // (i) Nacionais FIXOS de lei.
        foreach (var (mes, dia) in FeriadosNacionaisFixos)
        {
            datas.Add(new DateOnly(ano, mes, dia));
        }

        // (ii) MOVEIS via Computus (Sexta-feira Santa e' feriado consolidado; os demais sao facultativos).
        datas.Add(FeriadosMoveis.SextaFeiraSanta(ano));
        if (opcoes.AdotaCarnaval)
        {
            datas.Add(FeriadosMoveis.SegundaCarnaval(ano));
            datas.Add(FeriadosMoveis.TercaCarnaval(ano));
        }

        if (opcoes.AdotaQuartaCinzas)
        {
            datas.Add(FeriadosMoveis.QuartaCinzas(ano));
        }

        if (opcoes.AdotaCorpusChristi)
        {
            datas.Add(FeriadosMoveis.CorpusChristi(ano));
        }

        // (iii) Municipais recorrentes (MM-dd) + pontuais (yyyy-MM-dd) do tenant.
        foreach (var mmdd in opcoes.MunicipaisFixos)
        {
            if (TryParseMesDia(mmdd, ano, out var data))
            {
                datas.Add(data);
            }
        }

        foreach (var iso in opcoes.MunicipaisData)
        {
            if (DateOnly.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
                && data.Year == ano)
            {
                datas.Add(data);
            }
        }

        return datas;
    }

    private static bool TryParseMesDia(string mmdd, int ano, out DateOnly data)
    {
        data = default;
        var partes = mmdd.Split('-');
        if (partes.Length != 2
            || !int.TryParse(partes[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mes)
            || !int.TryParse(partes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dia)
            || mes is < 1 or > 12)
        {
            return false;
        }

        // 29-02 so existe em ano bissexto: ignora silenciosamente nos demais.
        if (dia < 1 || dia > DateTime.DaysInMonth(ano, mes))
        {
            return false;
        }

        data = new DateOnly(ano, mes, dia);
        return true;
    }
}
