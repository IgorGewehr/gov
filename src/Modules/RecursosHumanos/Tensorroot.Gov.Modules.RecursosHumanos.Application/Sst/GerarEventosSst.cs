using System.Globalization;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

/// <summary>
/// Gera o S-2210 (CAT) a partir de uma <see cref="ComunicacaoAcidente"/>. Idempotente por (tipo, catId).
/// </summary>
/// <param name="ComunicacaoAcidenteId">CAT de origem do evento.</param>
public sealed record GerarS2210Command(Guid ComunicacaoAcidenteId) : ICommand<Guid>;

/// <summary>Handler do S-2210.</summary>
public sealed class GerarS2210Handler(
    IEmpregadorESocialProvider empregador,
    IServidorRepository servidores,
    IComunicacaoAcidenteRepository cats,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS2210Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS2210Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var cat = await cats.ObterPorIdAsync(new ComunicacaoAcidenteId(request.ComunicacaoAcidenteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("CAT nao encontrada.");
        var servidor = await servidores.ObterPorIdAsync(cat.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor da CAT nao encontrado.");

        var insumo = new InsumoS2210(
            InscricaoEmpregadorFactory.De(p),
            new IdeVinculoSst(servidor.Cpf.ToString(), servidor.Matricula.Valor),
            DateOnly.FromDateTime(cat.DataHoraAcidente.DateTime),
            cat.DataHoraAcidente.ToString("HHmm", CultureInfo.InvariantCulture),
            (int)cat.TipoAcidente,
            (int)cat.TipoCat,
            cat.HouveObito ? "S" : "N",
            cat.DataObito,
            cat.DescricaoSituacao,
            cat.Cid,
            cat.ParteCorpoAtingida,
            cat.AgenteCausador,
            // nrRecCatOrig: id de negocio da CAT de origem (o recibo real e amarrado no M10 ao transmitir).
            cat.CatOrigem?.Value.ToString());

        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S2210Cat, cat.Id.Value.ToString());
        var xml = GeradorEventosSst.GerarS2210(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S2210Cat, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}

/// <summary>
/// Gera o S-2220 (Monitoramento da Saude/ASO) a partir de um <see cref="ExameOcupacional"/>. Idempotente
/// por (tipo, asoId).
/// </summary>
/// <param name="ExameOcupacionalId">ASO de origem do evento.</param>
public sealed record GerarS2220Command(Guid ExameOcupacionalId) : ICommand<Guid>;

/// <summary>Handler do S-2220.</summary>
public sealed class GerarS2220Handler(
    IEmpregadorESocialProvider empregador,
    IServidorRepository servidores,
    IExameOcupacionalRepository exames,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS2220Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS2220Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var exame = await exames.ObterPorIdAsync(new ExameOcupacionalId(request.ExameOcupacionalId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("ASO nao encontrado.");
        var servidor = await servidores.ObterPorIdAsync(exame.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor do ASO nao encontrado.");

        var insumo = new InsumoS2220(
            InscricaoEmpregadorFactory.De(p),
            new IdeVinculoSst(servidor.Cpf.ToString(), servidor.Matricula.Valor),
            (int)exame.Tipo,
            exame.DataExame,
            (int)exame.Resultado,
            [.. exame.ExamesComplementares],
            exame.Medico.Nome,
            exame.Medico.NrConselho,
            exame.Medico.UfConselho);

        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S2220MonitoramentoSaude, exame.Id.Value.ToString());
        var xml = GeradorEventosSst.GerarS2220(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S2220MonitoramentoSaude, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}

/// <summary>
/// Gera o S-2240 (Condicoes Ambientais/Agentes Nocivos) a partir de uma <see cref="ExposicaoAgenteNocivo"/>.
/// Idempotente por (tipo, exposicaoId).
/// </summary>
/// <param name="ExposicaoAgenteNocivoId">Exposicao de origem do evento.</param>
public sealed record GerarS2240Command(Guid ExposicaoAgenteNocivoId) : ICommand<Guid>;

/// <summary>Handler do S-2240.</summary>
public sealed class GerarS2240Handler(
    IEmpregadorESocialProvider empregador,
    IServidorRepository servidores,
    IExposicaoAgenteNocivoRepository exposicoes,
    GeradorEventoApplicationService gerador)
    : ICommandHandler<GerarS2240Command, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(GerarS2240Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var p = await empregador.ObterAsync(cancellationToken).ConfigureAwait(false);
        var exposicao = await exposicoes.ObterPorIdAsync(new ExposicaoAgenteNocivoId(request.ExposicaoAgenteNocivoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Exposicao nao encontrada.");
        var servidor = await servidores.ObterPorIdAsync(exposicao.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor da exposicao nao encontrado.");

        var agentes = exposicao.Agentes
            .Select(a => new ItemAgenteNocivo(a.Codigo, a.Descricao, a.Intensidade, a.UnidadeMedida, a.UtilizaEpc, a.UtilizaEpi))
            .ToList();

        var insumo = new InsumoS2240(
            InscricaoEmpregadorFactory.De(p),
            new IdeVinculoSst(servidor.Cpf.ToString(), servidor.Matricula.Valor),
            exposicao.InicioExposicao,
            exposicao.FimExposicao,
            exposicao.SetorAtividade,
            agentes);

        var chave = ChaveIdempotenciaEvento.Criar(TipoEventoESocial.S2240AgentesNocivos, exposicao.Id.Value.ToString());
        var xml = GeradorEventosSst.GerarS2240(insumo, idEvento: "PENDENTE");
        var evento = await gerador.MaterializarAsync(p, TipoEventoESocial.S2240AgentesNocivos, chave, xml, cancellationToken).ConfigureAwait(false);
        return evento.Id.Value;
    }
}
