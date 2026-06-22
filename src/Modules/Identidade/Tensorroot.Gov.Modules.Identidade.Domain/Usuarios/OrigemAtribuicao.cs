using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

/// <summary>Como uma <see cref="AtribuicaoDePapel"/> foi criada.</summary>
public enum TipoOrigem
{
    /// <summary>Atribuida diretamente por um administrador do tenant.</summary>
    Direta = 0,

    /// <summary>Concedida por um delegado, no escopo de uma delegacao de administracao (MODELO §4).</summary>
    Delegada = 1,
}

/// <summary>
/// Procedencia de uma <see cref="AtribuicaoDePapel"/> (MODELO §2.2): direta (admin do tenant) ou
/// delegada — neste caso, registra quem concedeu (o delegado concedente), essencial para a trilha
/// imutavel (I8) e para a regra "nao delega o que nao tem" (I4). Objeto de Valor imutavel.
/// </summary>
public sealed class OrigemAtribuicao : ValueObject
{
    private OrigemAtribuicao(TipoOrigem tipo, UsuarioId? concedentId)
    {
        Tipo = tipo;
        ConcedentId = concedentId;
    }

    /// <summary>Tipo de origem.</summary>
    public TipoOrigem Tipo { get; }

    /// <summary>Usuario que concedeu (apenas quando <see cref="TipoOrigem.Delegada"/>); senao <c>null</c>.</summary>
    public UsuarioId? ConcedentId { get; }

    /// <summary>Origem direta (administrador do tenant; sem concedente registrado).</summary>
    /// <returns>Origem <see cref="TipoOrigem.Direta"/>.</returns>
    public static OrigemAtribuicao Direta() => new(TipoOrigem.Direta, concedentId: null);

    /// <summary>Origem delegada, com o usuario concedente registrado.</summary>
    /// <param name="concedentId">Usuario que concedeu a atribuicao.</param>
    /// <returns>Origem <see cref="TipoOrigem.Delegada"/>.</returns>
    public static OrigemAtribuicao Delegada(UsuarioId concedentId) => new(TipoOrigem.Delegada, concedentId);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Tipo;
        yield return ConcedentId;
    }
}
