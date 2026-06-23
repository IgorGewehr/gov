using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.SharedKernel;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using PacienteRaiz = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.Paciente;

namespace Tensorroot.Gov.Modules.Saude.Application.Imunizacao;

/// <summary>Lista os imunobiologicos ativos do catalogo (operacional, sem LGPD). Tenant-scoped.</summary>
public sealed record ListarImunobiologicosQuery : IQuery<IReadOnlyList<ImunobiologicoItemLista>>;

/// <summary>Handler da listagem de imunobiologicos.</summary>
public sealed class ListarImunobiologicosHandler(IImunobiologicoRepository imunobiologicos)
    : IQueryHandler<ListarImunobiologicosQuery, IReadOnlyList<ImunobiologicoItemLista>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ImunobiologicoItemLista>> Handle(
        ListarImunobiologicosQuery request, CancellationToken cancellationToken)
    {
        var itens = await imunobiologicos.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);
        return itens
            .Select(i => new ImunobiologicoItemLista(
                i.Id.Value, i.Nome, i.Sigla, i.TotalDoses, i.IntervaloDiasProximaDose, i.DoseUnica))
            .ToList();
    }
}

/// <summary>
/// Obtem a carteira de vacinacao de um paciente (historico de doses + aprazamentos). Dado pessoal
/// SENSIVEL de saude (LGPD art. 11): por implementar <see cref="ISensivelLgpd"/>, GERA TRILHA DE
/// ACESSO (LG-2). Base legal: tutela da saude. Tenant-scoped via Global Query Filter.
/// </summary>
/// <param name="PacienteId">Paciente cuja carteira sera lida.</param>
public sealed record ObterCarteiraVacinacaoQuery(Guid PacienteId)
    : IQuery<CarteiraVacinacaoDto>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(PacienteRaiz);

    /// <inheritdoc />
    public string? EntidadeId => PacienteId.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
}

/// <summary>Handler da consulta de carteira de vacinacao.</summary>
public sealed class ObterCarteiraVacinacaoHandler(
    ICarteiraVacinacaoRepository carteiras,
    IImunobiologicoRepository imunobiologicos)
    : IQueryHandler<ObterCarteiraVacinacaoQuery, CarteiraVacinacaoDto>
{
    /// <inheritdoc />
    public async Task<CarteiraVacinacaoDto> Handle(ObterCarteiraVacinacaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pacienteId = new DomainPacienteId(request.PacienteId);

        var carteira = await carteiras.ObterPorPacienteAsync(pacienteId, cancellationToken).ConfigureAwait(false);
        if (carteira is null)
        {
            // Paciente sem nenhuma dose: carteira vazia (nao e erro).
            return new CarteiraVacinacaoDto(request.PacienteId, []);
        }

        var ativos = await imunobiologicos.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);
        var siglaPorId = ativos.ToDictionary(i => i.Id, i => i.Sigla);

        var doses = carteira.Doses
            .OrderBy(d => d.DataAplicacao)
            .Select(d => new DoseAplicadaDto(
                d.ImunobiologicoId.Value,
                siglaPorId.TryGetValue(d.ImunobiologicoId, out var sigla) ? sigla : string.Empty,
                d.TipoDose.ToString(),
                d.NumeroDose,
                d.Lote,
                d.DataAplicacao,
                d.ProximaDoseAprazada))
            .ToList();

        return new CarteiraVacinacaoDto(request.PacienteId, doses);
    }
}

/// <summary>
/// Lista os aprazamentos vencidos (proxima dose atrasada e nao cumprida) ate hoje — alvo da busca ativa
/// vacinal. Dado de saude do paciente: por implementar <see cref="ISensivelLgpd"/>, GERA TRILHA DE
/// ACESSO. EntidadeId nulo (e uma busca, nao um paciente especifico). Base legal: politica publica (PNI).
/// </summary>
public sealed record ListarAprazamentosVencidosQuery
    : IQuery<IReadOnlyList<AprazamentoVencidoDto>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(PacienteRaiz);

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.PoliticaPublica;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
}

/// <summary>Handler da busca ativa de aprazamentos vencidos.</summary>
public sealed class ListarAprazamentosVencidosHandler(
    ICarteiraVacinacaoRepository carteiras,
    IImunobiologicoRepository imunobiologicos,
    TimeProvider timeProvider)
    : IQueryHandler<ListarAprazamentosVencidosQuery, IReadOnlyList<AprazamentoVencidoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AprazamentoVencidoDto>> Handle(
        ListarAprazamentosVencidosQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var comVencidos = await carteiras.ListarComAprazamentoVencidoAsync(hoje, cancellationToken).ConfigureAwait(false);
        var ativos = await imunobiologicos.ListarAtivosAsync(cancellationToken).ConfigureAwait(false);
        var siglaPorId = ativos.ToDictionary(i => i.Id, i => i.Sigla);

        var resultado = new List<AprazamentoVencidoDto>();
        foreach (var carteira in comVencidos)
        {
            foreach (var dose in carteira.AprazamentosVencidos(hoje))
            {
                resultado.Add(new AprazamentoVencidoDto(
                    carteira.PacienteId.Value,
                    dose.ImunobiologicoId.Value,
                    siglaPorId.TryGetValue(dose.ImunobiologicoId, out var sigla) ? sigla : string.Empty,
                    dose.NumeroDose,
                    dose.ProximaDoseAprazada!.Value));
            }
        }

        return resultado;
    }
}
