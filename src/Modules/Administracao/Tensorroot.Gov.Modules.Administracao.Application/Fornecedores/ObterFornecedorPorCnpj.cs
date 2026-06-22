using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Obtem o detalhe de um fornecedor pelo CNPJ (tenant-scoped).</summary>
/// <param name="Cnpj">CNPJ a consultar (com ou sem mascara).</param>
public sealed record ObterFornecedorPorCnpjQuery(string Cnpj) : IQuery<FornecedorDetalhe?>;

/// <summary>Handler da consulta de detalhe do fornecedor por CNPJ.</summary>
public sealed class ObterFornecedorPorCnpjHandler(
    IFornecedorRepository fornecedores,
    TimeProvider timeProvider)
    : IQueryHandler<ObterFornecedorPorCnpjQuery, FornecedorDetalhe?>
{
    /// <inheritdoc />
    public async Task<FornecedorDetalhe?> Handle(ObterFornecedorPorCnpjQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Cnpj.TryCreate(request.Cnpj, out var cnpj) || cnpj is null)
        {
            return null;
        }

        var fornecedor = await fornecedores.ObterPorCnpjAsync(cnpj, cancellationToken).ConfigureAwait(false);
        if (fornecedor is null)
        {
            return null;
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return FornecedorProjecao.ParaDetalhe(fornecedor, hoje);
    }
}
