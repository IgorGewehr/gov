using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;
using Tensorroot.Gov.SharedKernel;
using PacienteRaiz = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.Paciente;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Resumo de um atendimento na linha do tempo do paciente.</summary>
/// <param name="Id">Identificador do atendimento.</param>
/// <param name="DataHora">Data/hora do atendimento.</param>
/// <param name="Modalidade">Modalidade.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="Cid">Diagnostico CID-10, se informado.</param>
/// <param name="Ciap">Diagnostico CIAP-2, se informado.</param>
public sealed record AtendimentoResumo(
    Guid Id,
    DateTimeOffset DataHora,
    string Modalidade,
    string Situacao,
    string? Cid,
    string? Ciap);

/// <summary>
/// Lista os atendimentos de um paciente (tenant-scoped; dado sensivel — LGPD art. 11). A linha do tempo
/// expoe diagnostico (CID-10/CIAP-2); por implementar <see cref="ISensivelLgpd"/>, GERA TRILHA DE ACESSO
/// (LG-2). Base legal: tutela da saude (LGPD art. 11, II, "f").
/// </summary>
/// <param name="PacienteId">Paciente.</param>
/// <param name="De">Data inicial (opcional).</param>
/// <param name="Ate">Data final (opcional).</param>
public sealed record ListarAtendimentosDoPacienteQuery(
    Guid PacienteId,
    DateOnly? De,
    DateOnly? Ate) : IQuery<IReadOnlyList<AtendimentoResumo>>, ISensivelLgpd
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

/// <summary>Handler da consulta de atendimentos do paciente.</summary>
public sealed class ListarAtendimentosDoPacienteHandler(IAtendimentoRepository atendimentos)
    : IQueryHandler<ListarAtendimentosDoPacienteQuery, IReadOnlyList<AtendimentoResumo>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AtendimentoResumo>> Handle(
        ListarAtendimentosDoPacienteQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lista = await atendimentos
            .ListarPorPacienteAsync(new PacienteId(request.PacienteId), request.De, request.Ate, cancellationToken)
            .ConfigureAwait(false);

        return lista
            .Select(atendimento => new AtendimentoResumo(
                atendimento.Id.Value,
                atendimento.DataHora,
                atendimento.Modalidade.ToString(),
                atendimento.Situacao.ToString(),
                atendimento.Cid?.Codigo,
                atendimento.Ciap?.Codigo))
            .ToList();
    }
}
