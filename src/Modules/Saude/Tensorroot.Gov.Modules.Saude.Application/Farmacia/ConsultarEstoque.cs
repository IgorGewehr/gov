using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Posicao de estoque de medicamentos de um estabelecimento (saldo, lotes, ruptura). Dado operacional
/// (nao identifica paciente) — sem trilha LGPD. Tenant-scoped via Global Query Filter.
/// </summary>
/// <param name="EstabelecimentoId">Estabelecimento a consultar.</param>
public sealed record ObterPosicaoEstoqueQuery(Guid EstabelecimentoId)
    : IQuery<IReadOnlyList<PosicaoEstoqueDto>>;

/// <summary>Handler da posicao de estoque.</summary>
public sealed class ObterPosicaoEstoqueHandler(
    IEstoqueMedicamentoRepository estoques,
    IMedicamentoRepository medicamentos,
    TimeProvider timeProvider)
    : IQueryHandler<ObterPosicaoEstoqueQuery, IReadOnlyList<PosicaoEstoqueDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PosicaoEstoqueDto>> Handle(
        ObterPosicaoEstoqueQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var posicoes = await estoques
            .ListarPorEstabelecimentoAsync(new EstabelecimentoId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false);

        var resultado = new List<PosicaoEstoqueDto>(posicoes.Count);
        foreach (var estoque in posicoes)
        {
            var medicamento = await medicamentos.ObterPorIdAsync(estoque.MedicamentoId, cancellationToken).ConfigureAwait(false);
            var lotes = estoque.Lotes
                .OrderBy(l => l.Validade)
                .Select(l => new LoteDto(l.NumeroLote, l.Validade, l.Saldo, l.EstaVencido(hoje)))
                .ToList();

            resultado.Add(new PosicaoEstoqueDto(
                estoque.Id.Value,
                estoque.EstabelecimentoId.Value,
                estoque.MedicamentoId.Value,
                medicamento?.PrincipioAtivo ?? string.Empty,
                estoque.Saldo,
                estoque.SaldoValido(hoje),
                estoque.PontoDeRessuprimento,
                estoque.EmRuptura(hoje),
                lotes));
        }

        return resultado;
    }
}
