using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Resumo de um fornecedor para listagens.</summary>
/// <param name="Id">Identificador do fornecedor.</param>
/// <param name="Cnpj">CNPJ formatado (com mascara).</param>
/// <param name="RazaoSocial">Razao social.</param>
/// <param name="Situacao">Situacao cadastral (nome do enum).</param>
/// <param name="NivelCadastralSICAF">Nivel cadastral no SICAF (nome do enum).</param>
public sealed record FornecedorResumo(
    Guid Id,
    string Cnpj,
    string RazaoSocial,
    string Situacao,
    string NivelCadastralSICAF);

/// <summary>Lista os fornecedores com sancao impeditiva vigente na referencia (tenant-scoped).</summary>
/// <param name="Referencia">Data de referencia para a vigencia impeditiva.</param>
public sealed record ListarFornecedoresImpedidosQuery(DateOnly Referencia) : IQuery<IReadOnlyList<FornecedorResumo>>;

/// <summary>Handler da listagem de fornecedores impedidos.</summary>
public sealed class ListarFornecedoresImpedidosHandler(IFornecedorRepository fornecedores)
    : IQueryHandler<ListarFornecedoresImpedidosQuery, IReadOnlyList<FornecedorResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FornecedorResumo>> Handle(
        ListarFornecedoresImpedidosQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var impedidos = await fornecedores.ListarImpedidosAsync(request.Referencia, cancellationToken).ConfigureAwait(false);

        return impedidos
            .Select(fornecedor => new FornecedorResumo(
                fornecedor.Id.Value,
                fornecedor.Cnpj.Formatar(),
                fornecedor.RazaoSocial,
                fornecedor.Situacao.ToString(),
                fornecedor.NivelCadastralSICAF.ToString()))
            .ToList();
    }
}
