using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Pacientes;
using Tensorroot.Gov.Modules.Saude.Domain.Regulacao;
using Tensorroot.Gov.SharedKernel;
using PacienteRaiz = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.Paciente;

namespace Tensorroot.Gov.Modules.Saude.Application.Regulacao;

/// <summary>Projecao detalhada de uma solicitacao de regulacao para leitura.</summary>
/// <param name="Id">Identificador da solicitacao.</param>
/// <param name="PacienteId">Paciente da solicitacao.</param>
/// <param name="CodigoSigtap">Codigo SIGTAP do procedimento.</param>
/// <param name="DescricaoProcedimento">Descricao do procedimento.</param>
/// <param name="Prioridade">Prioridade (texto).</param>
/// <param name="Situacao">Situacao atual (texto).</param>
/// <param name="DataSolicitacao">Data de abertura.</param>
/// <param name="DataAutorizacao">Data da autorizacao, se houver.</param>
/// <param name="ProtocoloSisreg">Protocolo da reserva no SISREG, se houver.</param>
public sealed record SolicitacaoRegulacaoDetalhe(
    Guid Id,
    Guid PacienteId,
    string CodigoSigtap,
    string DescricaoProcedimento,
    string Prioridade,
    string Situacao,
    DateOnly DataSolicitacao,
    DateOnly? DataAutorizacao,
    string? ProtocoloSisreg);

/// <summary>
/// Obtem uma solicitacao de regulacao por identificador (sempre tenant-scoped). Expoe o procedimento
/// (SIGTAP) vinculado ao paciente — dado sensivel de saude (LGPD art. 11); por implementar
/// <see cref="ISensivelLgpd"/>, GERA TRILHA DE ACESSO (LG-2). Base legal: tutela da saude (art. 11, II, "f").
/// </summary>
/// <param name="SolicitacaoRegulacaoId">Solicitacao a consultar.</param>
public sealed record ObterSolicitacaoRegulacaoPorIdQuery(Guid SolicitacaoRegulacaoId)
    : IQuery<SolicitacaoRegulacaoDetalhe?>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(PacienteRaiz);

    /// <inheritdoc />
    public string? EntidadeId => SolicitacaoRegulacaoId.ToString();

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
}

/// <summary>Handler da consulta de solicitacao de regulacao por identificador.</summary>
public sealed class ObterSolicitacaoRegulacaoPorIdHandler(ISolicitacaoRegulacaoRepository solicitacoes)
    : IQueryHandler<ObterSolicitacaoRegulacaoPorIdQuery, SolicitacaoRegulacaoDetalhe?>
{
    /// <inheritdoc />
    public async Task<SolicitacaoRegulacaoDetalhe?> Handle(
        ObterSolicitacaoRegulacaoPorIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var solicitacao = await solicitacoes
            .ObterPorIdAsync(new SolicitacaoRegulacaoId(request.SolicitacaoRegulacaoId), cancellationToken)
            .ConfigureAwait(false);

        if (solicitacao is null)
        {
            return null;
        }

        return new SolicitacaoRegulacaoDetalhe(
            solicitacao.Id.Value,
            solicitacao.PacienteId.Value,
            solicitacao.Procedimento.CodigoSigtap,
            solicitacao.Procedimento.Descricao,
            solicitacao.Prioridade.ToString(),
            solicitacao.Situacao.ToString(),
            solicitacao.DataSolicitacao,
            solicitacao.DataAutorizacao,
            solicitacao.ProtocoloSisreg);
    }
}
