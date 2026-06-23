using System.Text.Json.Serialization;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Autoria (iniciativa) de uma proposicao legislativa — autor(es) da materia.</summary>
/// <remarks>
/// P0-4 (robustez da fundacao): a desserializacao (System.Text.Json) de um <c>record struct</c> NAO
/// usa o construtor primario automaticamente — todo struct tem um ctor SEM-PARAMETRO implicito e, sem
/// anotacao, o STJ instancia por ele e tenta SETAR <see cref="Valor"/> (getter-only) → fica <c>null</c>,
/// SEM lancar (corrupcao silenciosa). Por isso o unico construtor que carrega <see cref="Valor"/> e
/// marcado com <see cref="JsonConstructorAttribute"/> e VALIDA a invariante: a reidratacao de um valor
/// ausente/invalido (ex.: o evento <c>ProposicaoApresentada</c> drenado do Outbox) FALHA em vez de
/// nascer com <c>Valor == null</c>. Mesmo padrao de <c>ValorMonetario</c>/<c>ClassificacaoOrcamentaria</c>.
/// </remarks>
public readonly record struct Autoria
{
    /// <summary>Comprimento maximo da descricao de autoria.</summary>
    public const int ComprimentoMaximo = 400;

    /// <summary>
    /// Cria uma autoria validada. Anotado com <see cref="JsonConstructorAttribute"/> para que o STJ
    /// reidrate POR AQUI (e nao pelo ctor sem-parametro implicito do struct) no round-trip do Outbox,
    /// validando a invariante. O nome do parametro casa com a propriedade <see cref="Valor"/>.
    /// </summary>
    /// <param name="valor">Descricao da iniciativa/autor(es).</param>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    [JsonConstructor]
    public Autoria(string valor) => Valor = Validar(valor);

    /// <summary>Descricao da iniciativa/autor(es).</summary>
    public string Valor { get; }

    /// <summary>Cria uma autoria validada (nao vazia e dentro do limite).</summary>
    /// <param name="valor">Descricao da iniciativa.</param>
    /// <returns>Instancia de <see cref="Autoria"/>.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    public static Autoria De(string valor) => new(valor);

    /// <summary>Normaliza e valida a invariante; lanca em valor ausente/vazio ou alem do limite.</summary>
    /// <param name="valor">Valor bruto (inclusive o vindo da desserializacao).</param>
    /// <returns>Valor normalizado (trim) e validado.</returns>
    /// <exception cref="ArgumentException">Se o valor for vazio ou exceder o comprimento maximo.</exception>
    private static string Validar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var normalizado = valor.Trim();
        if (normalizado.Length > ComprimentoMaximo)
        {
            throw new ArgumentException($"Autoria excede {ComprimentoMaximo} caracteres.", nameof(valor));
        }

        return normalizado;
    }

    /// <inheritdoc />
    public override string ToString() => Valor;
}
