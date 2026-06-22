using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Application.Abstractions;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;

/// <summary>Projecao de leitura de uma familia para listagem por territorio (NIS mascarado).</summary>
/// <param name="Id">Identificador da familia.</param>
/// <param name="NisMascarado">NIS do responsavel familiar, mascarado (minimizacao — LGPD).</param>
/// <param name="UnidadeAtendimentoId">CRAS de referencia.</param>
/// <param name="Territorio">Territorio de cobertura.</param>
/// <param name="RendaPerCapita">Renda per capita calculada.</param>
/// <param name="Situacao">Situacao atual da familia.</param>
/// <param name="DataReferenciamento">Data do referenciamento.</param>
/// <param name="DataUltimaAtualizacaoCadastral">Marco da ultima atualizacao do CadUnico.</param>
public sealed record FamiliaResumo(
    Guid Id,
    string NisMascarado,
    Guid UnidadeAtendimentoId,
    string Territorio,
    decimal RendaPerCapita,
    string Situacao,
    DateOnly DataReferenciamento,
    DateOnly DataUltimaAtualizacaoCadastral);

/// <summary>Lista as familias de um territorio (sempre tenant-scoped via Global Query Filter).</summary>
/// <param name="Territorio">Territorio de cobertura a consultar.</param>
public sealed record ObterFamiliasDoTerritorioQuery(string Territorio) : IQuery<IReadOnlyList<FamiliaResumo>>;

/// <summary>Handler da listagem de familias por territorio.</summary>
public sealed class ObterFamiliasDoTerritorioHandler(IFamiliaRepository familias)
    : IQueryHandler<ObterFamiliasDoTerritorioQuery, IReadOnlyList<FamiliaResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<FamiliaResumo>> Handle(ObterFamiliasDoTerritorioQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var encontradas = await familias.ListarPorTerritorioAsync(request.Territorio, cancellationToken).ConfigureAwait(false);

        return encontradas
            .Select(familia => new FamiliaResumo(
                familia.Id.Value,
                familia.Nis.Mascarado,
                familia.UnidadeAtendimentoId,
                familia.Territorio,
                familia.RendaPerCapita.Valor,
                familia.Situacao.ToString(),
                familia.DataReferenciamento,
                familia.DataUltimaAtualizacaoCadastral))
            .ToList();
    }
}
