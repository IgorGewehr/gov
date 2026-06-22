using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

/// <summary>Identificador forte de um <see cref="RegistroAcompanhamento"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroAcompanhamentoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroAcompanhamentoId"/>.</returns>
    public static RegistroAcompanhamentoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Anotacao de um atendimento/evolucao do acompanhamento familiar (entidade-filha do
/// <see cref="ProntuarioSuas"/>). O conteudo (<see cref="Descricao"/>) e sigiloso (LGPD art. 11).
/// </summary>
public sealed class RegistroAcompanhamento : Entity<RegistroAcompanhamentoId>
{
    private RegistroAcompanhamento()
    {
    }

    private RegistroAcompanhamento(
        RegistroAcompanhamentoId id,
        TipoServico servico,
        DateOnly dataAtendimento,
        string descricao,
        Guid profissionalId)
        : base(id)
    {
        Servico = servico;
        DataAtendimento = dataAtendimento;
        Descricao = descricao;
        ProfissionalId = profissionalId;
    }

    /// <summary>Servico socioassistencial do atendimento (PAIF/PAEFI/SCFV).</summary>
    public TipoServico Servico { get; private set; }

    /// <summary>Data do atendimento.</summary>
    public DateOnly DataAtendimento { get; private set; }

    /// <summary>Descricao do atendimento — conteudo sigiloso (LGPD art. 11).</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Profissional autor do registro.</summary>
    public Guid ProfissionalId { get; private set; }

    /// <summary>Registra um novo atendimento no acompanhamento.</summary>
    /// <param name="servico">Servico socioassistencial.</param>
    /// <param name="dataAtendimento">Data do atendimento.</param>
    /// <param name="descricao">Descricao sigilosa do atendimento.</param>
    /// <param name="profissionalId">Profissional autor.</param>
    /// <returns>Novo <see cref="RegistroAcompanhamento"/>.</returns>
    /// <exception cref="ArgumentException">Se a descricao for vazia.</exception>
    internal static RegistroAcompanhamento Registrar(
        TipoServico servico,
        DateOnly dataAtendimento,
        string descricao,
        Guid profissionalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        return new RegistroAcompanhamento(
            RegistroAcompanhamentoId.New(),
            servico,
            dataAtendimento,
            descricao,
            profissionalId);
    }
}
