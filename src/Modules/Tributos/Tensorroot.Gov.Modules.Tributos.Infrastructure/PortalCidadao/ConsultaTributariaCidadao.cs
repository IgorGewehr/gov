using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Tributos.Infrastructure.PortalCidadao;

/// <summary>
/// Implementacao da porta de leitura cidada do Tributos (<see cref="IConsultaTributariaCidadao"/>).
/// Resolve o <c>Contribuinte</c> SEMPRE pelo DOCUMENTO recebido (CPF/CNPJ ja resolvido server-side do
/// principal pelo modulo Cidadao) DENTRO do tenant (Global Query Filter do <see cref="TributosDbContext"/>)
/// e filtra estritamente o dado-proprio. O cliente jamais informa um id de contribuinte/lancamento.
/// <para>
/// SEGURANCA: a consulta de 2a via por <c>damId</c> revalida a titularidade (o DAM tem de pertencer ao
/// contribuinte do documento) — anti-IDOR; um id de outro contribuinte retorna <c>null</c>.
/// </para>
/// </summary>
public sealed class ConsultaTributariaCidadao(TributosDbContext context) : IConsultaTributariaCidadao
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MeuLancamentoDto>> ObterMeusLancamentosEmAbertoAsync(
        string documento,
        CancellationToken cancellationToken)
    {
        var contribuinteId = await ResolverContribuinteIdAsync(documento, cancellationToken).ConfigureAwait(false);
        if (contribuinteId is null)
        {
            return [];
        }

        var lancamentos = await context.Lancamentos
            .AsNoTracking()
            .Where(lancamento => lancamento.ContribuinteId == contribuinteId.Value
                && lancamento.Situacao == SituacaoLancamento.Aberto)
            .OrderBy(lancamento => lancamento.Vencimento)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return lancamentos
            .Select(lancamento => new MeuLancamentoDto(
                lancamento.Id.Value,
                lancamento.TipoTributo.ToString(),
                lancamento.Competencia.ToString(),
                lancamento.Vencimento,
                lancamento.ValorPrincipal.Valor,
                lancamento.Situacao.ToString()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MinhaDividaAtivaDto>> ObterMinhaDividaAtivaAsync(
        string documento,
        DateOnly dataBase,
        CancellationToken cancellationToken)
    {
        var contribuinteId = await ResolverContribuinteIdAsync(documento, cancellationToken).ConfigureAwait(false);
        if (contribuinteId is null)
        {
            return [];
        }

        var dividas = await context.DividasAtivas
            .AsNoTracking()
            .Where(divida => divida.ContribuinteId == contribuinteId.Value)
            .OrderBy(divida => divida.DataInscricao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return dividas
            .Select(divida => new MinhaDividaAtivaDto(
                divida.Id.Value,
                divida.TipoTributo.ToString(),
                divida.NumeroInscricao,
                divida.NumeroCda,
                divida.ValorOriginario.Valor,
                // Encargos apurados deterministicamente pela regra do dominio na data-base informada.
                divida.ApurarEncargos(dataBase).ValorAtualizado.Valor,
                divida.Situacao.ToString(),
                divida.Situacao == SituacaoDividaAtiva.Parcelada,
                divida.DataInscricao))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<MeuDamDto?> ObterMeuDamAsync(
        string documento,
        Guid damId,
        CancellationToken cancellationToken)
    {
        var contribuinteId = await ResolverContribuinteIdAsync(documento, cancellationToken).ConfigureAwait(false);
        if (contribuinteId is null)
        {
            return null;
        }

        var dam = await context.Dams
            .AsNoTracking()
            .Include(d => d.Parcelas)
            .FirstOrDefaultAsync(d => d.Id == new DamId(damId), cancellationToken)
            .ConfigureAwait(false);

        // REVALIDACAO DE TITULARIDADE (anti-IDOR): o DAM tem de ser do contribuinte do documento. Caso
        // contrario, retorna null — indistinguivel de "nao existe", sem vazar a existencia de terceiros.
        if (dam is null || dam.ContribuinteId != contribuinteId.Value)
        {
            return null;
        }

        var parcelas = dam.Parcelas
            .OrderBy(parcela => parcela.Numero)
            .Select(parcela => new MinhaParcelaDamDto(parcela.Numero, parcela.Valor.Valor, parcela.Vencimento, parcela.Paga))
            .ToList();

        return new MeuDamDto(dam.Id.Value, dam.LancamentoId.Value, dam.ValorTotal.Valor, dam.Quitado, parcelas);
    }

    /// <summary>
    /// Resolve o <see cref="ContribuinteId"/> do tenant atual a partir do DOCUMENTO (somente digitos).
    /// O Global Query Filter garante que so se enxergue o contribuinte do tenant do principal — o cidadao
    /// de um municipio nunca alcanca o contribuinte de outro. Retorna <c>null</c> quando nao ha
    /// contribuinte com o documento neste tenant (sem vinculo => sem dado-proprio).
    /// </summary>
    private async Task<ContribuinteId?> ResolverContribuinteIdAsync(string documento, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return null;
        }

        var contribuinte = await context.Contribuintes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Documento == documento, cancellationToken)
            .ConfigureAwait(false);

        return contribuinte?.Id;
    }
}
