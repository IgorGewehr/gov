using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.Protocolo;

/// <summary>
/// Gerador do Numero Unico de Protocolo (NUP) no padrao CONARQ/Decreto 8.539/2015
/// (<c>nnnnnn/aaaa-dd</c>): sequencial anual por tenant + ano + digito verificador (modulo 11).
/// A sequencia deriva da contagem de processos do exercicio corrente; a unicidade efetiva e
/// garantida pelo indice unico <c>(TenantId, Nup)</c> da Infrastructure.
/// </summary>
public sealed class NupSequencialGenerator(ProtocoloDbContext context, TimeProvider timeProvider) : INupGenerator
{
    /// <inheritdoc />
    public async Task<Nup> GerarAsync(CancellationToken cancellationToken)
    {
        var ano = timeProvider.GetUtcNow().UtcDateTime.Year;
        var prefixoAno = $"/{ano.ToString(CultureInfo.InvariantCulture)}-";

        // Maior sequencial ja emitido no exercicio (tenant-scoped via Global Query Filter).
        var emitidosNoAno = await context.Processos
            .Where(processo => processo.Nup.Valor.Contains(prefixoAno))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var sequencial = emitidosNoAno + 1;
        var corpo = sequencial.ToString("D6", CultureInfo.InvariantCulture);
        var digito = CalcularDigitoVerificador(corpo, ano);

        var valor = $"{corpo}/{ano.ToString(CultureInfo.InvariantCulture)}-{digito.ToString("D2", CultureInfo.InvariantCulture)}";
        return new Nup(valor);
    }

    /// <summary>
    /// Calcula o digito verificador (modulo 11) sobre o sequencial concatenado ao ano,
    /// conforme a Orientacao Tecnica do NUP (Decreto 8.539/2015).
    /// </summary>
    private static int CalcularDigitoVerificador(string sequencial, int ano)
    {
        var baseCalculo = sequencial + ano.ToString(CultureInfo.InvariantCulture);
        var soma = 0;
        var peso = 2;
        for (var indice = baseCalculo.Length - 1; indice >= 0; indice--)
        {
            soma += (baseCalculo[indice] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        var digito = 11 - resto;
        return digito >= 10 ? 0 : digito;
    }
}
