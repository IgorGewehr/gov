using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.Modules.Protocolo.Domain.Processos;
using Tensorroot.Gov.Modules.Protocolo.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Protocolo.Infrastructure.PortalCidadao;

/// <summary>
/// Implementacao da porta de leitura cidada do Protocolo (<see cref="IConsultaProcessoCidadao"/>).
/// Filtra os processos pelo <c>InteressadoDocumento</c> recebido (CPF/CNPJ ja resolvido server-side do
/// principal pelo modulo Cidadao) DENTRO do tenant (Global Query Filter) e SOMENTE quando o processo e
/// PUBLICO (<see cref="NivelDeAcesso.Publico"/>) — processos restritos/sigilosos nao vao ao portal mesmo
/// ao proprio interessado (decisao de produto). A consulta por id revalida titularidade + nivel de acesso
/// (anti-IDOR): processo de outro interessado, ou nao-publico, retorna <c>null</c> (indistinguivel).
/// </summary>
public sealed class ConsultaProcessoCidadao(ProtocoloDbContext context) : IConsultaProcessoCidadao
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MeuProcessoDto>> ObterMeusProcessosAsync(
        string documento,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return [];
        }

        var processos = await context.Processos
            .AsNoTracking()
            .Where(processo => processo.InteressadoDocumento == documento
                && processo.NivelAcesso == NivelDeAcesso.Publico)
            .OrderByDescending(processo => processo.DataAutuacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return processos
            .Select(processo => new MeuProcessoDto(
                processo.Id.Value,
                processo.Nup.Valor,
                processo.Classificacao.Codigo,
                processo.Situacao.ToString(),
                processo.DataAutuacao))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MeuProcessoDetalheDto?> ObterMeuProcessoAsync(
        string documento,
        Guid processoId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return null;
        }

        var processo = await context.Processos
            .AsNoTracking()
            .Include(p => p.Movimentacoes)
            .FirstOrDefaultAsync(p => p.Id == new ProcessoId(processoId), cancellationToken)
            .ConfigureAwait(false);

        // REVALIDACAO (anti-IDOR): o processo tem de ser do interessado do documento E publico. Caso
        // contrario, null — indistinguivel de "nao existe", sem vazar a existencia de processo de terceiro
        // nem de processo sigiloso do proprio interessado.
        if (processo is null
            || processo.InteressadoDocumento != documento
            || processo.NivelAcesso != NivelDeAcesso.Publico)
        {
            return null;
        }

        var movimentacoes = processo.Movimentacoes
            .OrderBy(movimentacao => movimentacao.DataMovimentacao)
            .Select(movimentacao => new MinhaMovimentacaoDto(movimentacao.DataMovimentacao, movimentacao.Observacao))
            .ToList();

        return new MeuProcessoDetalheDto(
            processo.Id.Value,
            processo.Nup.Valor,
            processo.Classificacao.Codigo,
            processo.Situacao.ToString(),
            processo.DataAutuacao,
            processo.Prazo.Fim,
            movimentacoes);
    }
}
