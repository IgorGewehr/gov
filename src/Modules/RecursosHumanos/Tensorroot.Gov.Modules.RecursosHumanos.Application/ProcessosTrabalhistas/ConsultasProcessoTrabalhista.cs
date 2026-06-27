using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Common;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ProcessosTrabalhistas;

/// <summary>Resumo de um processo trabalhista (linha de lista).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="NumeroProcesso">Numero do processo (CNJ).</param>
/// <param name="Vara">Vara/orgao julgador.</param>
/// <param name="Reclamante">Reclamante.</param>
/// <param name="ValorCausa">Valor da causa.</param>
/// <param name="ValorProvisionado">Valor provisionado vigente.</param>
/// <param name="Prognostico">Prognostico de perda.</param>
/// <param name="Situacao">Situacao processual.</param>
/// <param name="DataAjuizamento">Data de ajuizamento.</param>
public sealed record ProcessoTrabalhistaResumo(
    Guid Id,
    string NumeroProcesso,
    string Vara,
    string Reclamante,
    decimal ValorCausa,
    decimal ValorProvisionado,
    string Prognostico,
    string Situacao,
    DateOnly DataAjuizamento);

/// <summary>Detalhe completo de um processo trabalhista.</summary>
/// <param name="Resumo">Resumo do processo.</param>
/// <param name="ServidorId">Servidor vinculado (opcional).</param>
/// <param name="Objeto">Objeto/pedidos.</param>
/// <param name="ValorAcordo">Valor do acordo (quando houver).</param>
/// <param name="ValorCondenacao">Valor da condenacao (quando houver).</param>
/// <param name="DataEncerramento">Data de encerramento (quando houver).</param>
public sealed record ProcessoTrabalhistaDetalhe(
    ProcessoTrabalhistaResumo Resumo,
    Guid? ServidorId,
    string Objeto,
    decimal? ValorAcordo,
    decimal? ValorCondenacao,
    DateOnly? DataEncerramento);

/// <summary>Demonstrativo de provisoes trabalhistas (NBC TG 25) — total provisionado vigente.</summary>
/// <param name="TotalProvisionado">Soma das provisoes vigentes (perda provavel, processos em andamento).</param>
public sealed record DemonstrativoProvisaoTrabalhista(decimal TotalProvisionado);

/// <summary>Projecoes do dominio de processo trabalhista para os DTOs.</summary>
internal static class ProjetarProcesso
{
    internal static ProcessoTrabalhistaResumo ParaResumo(ProcessoTrabalhista processo) => new(
        processo.Id.Value,
        processo.NumeroProcesso,
        processo.Vara,
        processo.Reclamante,
        processo.ValorCausa,
        processo.ValorProvisionado,
        processo.Prognostico.ToString(),
        processo.Situacao.ToString(),
        processo.DataAjuizamento);

    internal static ProcessoTrabalhistaDetalhe ParaDetalhe(ProcessoTrabalhista processo) => new(
        ParaResumo(processo),
        processo.ServidorId?.Value,
        processo.Objeto,
        processo.ValorAcordo,
        processo.ValorCondenacao,
        processo.DataEncerramento);
}

/// <summary>Busca paginada de processos trabalhistas por situacao/prognostico/termo (navegabilidade); read-only.</summary>
/// <param name="Situacao">Filtro opcional por situacao.</param>
/// <param name="Prognostico">Filtro opcional por prognostico.</param>
/// <param name="Termo">Filtro opcional por numero/reclamante.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarProcessosTrabalhistasQuery(
    SituacaoProcessoTrabalhista? Situacao,
    PrognosticoPerda? Prognostico,
    string? Termo,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<ProcessoTrabalhistaResumo>>;

/// <summary>Handler da busca paginada de processos.</summary>
public sealed class BuscarProcessosTrabalhistasHandler(IProcessoTrabalhistaRepository processos)
    : IQueryHandler<BuscarProcessosTrabalhistasQuery, ResultadoPaginado<ProcessoTrabalhistaResumo>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<ProcessoTrabalhistaResumo>> Handle(BuscarProcessosTrabalhistasQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await processos
            .BuscarAsync(request.Situacao, request.Prognostico, request.Termo, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens.Select(ProjetarProcesso.ParaResumo).ToList();
        return new ResultadoPaginado<ProcessoTrabalhistaResumo>(projetados, total, pagina, tamanho);
    }
}

/// <summary>Obtem um processo trabalhista por identificador (detalhe completo); read-only.</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
public sealed record ObterProcessoTrabalhistaQuery(Guid ProcessoId) : IQuery<ProcessoTrabalhistaDetalhe>;

/// <summary>Handler do detalhe de processo.</summary>
public sealed class ObterProcessoTrabalhistaHandler(IProcessoTrabalhistaRepository processos)
    : IQueryHandler<ObterProcessoTrabalhistaQuery, ProcessoTrabalhistaDetalhe>
{
    /// <inheritdoc />
    public async Task<ProcessoTrabalhistaDetalhe> Handle(ObterProcessoTrabalhistaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await processos.ObterPorIdAsync(new ProcessoTrabalhistaId(request.ProcessoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo trabalhista nao encontrado.");
        return ProjetarProcesso.ParaDetalhe(processo);
    }
}

/// <summary>Demonstrativo de provisoes trabalhistas vigentes (passivo NBC TG 25); read-only.</summary>
public sealed record ObterDemonstrativoProvisaoQuery : IQuery<DemonstrativoProvisaoTrabalhista>;

/// <summary>Handler do demonstrativo de provisao.</summary>
public sealed class ObterDemonstrativoProvisaoHandler(IProcessoTrabalhistaRepository processos)
    : IQueryHandler<ObterDemonstrativoProvisaoQuery, DemonstrativoProvisaoTrabalhista>
{
    /// <inheritdoc />
    public async Task<DemonstrativoProvisaoTrabalhista> Handle(ObterDemonstrativoProvisaoQuery request, CancellationToken cancellationToken)
    {
        var total = await processos.SomarProvisaoVigenteAsync(cancellationToken).ConfigureAwait(false);
        return new DemonstrativoProvisaoTrabalhista(total);
    }
}
