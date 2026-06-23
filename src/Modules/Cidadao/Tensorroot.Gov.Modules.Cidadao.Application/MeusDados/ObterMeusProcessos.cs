using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Cidadao.Application.Abstractions;
using Tensorroot.Gov.Modules.Protocolo.Contracts;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Cidadao.Application.MeusDados;

/// <summary>
/// MEUS PROCESSOS: lista os processos PUBLICOS de que o PROPRIO cidadao e o interessado. O documento e
/// resolvido server-side (ancora dado-proprio). Dado pessoal (LGPD): <see cref="ISensivelLgpd"/> + trilha.
/// </summary>
public sealed record ObterMeusProcessosQuery
    : IQuery<IReadOnlyList<MeuProcessoDto>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MeusProcessos";

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisPortalCidadao.Aplicaveis;
}

/// <summary>Handler de "meus processos" — delega ao Protocolo (Contracts) com a pessoa resolvida.</summary>
public sealed class ObterMeusProcessosHandler(
    IResolvedorPessoaDoCidadaoAutenticado resolvedor,
    IConsultaCidadaoEmEscopoDedicado consultaDedicada)
    : IQueryHandler<ObterMeusProcessosQuery, IReadOnlyList<MeuProcessoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MeuProcessoDto>> Handle(ObterMeusProcessosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pessoa = await resolvedor.ResolverPessoaAtualAsync(cancellationToken).ConfigureAwait(false);

        // Consulta ao Protocolo em ESCOPO DEDICADO (guarda H5); documento ja resolvido server-side.
        return await consultaDedicada
            .ExecutarAsync<IConsultaProcessoCidadao, IReadOnlyList<MeuProcessoDto>>(
                (consulta, ct) => consulta.ObterMeusProcessosAsync(pessoa.Documento, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// MEU PROCESSO (detalhe) por id. O <paramref name="ProcessoId"/> e revalidado server-side contra a
/// pessoa resolvida E contra o nivel de acesso (anti-IDOR): processo de outro interessado, ou
/// nao-publico, retorna <c>null</c> (404). Dado pessoal (LGPD): <see cref="ISensivelLgpd"/> + trilha.
/// </summary>
/// <param name="ProcessoId">Identificador do processo.</param>
public sealed record ObterMeuProcessoQuery(Guid ProcessoId)
    : IQuery<MeuProcessoDetalheDto?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => "MeuProcesso";

    /// <summary>Id do processo solicitado (revalidado contra a pessoa resolvida no handler).</summary>
    public string? EntidadeId => ProcessoId.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.ExercicioDeDireitos;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisPortalCidadao.Aplicaveis;
}

/// <summary>Handler do detalhe de "meu processo" — delega ao Protocolo (Contracts) com revalidacao.</summary>
public sealed class ObterMeuProcessoHandler(
    IResolvedorPessoaDoCidadaoAutenticado resolvedor,
    IConsultaCidadaoEmEscopoDedicado consultaDedicada)
    : IQueryHandler<ObterMeuProcessoQuery, MeuProcessoDetalheDto?>
{
    /// <inheritdoc />
    public async Task<MeuProcessoDetalheDto?> Handle(ObterMeuProcessoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pessoa = await resolvedor.ResolverPessoaAtualAsync(cancellationToken).ConfigureAwait(false);

        // Consulta ao Protocolo em ESCOPO DEDICADO (guarda H5). O Protocolo revalida titularidade + nivel
        // de acesso (anti-IDOR) — retorna null senao.
        return await consultaDedicada
            .ExecutarAsync<IConsultaProcessoCidadao, MeuProcessoDetalheDto?>(
                (consulta, ct) => consulta.ObterMeuProcessoAsync(pessoa.Documento, request.ProcessoId, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
