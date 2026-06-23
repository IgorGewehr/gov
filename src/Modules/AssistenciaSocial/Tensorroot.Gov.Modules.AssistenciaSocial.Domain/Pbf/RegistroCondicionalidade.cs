using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;

/// <summary>Identificador forte de <see cref="RegistroCondicionalidade"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RegistroCondicionalidadeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RegistroCondicionalidadeId"/>.</returns>
    public static RegistroCondicionalidadeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Registro de uma condicionalidade (educacao/saude) de um membro da familia no periodo de
/// acompanhamento PBF. Entidade-filha (owned) do agregado <see cref="AcompanhamentoCondicionalidade"/>:
/// nao implementa <c>IMustHaveTenant</c> — o isolamento por tenant e herdado do dono via FK.
/// </summary>
public sealed class RegistroCondicionalidade : Entity<RegistroCondicionalidadeId>
{
    private RegistroCondicionalidade()
    {
    }

    private RegistroCondicionalidade(
        RegistroCondicionalidadeId id,
        AcompanhamentoCondicionalidadeId acompanhamentoId,
        TipoCondicionalidade tipo,
        Guid membroId,
        StatusCondicionalidade status,
        string? observacao)
        : base(id)
    {
        AcompanhamentoId = acompanhamentoId;
        Tipo = tipo;
        MembroId = membroId;
        Status = status;
        Observacao = observacao;
    }

    /// <summary>Acompanhamento ao qual o registro pertence.</summary>
    public AcompanhamentoCondicionalidadeId AcompanhamentoId { get; private set; }

    /// <summary>Eixo da condicionalidade (educacao/saude).</summary>
    public TipoCondicionalidade Tipo { get; private set; }

    /// <summary>Membro da familia (aluno/crianca/gestante) ao qual a condicionalidade se aplica.</summary>
    public Guid MembroId { get; private set; }

    /// <summary>Status do cumprimento da condicionalidade no periodo.</summary>
    public StatusCondicionalidade Status { get; private set; }

    /// <summary>Observacao/motivo (obrigatorio na justificativa de descumprimento).</summary>
    public string? Observacao { get; private set; }

    internal static RegistroCondicionalidade Criar(
        AcompanhamentoCondicionalidadeId acompanhamentoId,
        TipoCondicionalidade tipo,
        Guid membroId,
        StatusCondicionalidade status,
        string? observacao)
    {
        if (membroId == Guid.Empty)
        {
            throw new ArgumentException("Membro da familia e obrigatorio na condicionalidade.", nameof(membroId));
        }

        // R-3: descumprimento justificado exige motivo registrado (trilha do CRAS).
        if (status == StatusCondicionalidade.Justificada && string.IsNullOrWhiteSpace(observacao))
        {
            throw new InvalidOperationException("A justificativa de descumprimento exige um motivo registrado.");
        }

        return new RegistroCondicionalidade(RegistroCondicionalidadeId.New(), acompanhamentoId, tipo, membroId, status, observacao?.Trim());
    }

    internal void Justificar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        // R-4: so descumprimento se justifica (cumprida/pendente nao tem o que justificar).
        if (Status != StatusCondicionalidade.Descumprida)
        {
            throw new InvalidOperationException($"So e possivel justificar condicionalidade descumprida. Status atual: {Status}.");
        }

        Status = StatusCondicionalidade.Justificada;
        Observacao = motivo.Trim();
    }
}
