using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Protocolo.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Protocolo.Application.Processos;

/// <summary>Projecao de detalhe de um processo para leitura (tenant-scoped; minimizada para LGPD).</summary>
/// <param name="Id">Identificador do processo.</param>
/// <param name="Nup">Numero Unico de Protocolo.</param>
/// <param name="Classificacao">Classe documental.</param>
/// <param name="NivelAcesso">Nivel de acesso do processo.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="DataAutuacao">Data da autuacao.</param>
/// <param name="SetorAtualId">Setor atualmente responsavel.</param>
/// <param name="OrigemModulo">Modulo originador (quando aplicavel).</param>
public sealed record ProcessoDetalhe(
    Guid Id,
    string Nup,
    string Classificacao,
    string NivelAcesso,
    string Situacao,
    DateOnly DataAutuacao,
    Guid? SetorAtualId,
    string? OrigemModulo);

/// <summary>Obtem o detalhe de um processo pelo NUP (tenant-scoped; sigiloso retorna <c>null</c> para nao autorizados — I-9).</summary>
/// <param name="Nup">Numero Unico de Protocolo a consultar.</param>
public sealed record ObterProcessoPorNupQuery(string Nup) : IQuery<ProcessoDetalhe?>;

/// <summary>Handler da consulta de processo por NUP.</summary>
public sealed class ObterProcessoPorNupHandler(IProcessoRepository processos)
    : IQueryHandler<ObterProcessoPorNupQuery, ProcessoDetalhe?>
{
    /// <inheritdoc />
    public async Task<ProcessoDetalhe?> Handle(ObterProcessoPorNupQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await processos.ObterPorNupAsync(new Nup(request.Nup), cancellationToken).ConfigureAwait(false);
        if (processo is null)
        {
            return null;
        }

        return Projetar(processo);
    }

    private static ProcessoDetalhe Projetar(Processo processo)
        => new(
            processo.Id.Value,
            processo.Nup.Valor,
            processo.Classificacao.Codigo,
            processo.NivelAcesso.ToString(),
            processo.Situacao.ToString(),
            processo.DataAutuacao,
            processo.SetorAtualId,
            processo.OrigemModulo);
}
