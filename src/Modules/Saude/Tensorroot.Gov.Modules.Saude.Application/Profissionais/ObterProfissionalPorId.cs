using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using ProfissionalId = Tensorroot.Gov.Modules.Saude.Domain.Atendimento.ProfissionalId;

namespace Tensorroot.Gov.Modules.Saude.Application.Profissionais;

/// <summary>Obtem a ficha completa de um profissional (com vinculos) por identificador. Tenant-scoped; read-only.</summary>
/// <param name="ProfissionalId">Identificador do profissional.</param>
public sealed record ObterProfissionalPorIdQuery(Guid ProfissionalId) : IQuery<ProfissionalDetalhe>;

/// <summary>Handler da obtencao de profissional por id.</summary>
public sealed class ObterProfissionalPorIdHandler(IProfissionalCadastroRepository profissionais)
    : IQueryHandler<ObterProfissionalPorIdQuery, ProfissionalDetalhe>
{
    /// <inheritdoc />
    public async Task<ProfissionalDetalhe> Handle(
        ObterProfissionalPorIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var profissional = await profissionais
            .ObterPorIdAsync(new ProfissionalId(request.ProfissionalId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Profissional nao encontrado.");

        var vinculos = profissional.Vinculos
            .Select(vinculo => new VinculoCnesDto(
                vinculo.EstabelecimentoId.Value,
                vinculo.Cbo.Valor,
                vinculo.DataInicio,
                vinculo.DataFim))
            .ToList();

        return new ProfissionalDetalhe(
            profissional.Id.Value,
            profissional.Nome,
            profissional.Cpf.Formatar(),
            profissional.Cns,
            profissional.Registro is { } registro ? registro.ToString() : null,
            profissional.TemCrmAtivo,
            profissional.Situacao.ToString(),
            vinculos);
    }
}
