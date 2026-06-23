using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using EstabelecimentoId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.EstabelecimentoId;

namespace Tensorroot.Gov.Modules.Saude.Application.Estabelecimentos;

/// <summary>Obtem a ficha completa de um estabelecimento por identificador. Tenant-scoped; read-only.</summary>
/// <param name="EstabelecimentoId">Identificador do estabelecimento.</param>
public sealed record ObterEstabelecimentoPorIdQuery(Guid EstabelecimentoId) : IQuery<EstabelecimentoDetalhe>;

/// <summary>Handler da obtencao de estabelecimento por id.</summary>
public sealed class ObterEstabelecimentoPorIdHandler(IEstabelecimentoCadastroRepository estabelecimentos)
    : IQueryHandler<ObterEstabelecimentoPorIdQuery, EstabelecimentoDetalhe>
{
    /// <inheritdoc />
    public async Task<EstabelecimentoDetalhe> Handle(
        ObterEstabelecimentoPorIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var estabelecimento = await estabelecimentos
            .ObterPorIdAsync(new EstabelecimentoId(request.EstabelecimentoId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Estabelecimento nao encontrado.");

        return new EstabelecimentoDetalhe(
            estabelecimento.Id.Value,
            estabelecimento.Cnes.Valor,
            estabelecimento.Nome,
            estabelecimento.Tipo.ToString(),
            estabelecimento.Endereco.Logradouro,
            estabelecimento.Endereco.Numero,
            estabelecimento.Endereco.Bairro,
            estabelecimento.Endereco.Municipio,
            estabelecimento.Endereco.Uf,
            estabelecimento.Endereco.Cep,
            estabelecimento.Situacao.ToString());
    }
}
