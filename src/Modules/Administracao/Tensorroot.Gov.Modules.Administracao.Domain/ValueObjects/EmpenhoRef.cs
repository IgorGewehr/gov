namespace Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

/// <summary>
/// Referencia (somente por Id) ao ato de empenho do modulo Financas que reserva a dotacao
/// orcamentaria do contrato (Lei 4.320; LRF). Value Object sem navegacao cruzada entre raizes
/// ou modulos — a integracao ocorre exclusivamente via Integration Events.
/// </summary>
/// <param name="EmpenhoId">Identificador do empenho no modulo Financas.</param>
/// <param name="NumeroEmpenho">Numero do empenho (rotulo administrativo).</param>
public readonly record struct EmpenhoRef(Guid EmpenhoId, string NumeroEmpenho)
{
    /// <summary>Cria uma referencia de empenho valida.</summary>
    /// <param name="empenhoId">Identificador do empenho (nao vazio).</param>
    /// <param name="numeroEmpenho">Numero do empenho (nao vazio).</param>
    /// <returns>Nova <see cref="EmpenhoRef"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o identificador for vazio.</exception>
    /// <exception cref="ArgumentException">Se o numero for vazio.</exception>
    public static EmpenhoRef De(Guid empenhoId, string numeroEmpenho)
    {
        if (empenhoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(empenhoId), "Empenho e obrigatorio.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(numeroEmpenho);
        return new EmpenhoRef(empenhoId, numeroEmpenho);
    }

    /// <inheritdoc />
    public override string ToString() => NumeroEmpenho;
}
