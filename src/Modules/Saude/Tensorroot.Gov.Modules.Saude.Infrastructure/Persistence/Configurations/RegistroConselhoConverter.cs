using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence.Configurations;

/// <summary>
/// Conversor de valor do VO opcional <see cref="RegistroConselho"/> para uma unica coluna compacta
/// "Tipo|Uf|Numero" (nula quando ausente). Necessario porque o EF Core 8 nao suporta complex property
/// nullable. Reconstroi o VO na leitura, preservando as invariantes do construtor.
/// </summary>
public sealed class RegistroConselhoConverter : ValueConverter<RegistroConselho?, string?>
{
    private const char Separador = '|';

    /// <summary>Cria o conversor compacto do <see cref="RegistroConselho"/>.</summary>
    public RegistroConselhoConverter()
        : base(
            registro => Serializar(registro),
            texto => Desserializar(texto))
    {
    }

    private static string? Serializar(RegistroConselho? registro)
        => registro is { } r
            ? string.Join(Separador, (int)r.Tipo, r.Uf, r.Numero)
            : null;

    private static RegistroConselho? Desserializar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var partes = texto.Split(Separador, 3);
        if (partes.Length != 3)
        {
            throw new InvalidOperationException("Registro de conselho persistido invalido.");
        }

        var tipo = (TipoConselho)int.Parse(partes[0], System.Globalization.CultureInfo.InvariantCulture);
        return new RegistroConselho(tipo, partes[1], partes[2]);
    }
}
