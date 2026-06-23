using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>
/// Função/subfunção da despesa (Portaria MOG 42/1999). A <b>função</b> são os 2 primeiros dígitos
/// (ex.: "10" = Saúde, "12" = Educação); a subfunção, os 3 seguintes. Aceita tanto a função pura
/// ("10") quanto a funcional completa concatenada (ex.: "10.301.0002.2010"), extraindo a função.
/// <para>
/// Esta é a peça que <b>resolve a função no ponto de consumo</b> do M7: o eixo fiscal precisa saber
/// a função de cada despesa para apurar os mínimos. A função vem decomposta do contrato de Finanças
/// (Via A1, campo opcional <c>FuncaoSubfuncao</c>) ou é extraída da funcional-programática textual
/// (Via A2). // TODO(validar-oficial): dígitos exatos por função na MOG 42/1999.
/// </para>
/// </summary>
public sealed class CodigoFuncional : ValueObject
{
    private CodigoFuncional(string funcao, string? subfuncao)
    {
        Funcao = funcao;
        Subfuncao = subfuncao;
    }

    /// <summary>Código da função (2 dígitos, ex.: "10" Saúde, "12" Educação).</summary>
    public string Funcao { get; }

    /// <summary>Código da subfunção (3 dígitos), quando disponível.</summary>
    public string? Subfuncao { get; }

    /// <summary>
    /// Cria a partir de uma representação textual da funcional (função pura, "FS" de 5 dígitos, ou
    /// funcional-programática completa "10.301.0002.2010" / "10301..."). Extrai os 2 primeiros dígitos
    /// numéricos como função e os 3 seguintes como subfunção (quando presentes).
    /// </summary>
    /// <param name="texto">Texto da funcional (função, FS ou funcional-programática).</param>
    /// <returns>Instância de <see cref="CodigoFuncional"/>.</returns>
    /// <exception cref="ArgumentException">Se não houver ao menos 2 dígitos para a função.</exception>
    public static CodigoFuncional DeTexto(string texto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);

        // Mantém apenas dígitos: tolera separadores ".", "-", espaços (funcional-programática) e o
        // formato "FS" de 5 dígitos (Portaria 642/2019). A função é sempre os 2 primeiros dígitos.
        Span<char> digitos = stackalloc char[texto.Length];
        var n = 0;
        foreach (var c in texto)
        {
            if (char.IsDigit(c))
            {
                digitos[n++] = c;
            }
        }

        if (n < 2)
        {
            throw new ArgumentException("Funcional sem dígitos suficientes para extrair a função (mínimo 2).", nameof(texto));
        }

        var funcao = new string(digitos[..2]);
        string? subfuncao = n >= 5 ? new string(digitos.Slice(2, 3)) : null;
        return new CodigoFuncional(funcao, subfuncao);
    }

    /// <summary>Cria diretamente a partir da função (2 dígitos) e subfunção opcional.</summary>
    /// <param name="funcao">Código da função (2 dígitos).</param>
    /// <param name="subfuncao">Código da subfunção (3 dígitos), opcional.</param>
    /// <returns>Instância de <see cref="CodigoFuncional"/>.</returns>
    /// <exception cref="ArgumentException">Se a função não tiver 2 dígitos numéricos.</exception>
    public static CodigoFuncional De(string funcao, string? subfuncao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(funcao);
        var f = funcao.Trim();
        if (f.Length != 2 || !int.TryParse(f, out _))
        {
            throw new ArgumentException("Função deve ter 2 dígitos numéricos.", nameof(funcao));
        }

        return new CodigoFuncional(f, string.IsNullOrWhiteSpace(subfuncao) ? null : subfuncao.Trim());
    }

    /// <inheritdoc />
    public override string ToString() => Subfuncao is null ? Funcao : $"{Funcao}.{Subfuncao}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Funcao;
        yield return Subfuncao;
    }
}
