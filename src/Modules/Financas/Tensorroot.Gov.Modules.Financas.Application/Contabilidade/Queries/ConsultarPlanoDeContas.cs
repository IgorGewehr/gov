using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Queries;

/// <summary>Conta do plano para leitura.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Codigo">Código PCASP.</param>
/// <param name="Titulo">Título.</param>
/// <param name="NaturezaInformacao">Natureza da informação.</param>
/// <param name="NaturezaSaldo">Natureza do saldo.</param>
/// <param name="Tipo">Tipo (Sintética/Analítica).</param>
/// <param name="Nivel">Nível hierárquico.</param>
/// <param name="Ativa">Se está ativa.</param>
public sealed record ContaContabilDto(
    Guid Id,
    string Codigo,
    string Titulo,
    string NaturezaInformacao,
    string NaturezaSaldo,
    string Tipo,
    int Nivel,
    bool Ativa);

/// <summary>Lista todo o plano de contas do tenant.</summary>
public sealed record ListarPlanoDeContasQuery : IQuery<IReadOnlyList<ContaContabilDto>>;

/// <summary>Handler da listagem do plano de contas.</summary>
public sealed class ListarPlanoDeContasHandler(IContaContabilRepository contas)
    : IQueryHandler<ListarPlanoDeContasQuery, IReadOnlyList<ContaContabilDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ContaContabilDto>> Handle(ListarPlanoDeContasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var todas = await contas.ListarTodasAsync(cancellationToken).ConfigureAwait(false);
        return todas
            .OrderBy(c => c.Codigo.Codigo, StringComparer.Ordinal)
            .Select(Mapear)
            .ToList();
    }

    private static ContaContabilDto Mapear(ContaContabil c) => new(
        c.Id.Value,
        c.Codigo.Codigo,
        c.Titulo,
        c.NaturezaInformacao.ToString(),
        c.NaturezaSaldo.ToString(),
        c.Tipo.ToString(),
        c.Nivel,
        c.Ativa);
}
