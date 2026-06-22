using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Resumo de um processo do setor para leitura (tenant-scoped; minimizado para LGPD).</summary>
/// <param name="Id">Identificador do processo.</param>
/// <param name="Nup">Numero Unico de Protocolo.</param>
/// <param name="Classificacao">Classe documental.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataAutuacao">Data da autuacao.</param>
public sealed record ProcessoResumo(
    Guid Id,
    string Nup,
    string Classificacao,
    string Situacao,
    DateOnly DataAutuacao);

/// <summary>Lista os processos cujo setor atual e o informado (tenant-scoped; sigilosos omitidos para nao autorizados — I-9).</summary>
/// <param name="SetorId">Setor responsavel.</param>
public sealed record ListarProcessosDoSetorQuery(Guid SetorId) : IQuery<IReadOnlyList<ProcessoResumo>>;

/// <summary>Handler da listagem de processos do setor.</summary>
public sealed class ListarProcessosDoSetorHandler(IProcessoRepository processos)
    : IQueryHandler<ListarProcessosDoSetorQuery, IReadOnlyList<ProcessoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProcessoResumo>> Handle(
        ListarProcessosDoSetorQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resultado = await processos.ListarPorSetorAtualAsync(request.SetorId, cancellationToken).ConfigureAwait(false);

        return resultado
            .Select(processo => new ProcessoResumo(
                processo.Id.Value,
                processo.Nup.Valor,
                processo.Classificacao.Codigo,
                processo.Situacao.ToString(),
                processo.DataAutuacao))
            .ToList();
    }
}
