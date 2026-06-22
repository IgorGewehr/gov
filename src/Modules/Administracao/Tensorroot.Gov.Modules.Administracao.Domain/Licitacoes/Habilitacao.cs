using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

/// <summary>Identificador forte de uma <see cref="Habilitacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct HabilitacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="HabilitacaoId"/>.</returns>
    public static HabilitacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Verificacao de aptidao juridica, fiscal, tecnica e economica do licitante.</summary>
public sealed class Habilitacao : Entity<HabilitacaoId>
{
    private Habilitacao()
    {
    }

    private Habilitacao(HabilitacaoId id, Guid fornecedorId, ResultadoHabilitacao resultado, string? motivo)
        : base(id)
    {
        FornecedorId = fornecedorId;
        Resultado = resultado;
        Motivo = motivo;
    }

    /// <summary>Licitante verificado.</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Resultado da verificacao.</summary>
    public ResultadoHabilitacao Resultado { get; private set; }

    /// <summary>Motivo da (in)habilitacao, quando aplicavel.</summary>
    public string? Motivo { get; private set; }

    /// <summary>Registra o resultado de habilitacao de um licitante.</summary>
    /// <param name="fornecedorId">Licitante verificado.</param>
    /// <param name="resultado">Resultado (habilitado/inabilitado).</param>
    /// <param name="motivo">Motivo (opcional).</param>
    /// <returns>Nova <see cref="Habilitacao"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o fornecedor nao for informado.</exception>
    public static Habilitacao Registrar(Guid fornecedorId, ResultadoHabilitacao resultado, string? motivo)
    {
        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorId), "Fornecedor e obrigatorio na habilitacao.");
        }

        return new Habilitacao(HabilitacaoId.New(), fornecedorId, resultado, motivo);
    }
}
