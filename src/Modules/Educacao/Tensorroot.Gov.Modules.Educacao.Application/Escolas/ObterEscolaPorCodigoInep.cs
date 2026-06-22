using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Application.Escolas;

/// <summary>Projecao de resumo de uma escola para leitura.</summary>
/// <param name="Id">Identificador da escola.</param>
/// <param name="CodigoInep">Codigo INEP unico nacional.</param>
/// <param name="Nome">Nome da unidade escolar.</param>
/// <param name="Dependencia">Dependencia administrativa (texto).</param>
/// <param name="Situacao">Situacao atual (texto).</param>
public sealed record EscolaResumo(
    Guid Id,
    string CodigoInep,
    string Nome,
    string Dependencia,
    string Situacao);

/// <summary>Obtem o resumo de uma escola por codigo INEP (tenant-scoped).</summary>
/// <param name="CodigoInep">Codigo INEP a buscar.</param>
public sealed record ObterEscolaPorCodigoInepQuery(string CodigoInep) : IQuery<EscolaResumo?>;

/// <summary>Handler da consulta de escola por codigo INEP.</summary>
public sealed class ObterEscolaPorCodigoInepHandler(IEscolaRepository escolas)
    : IQueryHandler<ObterEscolaPorCodigoInepQuery, EscolaResumo?>
{
    /// <inheritdoc />
    public async Task<EscolaResumo?> Handle(ObterEscolaPorCodigoInepQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var escola = await escolas
            .ObterPorCodigoInepAsync(CodigoInep.Criar(request.CodigoInep), cancellationToken)
            .ConfigureAwait(false);
        if (escola is null)
        {
            return null;
        }

        return new EscolaResumo(
            escola.Id.Value,
            escola.CodigoInep.Valor,
            escola.Nome,
            escola.DependenciaAdministrativa.ToString(),
            escola.Situacao.ToString());
    }
}
