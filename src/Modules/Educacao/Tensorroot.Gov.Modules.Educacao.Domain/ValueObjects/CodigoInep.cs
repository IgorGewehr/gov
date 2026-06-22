namespace Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

/// <summary>
/// Codigo INEP: identificador unico nacional de uma escola, chave de integracao com o
/// Censo Escolar/EducaCenso (INEP). Nao nulo/nao vazio; armazenado normalizado (sem espacos).
/// </summary>
/// <param name="Valor">Texto do codigo INEP (8 digitos no leiaute do Censo).</param>
public readonly record struct CodigoInep(string Valor)
{
    /// <summary>Comprimento maximo do codigo INEP (8 digitos no leiaute do EducaCenso).</summary>
    public const int ComprimentoMaximo = 8;

    /// <summary>Cria um codigo INEP validado (nao vazio e dentro do limite).</summary>
    /// <param name="valor">Texto do codigo INEP.</param>
    /// <returns>Instancia de <see cref="CodigoInep"/> normalizada.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    public static CodigoInep Criar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Codigo INEP excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        return new CodigoInep(normalizado);
    }

    /// <summary>Indica se o valor informado e um codigo INEP de formato valido.</summary>
    /// <param name="valor">Codigo a verificar.</param>
    /// <returns><c>true</c> se valido.</returns>
    public static bool EhValido(string? valor) =>
        !string.IsNullOrWhiteSpace(valor) && valor.Trim().Length <= ComprimentoMaximo;

    /// <inheritdoc />
    public override string ToString() => Valor;
}
