using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Identificador forte de uma <see cref="Tramitacao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct TramitacaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="TramitacaoId"/>.</returns>
    public static TramitacaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Fase percorrida pela proposicao na trilha de tramitacao (append-only, imutavel — prova
/// juridica e LAI). Quando a fase e <see cref="FaseTramitacao.Parecer"/>, referencia a
/// Comissao e o sentido (favoravel/contrario) do parecer emitido.
/// </summary>
public sealed class Tramitacao : Entity<TramitacaoId>
{
    private Tramitacao()
    {
    }

    private Tramitacao(
        TramitacaoId id,
        ProposicaoId proposicaoId,
        FaseTramitacao fase,
        string? comissao,
        bool? parecerFavoravel,
        DateOnly data)
        : base(id)
    {
        ProposicaoId = proposicaoId;
        Fase = fase;
        Comissao = comissao;
        ParecerFavoravel = parecerFavoravel;
        Data = data;
    }

    /// <summary>Proposicao a qual a fase se vincula.</summary>
    public ProposicaoId ProposicaoId { get; private set; }

    /// <summary>Fase de tramitacao registrada.</summary>
    public FaseTramitacao Fase { get; private set; }

    /// <summary>Comissao emitente (quando a fase for parecer); nulo nas demais fases.</summary>
    public string? Comissao { get; private set; }

    /// <summary>Sentido do parecer (favoravel/contrario); nulo quando nao houver parecer.</summary>
    public bool? ParecerFavoravel { get; private set; }

    /// <summary>Data da fase.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Registra uma fase generica de tramitacao (sem parecer).</summary>
    /// <param name="proposicaoId">Proposicao vinculada.</param>
    /// <param name="fase">Fase de tramitacao.</param>
    /// <param name="data">Data da fase.</param>
    /// <returns>Nova <see cref="Tramitacao"/>.</returns>
    public static Tramitacao RegistrarFase(ProposicaoId proposicaoId, FaseTramitacao fase, DateOnly data)
        => new(TramitacaoId.New(), proposicaoId, fase, comissao: null, parecerFavoravel: null, data);

    /// <summary>Registra a fase de parecer de uma Comissao na trilha imutavel.</summary>
    /// <param name="proposicaoId">Proposicao vinculada.</param>
    /// <param name="comissao">Comissao emitente (ex.: Ccj, FinancasOrcamento).</param>
    /// <param name="favoravel">Sentido do parecer.</param>
    /// <param name="data">Data do parecer.</param>
    /// <returns>Nova <see cref="Tramitacao"/> de parecer.</returns>
    /// <exception cref="ArgumentException">Se a comissao for vazia.</exception>
    public static Tramitacao RegistrarParecer(ProposicaoId proposicaoId, string comissao, bool favoravel, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(comissao);
        return new Tramitacao(TramitacaoId.New(), proposicaoId, FaseTramitacao.Parecer, comissao.Trim(), favoravel, data);
    }
}
