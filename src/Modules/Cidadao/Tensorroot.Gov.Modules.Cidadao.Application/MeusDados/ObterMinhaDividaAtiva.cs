using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;

/// <summary>
/// MINHA DIVIDA ATIVA: posicao consolidada da Divida Ativa do PROPRIO cidadao (valor originario +
/// encargos atualizados na data-base). O documento e resolvido server-side (ancora dado-proprio). Dado
/// pessoal (LGPD): <see cref="ISensivelLgpd"/> + trilha de acesso.
/// </summary>
/// <param name="DataBase">Data-base para apurar encargos (default: hoje, definido no handler).</param>
public sealed record ObterMinhaDividaAtivaQuery(DateOnly? DataBase = null)
    : IQuery<IReadOnlyList<MinhaDividaAtivaDto>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MinhaDividaAtiva";

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisPortalCidadao.Aplicaveis;
}

/// <summary>Handler de "minha divida ativa" — delega ao Tributos (Contracts) com a pessoa resolvida.</summary>
public sealed class ObterMinhaDividaAtivaHandler(
    IResolvedorPessoaDoCidadaoAutenticado resolvedor,
    IConsultaCidadaoEmEscopoDedicado consultaDedicada,
    IDataHojeTenant dataHoje)
    : IQueryHandler<ObterMinhaDividaAtivaQuery, IReadOnlyList<MinhaDividaAtivaDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MinhaDividaAtivaDto>> Handle(ObterMinhaDividaAtivaQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pessoa = await resolvedor.ResolverPessoaAtualAsync(cancellationToken).ConfigureAwait(false);
        // Prescricao (CTN art. 174) corre por dia civil: a data-base default e' HOJE no FUSO do tenant
        // (UTC-3), nao o UTC — perto da meia-noite o UTC ja virou o dia seguinte e anteciparia a prescricao.
        var dataBase = request.DataBase ?? dataHoje.Hoje();

        // Consulta ao Tributos em ESCOPO DEDICADO (guarda H5); documento ja resolvido server-side.
        return await consultaDedicada
            .ExecutarAsync<IConsultaTributariaCidadao, IReadOnlyList<MinhaDividaAtivaDto>>(
                (consulta, ct) => consulta.ObterMinhaDividaAtivaAsync(pessoa.Documento, dataBase, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
