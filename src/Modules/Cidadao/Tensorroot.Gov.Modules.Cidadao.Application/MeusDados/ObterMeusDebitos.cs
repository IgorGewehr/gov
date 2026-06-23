using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;

/// <summary>
/// MEUS DEBITOS: lista os lancamentos tributarios EM ABERTO do PROPRIO cidadao autenticado. O documento
/// e SEMPRE resolvido server-side (ancora dado-proprio) — o cliente nada informa. Dado pessoal (LGPD):
/// implementa <see cref="ISensivelLgpd"/> e gera trilha de acesso.
/// </summary>
public sealed record ObterMeusDebitosQuery
    : IQuery<IReadOnlyList<MeuLancamentoDto>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MeusDebitos";

    /// <summary>Resolvido server-side do principal (nao vem do cliente) — por isso nulo aqui.</summary>
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisPortalCidadao.Aplicaveis;
}

/// <summary>Handler de "meus debitos" — delega ao Tributos (Contracts) com a pessoa resolvida.</summary>
public sealed class ObterMeusDebitosHandler(
    IResolvedorPessoaDoCidadaoAutenticado resolvedor,
    IConsultaCidadaoEmEscopoDedicado consultaDedicada)
    : IQueryHandler<ObterMeusDebitosQuery, IReadOnlyList<MeuLancamentoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MeuLancamentoDto>> Handle(ObterMeusDebitosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ANCORA: pessoa do PROPRIO cidadao autenticado — nunca um documento/id do cliente. Resolvida no
        // escopo da requisicao (CidadaoDbContext), onde a trilha LGPD tambem e selada.
        var pessoa = await resolvedor.ResolverPessoaAtualAsync(cancellationToken).ConfigureAwait(false);

        // A consulta ao Tributos roda em ESCOPO DEDICADO (guarda H5): o documento JA resolvido e o unico
        // dado que cruza — o isolamento dado-proprio nao e afetado.
        return await consultaDedicada
            .ExecutarAsync<IConsultaTributariaCidadao, IReadOnlyList<MeuLancamentoDto>>(
                (consulta, ct) => consulta.ObterMeusLancamentosEmAbertoAsync(pessoa.Documento, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
