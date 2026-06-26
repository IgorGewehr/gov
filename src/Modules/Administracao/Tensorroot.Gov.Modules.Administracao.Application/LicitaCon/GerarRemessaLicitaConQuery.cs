using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.LicitaCon;

/// <summary>
/// Gera a remessa LicitaCon 1.4 (e-Validador TCE-RS) de uma licitacao — os 14 arquivos CSV. Saida que o
/// e-Validador do TCE-RS efetivamente valida (IN 13/2017; LicitaCon leiaute 1.4). Os codigos de dominio do
/// orgao/leiaute sao informados pela borda (parametros do tenant — sem numero magico). A TRANSMISSAO
/// efetiva ao TCE-RS (empacotamento e envio ao Processo Eletronico, com credencial) e // TODO(M10).
/// </summary>
/// <param name="LicitacaoId">Licitacao a remeter.</param>
/// <param name="CodigoOrgao">Codigo do orgao no TCE-RS (CD_ORGAO).</param>
/// <param name="NomeOrgao">Nome/razao social do orgao (NM_ORGAO).</param>
/// <param name="CodigoTipoModalidade">Codigo da modalidade no leiaute (CD_TIPO_MODALIDADE).</param>
/// <param name="TipoObjeto">Codigo do tipo de objeto (TP_OBJETO).</param>
/// <param name="CodigoTipoFaseAtual">Codigo da fase atual (CD_TIPO_FASE_ATUAL).</param>
/// <param name="TipoNivelJulgamento">Codigo do nivel de julgamento (TP_NIVEL_JULGAMENTO).</param>
/// <param name="NumeroProcesso">Numero do processo administrativo (NR_PROCESSO).</param>
/// <param name="AnoProcesso">Ano do processo (ANO_PROCESSO).</param>
/// <param name="NumeroLicitacao">Numero da licitacao no ente (NR_LICITACAO).</param>
/// <param name="AnoLicitacao">Ano da licitacao (ANO_LICITACAO).</param>
public sealed record GerarRemessaLicitaConQuery(
    Guid LicitacaoId,
    int CodigoOrgao,
    string NomeOrgao,
    int CodigoTipoModalidade,
    int TipoObjeto,
    int CodigoTipoFaseAtual,
    int TipoNivelJulgamento,
    string NumeroProcesso,
    int AnoProcesso,
    int NumeroLicitacao,
    int AnoLicitacao) : IQuery<RemessaLicitaCon>;

/// <summary>Regras de validacao da geracao da remessa LicitaCon.</summary>
public sealed class GerarRemessaLicitaConValidator : AbstractValidator<GerarRemessaLicitaConQuery>
{
    /// <summary>Define as regras.</summary>
    public GerarRemessaLicitaConValidator()
    {
        RuleFor(q => q.LicitacaoId).NotEmpty();
        RuleFor(q => q.CodigoOrgao).GreaterThan(0).WithMessage("Codigo do orgao no TCE-RS e obrigatorio.");
        RuleFor(q => q.NomeOrgao).NotEmpty().MaximumLength(200);
        RuleFor(q => q.CodigoTipoModalidade).GreaterThan(0).WithMessage("Codigo da modalidade (leiaute) e obrigatorio.");
        RuleFor(q => q.NumeroProcesso).NotEmpty();
        RuleFor(q => q.NumeroLicitacao).GreaterThan(0);
        RuleFor(q => q.AnoLicitacao).GreaterThan(0);
    }
}

/// <summary>Handler da geracao da remessa LicitaCon: carrega a licitacao e produz os 14 CSV.</summary>
public sealed class GerarRemessaLicitaConHandler(ILicitacaoRepository licitacoes)
    : IQueryHandler<GerarRemessaLicitaConQuery, RemessaLicitaCon>
{
    /// <inheritdoc />
    public async Task<RemessaLicitaCon> Handle(GerarRemessaLicitaConQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        var parametros = new RemessaLicitaConParametros(
            request.CodigoOrgao,
            request.NomeOrgao,
            request.CodigoTipoModalidade,
            request.TipoObjeto,
            request.CodigoTipoFaseAtual,
            request.TipoNivelJulgamento,
            request.NumeroProcesso,
            request.AnoProcesso,
            request.NumeroLicitacao,
            request.AnoLicitacao);

        return GeradorRemessaLicitaCon.Gerar(licitacao, parametros);
    }
}
