using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>
/// Função/subfunção da despesa (Portaria MOG 42/1999) na ótica do módulo Educação. A <b>função</b> são
/// os 2 primeiros dígitos (a função "12" é Educação); a <b>subfunção</b>, os 3 seguintes. A subfunção é o
/// que distingue, dentro da função 12, o que é Manutenção e Desenvolvimento do Ensino (MDE) do que NÃO é
/// (ex.: 12.361 Ensino Fundamental e 12.306 Alimentação e Nutrição/merenda) — daí a importância de
/// carregá-la para a classificação fina (LDB Lei 9.394/1996, arts. 70 inclui / 71 exclui).
/// <para>
/// Réplica local do <c>CodigoFuncional</c> do M7.0 (que vive em Transparencia.Domain), espelhando o
/// <c>CodigoFuncionalSaude</c>: o isolamento de módulo (CLAUDE.md §2) proíbe referenciar o interno de
/// outro módulo — o cross-module só anda por <c>*.Contracts</c>. O padrão de extração de função/subfunção
/// é o mesmo; a classificação MDE é própria do bounded context de Educação.
/// // TODO(validar-oficial): dígitos exatos por função/subfunção na MOG 42/1999.
/// </para>
/// </summary>
public sealed class CodigoFuncionalEducacao : ValueObject
{
    private CodigoFuncionalEducacao(string funcao, string? subfuncao)
    {
        Funcao = funcao;
        Subfuncao = subfuncao;
    }

    /// <summary>Código da função (2 dígitos; "12" = Educação).</summary>
    public string Funcao { get; }

    /// <summary>Código da subfunção (3 dígitos), quando disponível.</summary>
    public string? Subfuncao { get; }

    /// <summary>
    /// Cria a partir de uma representação textual da funcional (função pura, "FS" de 5 dígitos ou
    /// funcional-programática completa "12.361.0002.2010" / "12361..."). Extrai os 2 primeiros dígitos
    /// como função e os 3 seguintes como subfunção (quando presentes). Tolera separadores ".", "-", espaços.
    /// </summary>
    /// <param name="texto">Texto da funcional.</param>
    /// <returns>Instância de <see cref="CodigoFuncionalEducacao"/>.</returns>
    /// <exception cref="ArgumentException">Se não houver ao menos 2 dígitos para a função.</exception>
    public static CodigoFuncionalEducacao DeTexto(string texto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);

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
        return new CodigoFuncionalEducacao(funcao, subfuncao);
    }

    /// <summary>Cria diretamente a partir da função (2 dígitos) e subfunção opcional (3 dígitos).</summary>
    /// <param name="funcao">Código da função (2 dígitos).</param>
    /// <param name="subfuncao">Código da subfunção (3 dígitos), opcional.</param>
    /// <returns>Instância de <see cref="CodigoFuncionalEducacao"/>.</returns>
    /// <exception cref="ArgumentException">Se a função não tiver 2 dígitos numéricos ou a subfunção não tiver 3.</exception>
    public static CodigoFuncionalEducacao De(string funcao, string? subfuncao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(funcao);
        var f = funcao.Trim();
        if (f.Length != 2 || !int.TryParse(f, out _))
        {
            throw new ArgumentException("Função deve ter 2 dígitos numéricos.", nameof(funcao));
        }

        string? sf = null;
        if (!string.IsNullOrWhiteSpace(subfuncao))
        {
            sf = subfuncao.Trim();
            if (sf.Length != 3 || !int.TryParse(sf, out _))
            {
                throw new ArgumentException("Subfunção deve ter 3 dígitos numéricos.", nameof(subfuncao));
            }
        }

        return new CodigoFuncionalEducacao(f, sf);
    }

    /// <summary>Função "12" (Educação) da Portaria MOG 42/1999 — discriminador primário da MDE.</summary>
    public const string FuncaoEducacao = "12";

    /// <summary>Indica se a função é a de Educação (12).</summary>
    public bool EhFuncaoEducacao => string.Equals(Funcao, FuncaoEducacao, StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() => Subfuncao is null ? Funcao : $"{Funcao}.{Subfuncao}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Funcao;
        yield return Subfuncao;
    }
}
