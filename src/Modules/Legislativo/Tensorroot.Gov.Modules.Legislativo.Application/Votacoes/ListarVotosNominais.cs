using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Resumo de um voto nominal (transparencia/LAI).</summary>
/// <param name="VereadorId">Vereador autor do voto.</param>
/// <param name="Sentido">Sentido do voto.</param>
/// <param name="RegistradoEm">Momento do registro.</param>
public sealed record VotoResumo(Guid VereadorId, string Sentido, DateTimeOffset RegistradoEm);

/// <summary>
/// Lista os votos individuais de uma votacao Nominal/Simbolica (transparencia/LAI).
/// Bloqueada para votacao Secreta, preservando o sigilo do voto (I-12).
/// </summary>
/// <param name="VotacaoId">Votacao a consultar.</param>
public sealed record ListarVotosNominaisQuery(Guid VotacaoId) : IQuery<IReadOnlyList<VotoResumo>>;

/// <summary>Handler da listagem de votos nominais.</summary>
public sealed class ListarVotosNominaisHandler(IVotacaoRepository votacoes)
    : IQueryHandler<ListarVotosNominaisQuery, IReadOnlyList<VotoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VotoResumo>> Handle(ListarVotosNominaisQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        // I-12: em votacao secreta, a identificacao individual nunca e exposta.
        if (votacao.Tipo == TipoVotacao.Secreta)
        {
            throw new InvalidOperationException("Votacao secreta nao expoe votos individuais.");
        }

        return votacao.Votos
            .Select(voto => new VotoResumo(voto.VereadorId.Value, voto.Sentido.ToString(), voto.RegistradoEm))
            .ToList();
    }
}
