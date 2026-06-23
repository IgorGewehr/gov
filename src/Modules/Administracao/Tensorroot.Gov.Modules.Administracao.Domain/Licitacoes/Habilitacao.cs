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

    private Habilitacao(
        HabilitacaoId id,
        Guid fornecedorId,
        ResultadoHabilitacao resultado,
        string? motivo,
        DateTimeOffset dataVerificacao,
        long sequencia)
        : base(id)
    {
        FornecedorId = fornecedorId;
        Resultado = resultado;
        Motivo = motivo;
        DataVerificacao = dataVerificacao;
        Sequencia = sequencia;
    }

    /// <summary>Licitante verificado.</summary>
    public Guid FornecedorId { get; private set; }

    /// <summary>Resultado da verificacao.</summary>
    public ResultadoHabilitacao Resultado { get; private set; }

    /// <summary>Motivo da (in)habilitacao, quando aplicavel.</summary>
    public string? Motivo { get; private set; }

    /// <summary>Momento da verificacao — prevalece a mais recente por fornecedor (BUG-A4).</summary>
    public DateTimeOffset DataVerificacao { get; private set; }

    /// <summary>
    /// Sequencia monotonica de desempate (BUG-A4): garante ordem deterministica mesmo quando duas
    /// verificacoes ocorrem no mesmo instante e apos reidratacao do EF Core (ordenacao explicita).
    /// </summary>
    public long Sequencia { get; private set; }

    /// <summary>Registra o resultado de habilitacao de um licitante.</summary>
    /// <param name="fornecedorId">Licitante verificado.</param>
    /// <param name="resultado">Resultado (habilitado/inabilitado).</param>
    /// <param name="motivo">Motivo (opcional).</param>
    /// <param name="dataVerificacao">Momento da verificacao (BUG-A4).</param>
    /// <param name="sequencia">Sequencia monotonica de desempate (BUG-A4).</param>
    /// <returns>Nova <see cref="Habilitacao"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o fornecedor nao for informado.</exception>
    internal static Habilitacao Registrar(
        Guid fornecedorId,
        ResultadoHabilitacao resultado,
        string? motivo,
        DateTimeOffset dataVerificacao,
        long sequencia)
    {
        if (fornecedorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorId), "Fornecedor e obrigatorio na habilitacao.");
        }

        return new Habilitacao(HabilitacaoId.New(), fornecedorId, resultado, motivo, dataVerificacao, sequencia);
    }
}
