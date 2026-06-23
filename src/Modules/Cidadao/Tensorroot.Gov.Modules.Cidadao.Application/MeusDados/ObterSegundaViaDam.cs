using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Contracts;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;

/// <summary>
/// 2a VIA DE DAM/boleto: obtem um DAM do PROPRIO cidadao por id. O <paramref name="DamId"/> e o UNICO
/// dado vindo do cliente e e REVALIDADO server-side contra a pessoa resolvida (anti-IDOR): um DAM de
/// outro contribuinte retorna <c>null</c> (404), indistinguivel de inexistente. Dado pessoal (LGPD):
/// <see cref="ISensivelLgpd"/> + trilha de acesso (o EntidadeId e o DAM solicitado).
/// </summary>
/// <param name="DamId">Identificador do DAM cuja 2a via se deseja.</param>
public sealed record ObterSegundaViaDamQuery(Guid DamId)
    : IQuery<MeuDamDto?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "SegundaViaDam";

    /// <summary>Id do DAM solicitado (revalidado contra a pessoa resolvida no handler).</summary>
    public string? EntidadeId => DamId.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ObrigacaoLegal;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisPortalCidadao.Aplicaveis;
}

/// <summary>Handler da 2a via de DAM — delega ao Tributos (Contracts) com revalidacao de titularidade.</summary>
public sealed class ObterSegundaViaDamHandler(
    IResolvedorPessoaDoCidadaoAutenticado resolvedor,
    IConsultaCidadaoEmEscopoDedicado consultaDedicada)
    : IQueryHandler<ObterSegundaViaDamQuery, MeuDamDto?>
{
    /// <inheritdoc />
    public async Task<MeuDamDto?> Handle(ObterSegundaViaDamQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pessoa = await resolvedor.ResolverPessoaAtualAsync(cancellationToken).ConfigureAwait(false);

        // Consulta ao Tributos em ESCOPO DEDICADO (guarda H5). O Tributos revalida que o DAM pertence ao
        // documento resolvido (anti-IDOR) — retorna null senao.
        return await consultaDedicada
            .ExecutarAsync<IConsultaTributariaCidadao, MeuDamDto?>(
                (consulta, ct) => consulta.ObterMeuDamAsync(pessoa.Documento, request.DamId, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
