using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Projecao minimizada do paciente para leitura por CNS (LGPD: minimizacao de dados).</summary>
/// <param name="Id">Identificador do paciente.</param>
/// <param name="Cns">Cartao Nacional de Saude.</param>
/// <param name="Nome">Nome civil.</param>
/// <param name="NomeSocial">Nome social, quando informado.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="Sexo">Sexo (descricao).</param>
/// <param name="CnsConfirmado">Indica se o CNS foi confirmado no CADSUS.</param>
/// <param name="Situacao">Situacao atual do cadastro.</param>
public sealed record PacienteResumo(
    Guid Id,
    string Cns,
    string Nome,
    string? NomeSocial,
    DateOnly DataNascimento,
    string Sexo,
    bool CnsConfirmado,
    string Situacao);

/// <summary>
/// Obtem o resumo de um paciente pelo CNS (sempre tenant-scoped via Global Query Filter).
/// Dado pessoal SENSIVEL (LGPD art. 11): requer claim de leitura e, por implementar
/// <see cref="ISensivelLgpd"/>, GERA TRILHA DE ACESSO (LG-2). O CNS e PII e NAO vai para a trilha
/// (<see cref="EntidadeId"/> nulo — leitura por chave de negocio). Base legal: tutela da saude.
/// </summary>
/// <param name="Cns">Cartao Nacional de Saude a consultar.</param>
public sealed record ObterPacientePorCnsQuery(string Cns)
    : IQuery<PacienteResumo?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(Paciente);

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;
}

/// <summary>Handler da consulta de paciente por CNS.</summary>
public sealed class ObterPacientePorCnsHandler(IPacienteRepository pacientes)
    : IQueryHandler<ObterPacientePorCnsQuery, PacienteResumo?>
{
    /// <inheritdoc />
    public async Task<PacienteResumo?> Handle(ObterPacientePorCnsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paciente = await pacientes.ObterPorCnsAsync(new Cns(request.Cns), cancellationToken).ConfigureAwait(false);
        if (paciente is null)
        {
            return null;
        }

        return new PacienteResumo(
            paciente.Id.Value,
            paciente.Cns.Valor,
            paciente.Identificacao.Nome,
            paciente.Identificacao.NomeSocial,
            paciente.Identificacao.DataNascimento,
            paciente.Identificacao.Sexo.ToString(),
            paciente.CnsConfirmado,
            paciente.Situacao.ToString());
    }
}
