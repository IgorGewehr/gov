using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Projecao de uma condicao de saude no historico clinico.</summary>
/// <param name="Codigo">Codigo CID-10/CIAP-2.</param>
/// <param name="Descricao">Descricao da condicao.</param>
/// <param name="DataRegistro">Data do registro.</param>
/// <param name="Ativa">Indica se a condicao esta ativa.</param>
public sealed record CondicaoDto(string Codigo, string Descricao, DateOnly DataRegistro, bool Ativa);

/// <summary>Projecao de uma alergia no historico clinico.</summary>
/// <param name="Substancia">Substancia/agente.</param>
/// <param name="Gravidade">Gravidade da reacao.</param>
/// <param name="DataRegistro">Data do registro.</param>
public sealed record AlergiaDto(string Substancia, string Gravidade, DateOnly DataRegistro);

/// <summary>Historico clinico do paciente (condicoes e alergias) — dado pessoal sensivel (LGPD art. 11).</summary>
/// <param name="PacienteId">Identificador do paciente.</param>
/// <param name="Condicoes">Condicoes de saude registradas.</param>
/// <param name="Alergias">Alergias registradas.</param>
public sealed record HistoricoClinicoDto(
    Guid PacienteId,
    IReadOnlyList<CondicaoDto> Condicoes,
    IReadOnlyList<AlergiaDto> Alergias);

/// <summary>
/// Obtem o historico clinico (condicoes/alergias) de um paciente (sempre tenant-scoped).
/// Dado pessoal SENSIVEL (LGPD art. 11): requer claim de leitura e, por implementar
/// <see cref="ISensivelLgpd"/>, GERA TRILHA DE ACESSO (LG-2) — o pipeline sela
/// <c>{Tenant,UserId,Ip,Entidade,EntityId,BaseLegal,Ts}</c> apos a leitura. Base legal: tutela da
/// saude (art. 11, II, "f").
/// </summary>
/// <param name="PacienteId">Paciente a consultar.</param>
public sealed record ObterHistoricoClinicoDoPacienteQuery(Guid PacienteId)
    : IQuery<HistoricoClinicoDto>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(Paciente);

    /// <inheritdoc />
    public string? EntidadeId => PacienteId.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;
}

/// <summary>Handler da consulta de historico clinico do paciente.</summary>
public sealed class ObterHistoricoClinicoDoPacienteHandler(IPacienteRepository pacientes)
    : IQueryHandler<ObterHistoricoClinicoDoPacienteQuery, HistoricoClinicoDto>
{
    /// <inheritdoc />
    public async Task<HistoricoClinicoDto> Handle(ObterHistoricoClinicoDoPacienteQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paciente = await pacientes.ObterPorIdAsync(new PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        var condicoes = paciente.Condicoes
            .Select(c => new CondicaoDto(c.Codigo, c.Descricao, c.DataRegistro, c.Ativa))
            .ToList();

        var alergias = paciente.Alergias
            .Select(a => new AlergiaDto(a.Substancia, a.Gravidade, a.DataRegistro))
            .ToList();

        return new HistoricoClinicoDto(paciente.Id.Value, condicoes, alergias);
    }
}
