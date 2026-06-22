using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

/// <summary>
/// Manifestacao de um vereador na votacao (sim / nao / abstencao). Entidade do agregado
/// <see cref="Votacao"/>. Pertence a trilha imutavel (append-only) — para prova juridica e LAI,
/// votos nao podem ser removidos ou alterados (I-11). O <see cref="VotoId"/> e a origem do painel
/// eletronico e a chave de idempotencia (I-3).
/// </summary>
public sealed class Voto : Entity<VotoId>
{
    private Voto()
    {
    }

    private Voto(VotoId id, VereadorId vereadorId, SentidoVoto sentido, DateTimeOffset registradoEm)
        : base(id)
    {
        VereadorId = vereadorId;
        Sentido = sentido;
        RegistradoEm = registradoEm;
    }

    /// <summary>Vereador que manifestou o voto (omitido/encriptado em votacao secreta — I-12).</summary>
    public VereadorId VereadorId { get; private set; }

    /// <summary>Sentido (direcao) do voto.</summary>
    public SentidoVoto Sentido { get; private set; }

    /// <summary>Momento do registro do voto.</summary>
    public DateTimeOffset RegistradoEm { get; private set; }

    /// <summary>Registra um voto na trilha imutavel.</summary>
    /// <param name="votoId">Identificador do voto (origem do painel, chave de idempotencia).</param>
    /// <param name="vereadorId">Vereador autor do voto.</param>
    /// <param name="sentido">Sentido do voto.</param>
    /// <param name="registradoEm">Momento do registro.</param>
    /// <returns>Novo <see cref="Voto"/>.</returns>
    internal static Voto Registrar(VotoId votoId, VereadorId vereadorId, SentidoVoto sentido, DateTimeOffset registradoEm)
        => new(votoId, vereadorId, sentido, registradoEm);
}
