using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

/// <summary>Identificador forte de um <see cref="PlanoAcompanhamentoFamiliar"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct PlanoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="PlanoId"/>.</returns>
    public static PlanoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Plano de acompanhamento familiar (entidade-filha do <see cref="ProntuarioSuas"/>):
/// objetivos e compromissos pactuados com a familia. Conteudo sigiloso (LGPD art. 11).
/// </summary>
public sealed class PlanoAcompanhamentoFamiliar : Entity<PlanoId>
{
    private readonly List<string> _compromissos = [];

    private PlanoAcompanhamentoFamiliar()
    {
    }

    private PlanoAcompanhamentoFamiliar(
        PlanoId id,
        string objetivos,
        DateOnly dataPactuacao,
        IEnumerable<string> compromissos)
        : base(id)
    {
        Objetivos = objetivos;
        DataPactuacao = dataPactuacao;
        _compromissos.AddRange(compromissos);
    }

    /// <summary>Objetivos pactuados — conteudo sigiloso.</summary>
    public string Objetivos { get; private set; } = default!;

    /// <summary>Data de pactuacao do plano com a familia.</summary>
    public DateOnly DataPactuacao { get; private set; }

    /// <summary>Compromissos pactuados com a familia.</summary>
    public IReadOnlyCollection<string> Compromissos => _compromissos;

    /// <summary>Define (pactua) um plano de acompanhamento familiar.</summary>
    /// <param name="objetivos">Objetivos do plano.</param>
    /// <param name="dataPactuacao">Data de pactuacao.</param>
    /// <param name="compromissos">Compromissos pactuados (opcional).</param>
    /// <returns>Novo <see cref="PlanoAcompanhamentoFamiliar"/>.</returns>
    /// <exception cref="ArgumentException">Se os objetivos forem vazios.</exception>
    internal static PlanoAcompanhamentoFamiliar Pactuar(
        string objetivos,
        DateOnly dataPactuacao,
        IEnumerable<string>? compromissos = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objetivos);
        return new PlanoAcompanhamentoFamiliar(
            PlanoId.New(),
            objetivos,
            dataPactuacao,
            compromissos ?? []);
    }
}
