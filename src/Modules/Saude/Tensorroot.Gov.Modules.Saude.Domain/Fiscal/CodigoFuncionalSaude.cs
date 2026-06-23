using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>
/// Função/subfunção da despesa (Portaria MOG 42/1999) na ótica do módulo Saúde. A <b>função</b> são os
/// 2 primeiros dígitos (a função "10" é Saúde); a <b>subfunção</b>, os 3 seguintes. A subfunção é o que
/// distingue, dentro da função 10, o que é ASPS do que NÃO é (ex.: 10.512 Saneamento Básico Urbano e
/// 10.301 Atenção Básica) — daí a importância de carregá-la para a classificação fina (LC 141/2012).
/// <para>
/// Réplica local do <c>CodigoFuncional</c> do M7.0 (que vive em Transparencia.Domain): o isolamento de
/// módulo (CLAUDE.md §2) proíbe referenciar o interno de outro módulo — o cross-module só anda por
/// <c>*.Contracts</c>. O padrão de extração de função/subfunção é o mesmo; a classificação ASPS é própria
/// do bounded context de Saúde. // TODO(validar-oficial): dígitos exatos por função/subfunção na MOG 42/1999.
/// </para>
/// </summary>
public sealed class CodigoFuncionalSaude : ValueObject
{
    private CodigoFuncionalSaude(string funcao, string? subfuncao)
    {
        Funcao = funcao;
        Subfuncao = subfuncao;
    }

    /// <summary>Código da função (2 dígitos; "10" = Saúde).</summary>
    public string Funcao { get; }

    /// <summary>Código da subfunção (3 dígitos), quando disponível.</summary>
    public string? Subfuncao { get; }

    /// <summary>
    /// Cria a partir de uma representação textual da funcional (função pura, "FS" de 5 dígitos ou
    /// funcional-programática completa "10.301.0002.2010" / "10301..."). Extrai os 2 primeiros dígitos
    /// como função e os 3 seguintes como subfunção (quando presentes). Tolera separadores ".", "-", espaços.
    /// </summary>
    /// <param name="texto">Texto da funcional.</param>
    /// <returns>Instância de <see cref="CodigoFuncionalSaude"/>.</returns>
    /// <exception cref="ArgumentException">Se não houver ao menos 2 dígitos para a função.</exception>
    public static CodigoFuncionalSaude DeTexto(string texto)
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
        return new CodigoFuncionalSaude(funcao, subfuncao);
    }

    /// <summary>Cria diretamente a partir da função (2 dígitos) e subfunção opcional (3 dígitos).</summary>
    /// <param name="funcao">Código da função (2 dígitos).</param>
    /// <param name="subfuncao">Código da subfunção (3 dígitos), opcional.</param>
    /// <returns>Instância de <see cref="CodigoFuncionalSaude"/>.</returns>
    /// <exception cref="ArgumentException">Se a função não tiver 2 dígitos numéricos ou a subfunção não tiver 3.</exception>
    public static CodigoFuncionalSaude De(string funcao, string? subfuncao = null)
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

        return new CodigoFuncionalSaude(f, sf);
    }

    /// <summary>Função "10" (Saúde) da Portaria MOG 42/1999 — discriminador primário das ASPS.</summary>
    public const string FuncaoSaude = "10";

    /// <summary>Indica se a função é a de Saúde (10).</summary>
    public bool EhFuncaoSaude => string.Equals(Funcao, FuncaoSaude, StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() => Subfuncao is null ? Funcao : $"{Funcao}.{Subfuncao}";

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Funcao;
        yield return Subfuncao;
    }
}
