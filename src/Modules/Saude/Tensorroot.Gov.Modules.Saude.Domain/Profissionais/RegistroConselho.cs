namespace Tensorroot.Gov.Modules.Saude.Domain.Profissionais;

/// <summary>
/// Registro do profissional em conselho de classe (owned type): tipo (CRM/COREN/CRO/...), UF e numero.
/// Habilita exigencias clinicas como a teleconsulta para portadores de CRM ativo (Lei 14.510/2022 — I-9).
/// </summary>
public readonly record struct RegistroConselho
{
    /// <summary>Comprimento maximo do numero do registro.</summary>
    public const int ComprimentoNumero = 20;

    /// <summary>Quantidade exata de caracteres de uma UF.</summary>
    public const int ComprimentoUf = 2;

    /// <summary>Cria um registro de conselho.</summary>
    /// <param name="tipo">Tipo do conselho (CRM, COREN, ...).</param>
    /// <param name="uf">UF do registro (2 caracteres).</param>
    /// <param name="numero">Numero do registro (obrigatorio).</param>
    /// <exception cref="ArgumentException">Se o tipo for invalido, a UF nao tiver 2 caracteres ou o numero for vazio/exceder.</exception>
    public RegistroConselho(TipoConselho tipo, string uf, string numero)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException($"Tipo de conselho invalido: {tipo}.", nameof(tipo));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(uf);
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);

        var ufNormalizada = uf.Trim().ToUpperInvariant();
        if (ufNormalizada.Length != ComprimentoUf)
        {
            throw new ArgumentException("UF deve ter 2 caracteres.", nameof(uf));
        }

        var numeroNormalizado = numero.Trim();
        if (numeroNormalizado.Length > ComprimentoNumero)
        {
            throw new ArgumentException($"Numero do registro excede {ComprimentoNumero} caracteres.", nameof(numero));
        }

        Tipo = tipo;
        Uf = ufNormalizada;
        Numero = numeroNormalizado;
    }

    /// <summary>Tipo do conselho.</summary>
    public TipoConselho Tipo { get; }

    /// <summary>UF do registro (2 caracteres).</summary>
    public string Uf { get; }

    /// <summary>Numero do registro.</summary>
    public string Numero { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Tipo}/{Uf} {Numero}";
}
