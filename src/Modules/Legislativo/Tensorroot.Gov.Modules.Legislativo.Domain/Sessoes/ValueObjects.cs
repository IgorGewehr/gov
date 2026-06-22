using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;

/// <summary>
/// Momento (data e hora) agendado/realizado de uma sessao plenaria. Nao admite valor padrao
/// (validado na fabrica <see cref="Sessao.Agendar"/>).
/// </summary>
public sealed class DataHora : ValueObject
{
    private DataHora(DateTimeOffset valor)
    {
        Valor = valor;
    }

    /// <summary>Instante agendado/realizado da sessao.</summary>
    public DateTimeOffset Valor { get; }

    /// <summary>Cria um <see cref="DataHora"/> valido.</summary>
    /// <param name="valor">Momento da sessao (nao default).</param>
    /// <returns>Instancia de <see cref="DataHora"/>.</returns>
    /// <exception cref="ArgumentException">Quando o valor for o padrao (nao informado).</exception>
    public static DataHora De(DateTimeOffset valor)
    {
        if (valor == default)
        {
            throw new ArgumentException("Data e hora da sessao sao obrigatorias.", nameof(valor));
        }

        return new DataHora(valor);
    }

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("O");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valor;
    }
}

/// <summary>Identificador forte de um vereador (membro da Camara) que registra presenca.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct VereadorId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="VereadorId"/>.</returns>
    public static VereadorId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
