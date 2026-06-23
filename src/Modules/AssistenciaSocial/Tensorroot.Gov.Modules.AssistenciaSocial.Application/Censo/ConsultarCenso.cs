using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Censo;

/// <summary>Servico ofertado por uma unidade (projecao de leitura).</summary>
/// <param name="Servico">Servico tipificado.</param>
/// <param name="CapacidadeMensal">Capacidade mensal de atendimento.</param>
public sealed record ServicoOfertadoResultado(string Servico, int CapacidadeMensal);

/// <summary>Projecao de leitura de uma unidade socioassistencial.</summary>
/// <param name="Id">Identificador da unidade.</param>
/// <param name="Nome">Nome da unidade.</param>
/// <param name="Tipo">Tipo (CRAS/CREAS/Centro POP).</param>
/// <param name="TerritorioCobertura">Territorio coberto.</param>
/// <param name="Endereco">Endereco.</param>
/// <param name="QuantidadeProfissionais">Tamanho da equipe de referencia.</param>
/// <param name="Servicos">Servicos ofertados.</param>
public sealed record UnidadeSocioassistencialResultado(
    Guid Id,
    string Nome,
    string Tipo,
    string TerritorioCobertura,
    string Endereco,
    int QuantidadeProfissionais,
    IReadOnlyList<ServicoOfertadoResultado> Servicos);

/// <summary>3d.2: lista as unidades socioassistenciais cadastradas, tenant-scoped.</summary>
public sealed record ListarUnidadesSocioassistenciaisQuery : IQuery<IReadOnlyList<UnidadeSocioassistencialResultado>>;

/// <summary>Handler da listagem de unidades.</summary>
public sealed class ListarUnidadesSocioassistenciaisHandler(IUnidadeSocioassistencialRepository unidades)
    : IQueryHandler<ListarUnidadesSocioassistenciaisQuery, IReadOnlyList<UnidadeSocioassistencialResultado>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UnidadeSocioassistencialResultado>> Handle(ListarUnidadesSocioassistenciaisQuery request, CancellationToken cancellationToken)
    {
        var encontradas = await unidades.ListarAsync(cancellationToken).ConfigureAwait(false);
        return encontradas
            .Select(u => new UnidadeSocioassistencialResultado(
                u.Id.Value,
                u.Nome,
                u.Tipo.ToString(),
                u.TerritorioCobertura,
                u.Endereco,
                u.QuantidadeProfissionais,
                u.Servicos.Select(s => new ServicoOfertadoResultado(s.Servico.ToString(), s.CapacidadeMensal)).ToList()))
            .ToList();
    }
}

/// <summary>Resultado consolidado do Censo SUAS de uma unidade num exercicio.</summary>
/// <param name="FormularioId">Identificador do formulario.</param>
/// <param name="UnidadeId">Unidade consolidada.</param>
/// <param name="Exercicio">Exercicio (ano).</param>
/// <param name="Fechado">Indica se o exercicio foi selado.</param>
/// <param name="QuantidadeProfissionais">Equipe de referencia consolidada.</param>
/// <param name="QuantidadeServicosOfertados">Servicos ofertados consolidados.</param>
/// <param name="FamiliasReferenciadas">Familias referenciadas a unidade.</param>
/// <param name="VolumeAtendimentosAno">Volume anual de atendimentos (do RMA).</param>
public sealed record CensoResultado(
    Guid FormularioId,
    Guid UnidadeId,
    int Exercicio,
    bool Fechado,
    int QuantidadeProfissionais,
    int QuantidadeServicosOfertados,
    int FamiliasReferenciadas,
    int VolumeAtendimentosAno);

/// <summary>3d.2: lista os formularios consolidados do Censo de um exercicio, tenant-scoped.</summary>
/// <param name="Exercicio">Exercicio (ano).</param>
public sealed record ObterCensoQuery(int Exercicio) : IQuery<IReadOnlyList<CensoResultado>>;

/// <summary>Handler da consulta do Censo consolidado.</summary>
public sealed class ObterCensoHandler(IFormularioCensoSuasRepository formularios)
    : IQueryHandler<ObterCensoQuery, IReadOnlyList<CensoResultado>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<CensoResultado>> Handle(ObterCensoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var encontrados = await formularios.ListarPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        return encontrados
            .Select(f => new CensoResultado(
                f.Id.Value,
                f.UnidadeId.Value,
                f.Exercicio,
                f.Situacao == SituacaoCenso.Fechado,
                f.QuantidadeProfissionais,
                f.QuantidadeServicosOfertados,
                f.FamiliasReferenciadas,
                f.VolumeAtendimentosAno))
            .ToList();
    }
}
