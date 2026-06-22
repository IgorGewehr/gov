namespace Tensorroot.Gov.SharedKernel.Primitives;

/// <summary>
/// Objeto de Valor: tipo imutável comparado por igualdade estrutural
/// (todos os seus componentes), sem identidade própria.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Componentes que definem a igualdade estrutural do objeto de valor.</summary>
    /// <returns>Sequência ordenada de componentes comparáveis.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other)
        => other is not null
        && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => GetEqualityComponents().Aggregate(0, static (hash, component) => HashCode.Combine(hash, component));

    /// <summary>Compara dois objetos de valor por igualdade estrutural.</summary>
    /// <param name="left">Primeiro objeto.</param>
    /// <param name="right">Segundo objeto.</param>
    /// <returns><c>true</c> se forem estruturalmente iguais.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    /// <summary>Compara dois objetos de valor por diferença estrutural.</summary>
    /// <param name="left">Primeiro objeto.</param>
    /// <param name="right">Segundo objeto.</param>
    /// <returns><c>true</c> se forem estruturalmente diferentes.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
