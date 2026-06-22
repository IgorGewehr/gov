using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce;

namespace Tensorroot.Gov.Modules.Transparencia.Application.RemessasTce;

/// <summary>Uma crítica (erro/aviso) do relatório de pré-validação local (RDI).</summary>
/// <param name="Arquivo">Arquivo apontado.</param>
/// <param name="Linha">Linha do registro (0 se não aplicável).</param>
/// <param name="Severidade">Severidade (Erro bloqueia; Aviso não).</param>
/// <param name="Mensagem">Descrição da crítica.</param>
public sealed record CriticaRemessa(string Arquivo, int Linha, string Severidade, string Mensagem);

/// <summary>Relatório de críticas da pré-validação local de uma remessa.</summary>
/// <param name="RemessaTceId">Identificador da remessa.</param>
/// <param name="Situacao">Situação atual da remessa.</param>
/// <param name="PossuiErro">Indica se há ao menos uma crítica de erro (bloqueia).</param>
/// <param name="QuantidadeErros">Quantidade de erros.</param>
/// <param name="QuantidadeAvisos">Quantidade de avisos.</param>
/// <param name="Criticas">Lista das críticas apuradas.</param>
public sealed record RelatorioCriticasRemessa(
    Guid RemessaTceId,
    string Situacao,
    bool PossuiErro,
    int QuantidadeErros,
    int QuantidadeAvisos,
    IReadOnlyList<CriticaRemessa> Criticas);

/// <summary>Consulta o relatório de críticas (RDI) da pré-validação local de uma remessa.</summary>
/// <param name="RemessaTceId">Remessa a consultar.</param>
public sealed record ObterCriticasRemessaTceQuery(Guid RemessaTceId) : IQuery<RelatorioCriticasRemessa?>;

/// <summary>Handler da consulta de críticas da remessa.</summary>
public sealed class ObterCriticasRemessaTceHandler(IRemessaTceRepository remessas)
    : IQueryHandler<ObterCriticasRemessaTceQuery, RelatorioCriticasRemessa?>
{
    /// <inheritdoc />
    public async Task<RelatorioCriticasRemessa?> Handle(
        ObterCriticasRemessaTceQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas
            .ObterPorIdAsync(new RemessaTceId(request.RemessaTceId), cancellationToken)
            .ConfigureAwait(false);
        if (remessa is null)
        {
            return null;
        }

        var rdi = remessa.ResultadoValidacao;
        var criticas = rdi is null
            ? []
            : rdi.Ocorrencias
                .Select(ocorrencia => new CriticaRemessa(
                    ocorrencia.Arquivo,
                    ocorrencia.Linha,
                    ocorrencia.Severidade.ToString(),
                    ocorrencia.Mensagem))
                .ToList();

        return new RelatorioCriticasRemessa(
            remessa.Id.Value,
            remessa.Situacao.ToString(),
            rdi?.PossuiErro ?? false,
            rdi?.QuantidadeErros ?? 0,
            rdi?.QuantidadeAvisos ?? 0,
            criticas);
    }
}
