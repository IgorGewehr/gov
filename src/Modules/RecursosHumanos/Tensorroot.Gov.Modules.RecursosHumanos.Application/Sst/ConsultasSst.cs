using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

/// <summary>Lista os ASO (monitoracao biologica) de um servidor — ficha de saude ocupacional.</summary>
/// <param name="ServidorId">Servidor.</param>
public sealed record ListarExamesOcupacionaisQuery(Guid ServidorId) : IQuery<IReadOnlyList<ExameOcupacionalView>>;

/// <summary>Handler da listagem de ASO por servidor.</summary>
public sealed class ListarExamesOcupacionaisHandler(IExameOcupacionalRepository exames)
    : IQueryHandler<ListarExamesOcupacionaisQuery, IReadOnlyList<ExameOcupacionalView>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ExameOcupacionalView>> Handle(ListarExamesOcupacionaisQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await exames.ListarPorServidorAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(SstProjections.Projetar)];
    }
}

/// <summary>Lista as exposicoes a agentes nocivos de um servidor (registros ambientais do PPP).</summary>
/// <param name="ServidorId">Servidor.</param>
public sealed record ListarExposicoesQuery(Guid ServidorId) : IQuery<IReadOnlyList<ExposicaoAgenteNocivoView>>;

/// <summary>Handler da listagem de exposicoes por servidor.</summary>
public sealed class ListarExposicoesHandler(IExposicaoAgenteNocivoRepository exposicoes)
    : IQueryHandler<ListarExposicoesQuery, IReadOnlyList<ExposicaoAgenteNocivoView>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ExposicaoAgenteNocivoView>> Handle(ListarExposicoesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await exposicoes.ListarPorServidorAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(SstProjections.Projetar)];
    }
}

/// <summary>Lista as CAT de um servidor.</summary>
/// <param name="ServidorId">Servidor.</param>
public sealed record ListarComunicacoesAcidenteQuery(Guid ServidorId) : IQuery<IReadOnlyList<ComunicacaoAcidenteView>>;

/// <summary>Handler da listagem de CAT por servidor.</summary>
public sealed class ListarComunicacoesAcidenteHandler(IComunicacaoAcidenteRepository cats)
    : IQueryHandler<ListarComunicacoesAcidenteQuery, IReadOnlyList<ComunicacaoAcidenteView>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ComunicacaoAcidenteView>> Handle(ListarComunicacoesAcidenteQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await cats.ListarPorServidorAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(SstProjections.Projetar)];
    }
}

/// <summary>
/// Agenda do PCMSO (NR-07): ASO cujo proximo exame esta previsto ate a data de corte (vencidos/a vencer),
/// para controle de convocacao dos exames periodicos.
/// </summary>
/// <param name="AteData">Data de corte (proximo exame ate ela).</param>
public sealed record ObterAgendaPcmsoQuery(DateOnly AteData) : IQuery<IReadOnlyList<ExameOcupacionalView>>;

/// <summary>Handler da agenda do PCMSO.</summary>
public sealed class ObterAgendaPcmsoHandler(IExameOcupacionalRepository exames)
    : IQueryHandler<ObterAgendaPcmsoQuery, IReadOnlyList<ExameOcupacionalView>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ExameOcupacionalView>> Handle(ObterAgendaPcmsoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var itens = await exames.ListarComProximoExameAteAsync(request.AteData, cancellationToken).ConfigureAwait(false);
        return [.. itens.Select(SstProjections.Projetar)];
    }
}

/// <summary>
/// Monta o PPP (Perfil Profissiografico Previdenciario) consolidado de um servidor: dados do trabalhador
/// + registros ambientais (exposicoes vigentes/historicas) + monitoracao biologica (ASO). Marca exposicao
/// especial quando ha periodo com agente nocivo (alem do codigo de ausencia de agente).
/// </summary>
/// <param name="ServidorId">Servidor titular do perfil.</param>
public sealed record ObterPerfilProfissiograficoQuery(Guid ServidorId) : IQuery<PerfilProfissiograficoView>;

