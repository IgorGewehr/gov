using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Lista os contratos vigentes do tenant em uma data de referencia (Eficaz/EmExecucao e nao expirados).</summary>
/// <param name="Referencia">Data de referencia.</param>
public sealed record ListarContratosVigentesQuery(DateOnly Referencia) : IQuery<IReadOnlyList<ContratoResumo>>;

/// <summary>Handler da consulta de contratos vigentes.</summary>
public sealed class ListarContratosVigentesHandler(IContratoRepository contratos)
    : IQueryHandler<ListarContratosVigentesQuery, IReadOnlyList<ContratoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContratoResumo>> Handle(
        ListarContratosVigentesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vigentes = await contratos.ListarVigentesAsync(request.Referencia, cancellationToken).ConfigureAwait(false);

        return vigentes
            .Select(contrato => new ContratoResumo(
                contrato.Id.Value,
                contrato.FornecedorId,
                contrato.Objeto,
                contrato.ValorAtual.Valor,
                contrato.VigenciaFim,
                contrato.Situacao.ToString()))
            .ToList();
    }
}
