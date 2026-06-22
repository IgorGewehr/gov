using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.DiarioOficial;

/// <summary>Identificador forte de uma <see cref="MateriaDiario"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct MateriaDiarioId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="MateriaDiarioId"/>.</returns>
    public static MateriaDiarioId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Materia (ato) publicada em uma edicao do Diario Oficial: norma, ata, edital, portaria, extrato.
/// Entidade do agregado <see cref="EdicaoDiario"/>; pode referenciar a entidade de origem (mesmo
/// modulo) por <see cref="ReferenciaId"/> ou trazer o conteudo bruto.
/// </summary>
public sealed class MateriaDiario : Entity<MateriaDiarioId>
{
    /// <summary>Comprimento maximo do titulo da materia.</summary>
    public const int TituloMaximo = 300;

    private MateriaDiario()
    {
    }

    private MateriaDiario(
        MateriaDiarioId id,
        TipoMateria tipo,
        string titulo,
        string? conteudo,
        Guid? referenciaId,
        int ordem)
        : base(id)
    {
        Tipo = tipo;
        Titulo = titulo;
        Conteudo = conteudo;
        ReferenciaId = referenciaId;
        Ordem = ordem;
    }

    /// <summary>Especie da materia.</summary>
    public TipoMateria Tipo { get; private set; }

    /// <summary>Titulo da materia (nao vazio).</summary>
    public string Titulo { get; private set; } = default!;

    /// <summary>Conteudo bruto (opcional quando ha <see cref="ReferenciaId"/>).</summary>
    public string? Conteudo { get; private set; }

    /// <summary>Identificador da entidade de origem no mesmo modulo (norma/ata), quando aplicavel.</summary>
    public Guid? ReferenciaId { get; private set; }

    /// <summary>Ordem sequencial da materia dentro da edicao.</summary>
    public int Ordem { get; private set; }

    /// <summary>Cria uma materia da edicao (D-2: titulo nao vazio; ordem sequencial atribuida pelo agregado).</summary>
    /// <param name="tipo">Especie da materia.</param>
    /// <param name="titulo">Titulo (obrigatorio).</param>
    /// <param name="conteudo">Conteudo bruto (opcional).</param>
    /// <param name="referenciaId">Referencia a entidade de origem (opcional).</param>
    /// <param name="ordem">Ordem sequencial.</param>
    /// <returns>Nova <see cref="MateriaDiario"/>.</returns>
    /// <exception cref="ArgumentException">Se o titulo for vazio, o tipo invalido ou faltarem conteudo e referencia.</exception>
    public static MateriaDiario Criar(TipoMateria tipo, string titulo, string? conteudo, Guid? referenciaId, int ordem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de materia invalido.", nameof(tipo));
        }

        var conteudoNormalizado = string.IsNullOrWhiteSpace(conteudo) ? null : conteudo.Trim();
        if (conteudoNormalizado is null && referenciaId is null)
        {
            throw new ArgumentException("Materia exige conteudo ou referencia a entidade de origem.", nameof(conteudo));
        }

        var tituloNormalizado = titulo.Trim();
        if (tituloNormalizado.Length > TituloMaximo)
        {
            throw new ArgumentException($"Titulo excede {TituloMaximo} caracteres.", nameof(titulo));
        }

        return new MateriaDiario(MateriaDiarioId.New(), tipo, tituloNormalizado, conteudoNormalizado, referenciaId, ordem);
    }
}
