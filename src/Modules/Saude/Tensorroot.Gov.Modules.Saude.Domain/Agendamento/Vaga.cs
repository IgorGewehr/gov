using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Agendamento;

/// <summary>
/// Slot de atendimento (vaga) de uma <see cref="AgendaProfissional"/>: uma data/hora marcavel com
/// capacidade unitaria. E uma entidade OWNED do agregado agenda — toda transicao de situacao da vaga
/// ocorre por metodos do agregado, nunca diretamente. A unicidade de ocupacao (anti-overbooking) e
/// garantida pela transicao <see cref="Ocupar"/>, que exige a vaga <see cref="SituacaoVaga.Livre"/>.
/// </summary>
public sealed class Vaga : Entity<VagaId>
{
    private Vaga()
    {
    }

    private Vaga(VagaId id, DateTimeOffset dataHora, SituacaoVaga situacao)
        : base(id)
    {
        DataHora = dataHora;
        Situacao = situacao;
    }

    /// <summary>Data/hora exata do slot.</summary>
    public DateTimeOffset DataHora { get; private set; }

    /// <summary>Situacao atual do slot.</summary>
    public SituacaoVaga Situacao { get; private set; }

    /// <summary>Indica se a vaga esta livre para marcacao.</summary>
    public bool EstaLivre => Situacao == SituacaoVaga.Livre;

    /// <summary>Cria uma vaga LIVRE em <paramref name="dataHora"/> (uso interno do agregado na expansao da grade).</summary>
    /// <param name="dataHora">Data/hora do slot.</param>
    /// <returns>Nova <see cref="Vaga"/> livre.</returns>
    internal static Vaga Criar(DateTimeOffset dataHora) => new(VagaId.New(), dataHora, SituacaoVaga.Livre);

    /// <summary>
    /// Ocupa a vaga (marcacao). Invariante anti-overbooking: exige situacao <see cref="SituacaoVaga.Livre"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a vaga nao estiver livre.</exception>
    internal void Ocupar()
    {
        if (Situacao != SituacaoVaga.Livre)
        {
            throw new InvalidOperationException($"Vaga indisponivel para marcacao. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoVaga.Ocupada;
    }

    /// <summary>Libera a vaga (cancelamento/falta): volta a <see cref="SituacaoVaga.Livre"/> se estava ocupada/reservada.</summary>
    internal void Liberar()
    {
        if (Situacao is SituacaoVaga.Ocupada or SituacaoVaga.Reservada)
        {
            Situacao = SituacaoVaga.Livre;
        }
    }

    /// <summary>Bloqueia a vaga (indisponibilidade do dia). Nao bloqueia vagas ja ocupadas.</summary>
    internal void Bloquear()
    {
        if (Situacao == SituacaoVaga.Livre)
        {
            Situacao = SituacaoVaga.Bloqueada;
        }
    }

    /// <summary>Reabre a vaga bloqueada (volta a <see cref="SituacaoVaga.Livre"/>).</summary>
    internal void Reabrir()
    {
        if (Situacao == SituacaoVaga.Bloqueada)
        {
            Situacao = SituacaoVaga.Livre;
        }
    }
}
