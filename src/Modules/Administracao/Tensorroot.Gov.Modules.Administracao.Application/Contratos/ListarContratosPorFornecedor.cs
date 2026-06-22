using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Lista os contratos de um fornecedor no tenant (tenant-scoped).</summary>
/// <param name="FornecedorId">Fornecedor a filtrar.</param>
public sealed record ListarContratosPorFornecedorQuery(Guid FornecedorId) : IQuery<IReadOnlyList<ContratoResumo>>;

/// <summary>Handler da consulta de contratos por fornecedor.</summary>
public sealed class ListarContratosPorFornecedorHandler(IContratoRepository contratos)
    : IQueryHandler<ListarContratosPorFornecedorQuery, IReadOnlyList<ContratoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContratoResumo>> Handle(
        ListarContratosPorFornecedorQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var doFornecedor = await contratos.ListarPorFornecedorAsync(request.FornecedorId, cancellationToken).ConfigureAwait(false);

        return doFornecedor
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
