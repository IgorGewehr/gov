using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Rubricas;

/// <summary>Projecao de leitura de uma rubrica parametrizada.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Codigo">Codigo (S-1010).</param>
/// <param name="Descricao">Descricao.</param>
/// <param name="Natureza">Natureza (provento/desconto/informativa).</param>
/// <param name="IncideInss">Integra base de INSS.</param>
/// <param name="IncideRpps">Integra base de RPPS.</param>
/// <param name="IncideIrrf">Integra base do IRRF.</param>
/// <param name="IncideFgts">Integra base do FGTS.</param>
public sealed record RubricaDto(
    Guid Id,
    string Codigo,
    string Descricao,
    string Natureza,
    bool IncideInss,
    bool IncideRpps,
    bool IncideIrrf,
    bool IncideFgts);

/// <summary>Lista as rubricas vigentes na competencia, no tenant atual.</summary>
/// <param name="Ano">Ano da competencia.</param>
/// <param name="Mes">Mes da competencia (1 a 12).</param>
public sealed record ListarRubricasQuery(int Ano, int Mes) : IQuery<IReadOnlyList<RubricaDto>>;

/// <summary>Handler da listagem de rubricas vigentes.</summary>
public sealed class ListarRubricasHandler(IRubricaFolhaRepository rubricas)
    : IQueryHandler<ListarRubricasQuery, IReadOnlyList<RubricaDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<RubricaDto>> Handle(ListarRubricasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var vigentes = await rubricas
            .ListarVigentesAsync(Competencia.De(request.Ano, request.Mes), cancellationToken)
            .ConfigureAwait(false);

        return vigentes
            .Select(r => new RubricaDto(
                r.Id.Value,
                r.Codigo.Codigo,
                r.Descricao,
                r.Natureza.ToString(),
                r.IncideInss,
                r.IncideRpps,
                r.IncideIrrf,
                r.IncideFgts))
            .ToList();
    }
}
