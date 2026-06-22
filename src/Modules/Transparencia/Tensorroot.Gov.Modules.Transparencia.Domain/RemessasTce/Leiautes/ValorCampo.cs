using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Valor BRUTO de um campo a serializar, pareado com o <see cref="CampoLeiaute"/> que o define. O
/// <see cref="EmissorRegistroSiapc"/> formata/pad de acordo com o tipo. Objeto de valor.
/// </summary>
public sealed class ValorCampo : ValueObject
{
    private ValorCampo(string nomeCampo, string? texto, long? numero, decimal? valorMonetario, DateOnly? data)
    {
        NomeCampo = nomeCampo;
        Texto = texto;
        Numero = numero;
        ValorMonetario = valorMonetario;
        Data = data;
    }

    /// <summary>Nome lógico do campo a que este valor se refere (casa com <see cref="CampoLeiaute.Nome"/>).</summary>
    public string NomeCampo { get; }

    /// <summary>Valor textual (quando campo Caractere).</summary>
    public string? Texto { get; }

    /// <summary>Valor inteiro (quando campo Numérico).</summary>
    public long? Numero { get; }

    /// <summary>Valor monetário em Reais (quando campo Valor) — será convertido a centavos.</summary>
    public decimal? ValorMonetario { get; }

    /// <summary>Valor de data (quando campo Data) — formatado ddmmaaaa.</summary>
    public DateOnly? Data { get; }

    /// <summary>Indica se o valor está "vazio" para fins de pré-validação de obrigatoriedade.</summary>
    public bool EstaVazio
        => Numero is null
            && ValorMonetario is null
            && Data is null
            && string.IsNullOrEmpty(Texto);

    /// <summary>Cria um valor textual (campo Caractere).</summary>
    /// <param name="nomeCampo">Nome do campo (não vazio).</param>
    /// <param name="texto">Texto (pode ser nulo/vazio se opcional).</param>
    /// <returns>Instância de <see cref="ValorCampo"/>.</returns>
    public static ValorCampo Caractere(string nomeCampo, string? texto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCampo);
        return new ValorCampo(nomeCampo, texto, null, null, null);
    }

    /// <summary>Cria um valor numérico inteiro (campo Numérico).</summary>
    /// <param name="nomeCampo">Nome do campo (não vazio).</param>
    /// <param name="numero">Inteiro não-negativo.</param>
    /// <returns>Instância de <see cref="ValorCampo"/>.</returns>
    public static ValorCampo Numerico(string nomeCampo, long numero)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCampo);
        ArgumentOutOfRangeException.ThrowIfNegative(numero);
        return new ValorCampo(nomeCampo, null, numero, null, null);
    }

    /// <summary>Cria um valor monetário (campo Valor; convertido a centavos com sinal).</summary>
    /// <param name="nomeCampo">Nome do campo (não vazio).</param>
    /// <param name="valor">Montante em Reais (pode ser negativo — sinal preservado).</param>
    /// <returns>Instância de <see cref="ValorCampo"/>.</returns>
    public static ValorCampo Monetario(string nomeCampo, decimal valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCampo);
        return new ValorCampo(nomeCampo, null, null, valor, null);
    }

    /// <summary>Cria um valor de data (campo Data; formatado ddmmaaaa).</summary>
    /// <param name="nomeCampo">Nome do campo (não vazio).</param>
    /// <param name="data">Data.</param>
    /// <returns>Instância de <see cref="ValorCampo"/>.</returns>
    public static ValorCampo DataCampo(string nomeCampo, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeCampo);
        return new ValorCampo(nomeCampo, null, null, null, data);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NomeCampo;
        yield return Texto;
        yield return Numero;
        yield return ValorMonetario;
        yield return Data;
    }
}
