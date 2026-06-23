using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.SharedKernel;
using DomainPacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;
using PacienteRaiz = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.Paciente;

namespace Tensorroot.Gov.Modules.Saude.Application.Farmacia;

/// <summary>
/// Historico de dispensacoes de medicamento de um paciente (o que recebeu, quando, onde). Dado pessoal
/// SENSIVEL de saude (LGPD art. 11): por implementar <see cref="ISensivelLgpd"/>, GERA TRILHA DE ACESSO
/// (LG-2) — quem leu, de qual paciente, sob qual base legal, quando, de qual IP. Base legal: tutela da
/// saude. Tenant-scoped via Global Query Filter.
/// </summary>
/// <param name="PacienteId">Paciente cujo historico de dispensacao sera lido.</param>
/// <param name="De">Data inicial (opcional).</param>
/// <param name="Ate">Data final (opcional).</param>
public sealed record ListarDispensacoesDoPacienteQuery(Guid PacienteId, DateOnly? De, DateOnly? Ate)
    : IQuery<IReadOnlyList<DispensacaoDto>>, ISensivelLgpd
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

/// <summary>Handler do historico de dispensacoes do paciente.</summary>
public sealed class ListarDispensacoesDoPacienteHandler(IDispensacaoRepository dispensacoes)
    : IQueryHandler<ListarDispensacoesDoPacienteQuery, IReadOnlyList<DispensacaoDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DispensacaoDto>> Handle(
        ListarDispensacoesDoPacienteQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var itens = await dispensacoes
            .ListarPorPacienteAsync(new DomainPacienteId(request.PacienteId), request.De, request.Ate, cancellationToken)
            .ConfigureAwait(false);

        return itens
            .Select(d => new DispensacaoDto(
                d.Id.Value,
                d.EstabelecimentoId.Value,
                d.DataHora,
                d.Situacao.ToString(),
                d.PrescricaoId?.Value,
                d.Itens.Select(i => new ItemDispensadoDto(i.MedicamentoId.Value, i.Quantidade, i.Posologia)).ToList()))
            .ToList();
    }
}