/// <summary>Handler do PPP.</summary>
public sealed class ObterPerfilProfissiograficoHandler(
    IServidorRepository servidores,
    IExposicaoAgenteNocivoRepository exposicoes,
    IExameOcupacionalRepository exames)
    : IQueryHandler<ObterPerfilProfissiograficoQuery, PerfilProfissiograficoView>
{
    /// <inheritdoc />
    public async Task<PerfilProfissiograficoView> Handle(ObterPerfilProfissiograficoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var servidorId = new ServidorId(request.ServidorId);
        var servidor = await servidores.ObterPorIdAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var exposicoesServidor = await exposicoes.ListarPorServidorAsync(servidorId, cancellationToken).ConfigureAwait(false);
        var examesServidor = await exames.ListarPorServidorAsync(servidorId, cancellationToken).ConfigureAwait(false);

        // Exposicao especial: ha periodo VIGENTE/historico (nao cancelado) com pelo menos um agente nocivo
        // que NAO seja o codigo de ausencia de agente — indicio de direito a aposentadoria especial.
        var possuiExposicaoEspecial = exposicoesServidor
            .Where(e => e.Situacao == SituacaoRegistroSst.Registrado)
            .SelectMany(e => e.Agentes)
            .Any(a => !a.EhAusenciaDeAgente);

        return new PerfilProfissiograficoView(
            request.ServidorId,
            servidor.DadosPessoais.Nome,
            servidor.Matricula.Valor,
            [.. exposicoesServidor.Select(SstProjections.Projetar)],
            [.. examesServidor.Select(SstProjections.Projetar)],
            possuiExposicaoEspecial);
    }
}

/// <summary>Projecoes compartilhadas dominio -> view de SST (sem I/O).</summary>
public static class SstProjections
{
    /// <summary>Projeta um ASO para a view de leitura.</summary>
    /// <param name="exame">ASO de dominio.</param>
    /// <returns>View do ASO.</returns>
    public static ExameOcupacionalView Projetar(ExameOcupacional exame)
    {
        ArgumentNullException.ThrowIfNull(exame);
        return new ExameOcupacionalView(
            exame.Id.Value,
            exame.ServidorId.Value,
            exame.Tipo,
            exame.DataExame,
            exame.Resultado,
            exame.Medico.Nome,
            $"{exame.Medico.NrConselho}/{exame.Medico.UfConselho}",
            exame.DataProximoExame,
            [.. exame.ExamesComplementares],
            exame.Situacao);
    }

    /// <summary>Projeta uma exposicao para a view de leitura.</summary>
    /// <param name="exposicao">Exposicao de dominio.</param>
    /// <returns>View da exposicao.</returns>
    public static ExposicaoAgenteNocivoView Projetar(ExposicaoAgenteNocivo exposicao)
    {
        ArgumentNullException.ThrowIfNull(exposicao);
        return new ExposicaoAgenteNocivoView(
            exposicao.Id.Value,
            exposicao.ServidorId.Value,
            exposicao.InicioExposicao,
            exposicao.FimExposicao,
            exposicao.SetorAtividade,
            [.. exposicao.Agentes.Select(a => new AgenteNocivoDto(a.Codigo, a.Descricao, a.Intensidade, a.UnidadeMedida, a.UtilizaEpc, a.UtilizaEpi))],
            exposicao.Situacao);
    }

    /// <summary>Projeta uma CAT para a view de leitura.</summary>
    /// <param name="cat">CAT de dominio.</param>
    /// <returns>View da CAT.</returns>
    public static ComunicacaoAcidenteView Projetar(ComunicacaoAcidente cat)
    {
        ArgumentNullException.ThrowIfNull(cat);
        return new ComunicacaoAcidenteView(
            cat.Id.Value,
            cat.ServidorId.Value,
            cat.TipoCat,
            cat.TipoAcidente,
            cat.DataHoraAcidente,
            cat.HouveObito,
            cat.DescricaoSituacao,
            cat.Cid,
            cat.Situacao);
    }
}
