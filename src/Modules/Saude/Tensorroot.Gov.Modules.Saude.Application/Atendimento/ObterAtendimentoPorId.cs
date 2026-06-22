using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Atendimento;

namespace Tensorroot.Gov.Modules.Saude.Application.Atendimento;

/// <summary>Projecao de uma evolucao SOAP/adendo para leitura.</summary>
/// <param name="Id">Identificador da evolucao.</param>
/// <param name="Subjetivo">Componente Subjetivo.</param>
/// <param name="Objetivo">Componente Objetivo.</param>
/// <param name="Avaliacao">Componente Avaliacao.</param>
/// <param name="Plano">Componente Plano.</param>
/// <param name="DataHora">Data/hora do registro.</param>
/// <param name="Assinada">Indica se a evolucao esta assinada.</param>
/// <param name="EhAdendo">Indica se a nota e um adendo.</param>
public sealed record EvolucaoDto(
    Guid Id,
    string Subjetivo,
    string Objetivo,
    string Avaliacao,
    string Plano,
    DateTimeOffset DataHora,
    bool Assinada,
    bool EhAdendo);

/// <summary>Projecao de uma prescricao para leitura.</summary>
/// <param name="Id">Identificador da prescricao.</param>
/// <param name="Item">Item prescrito.</param>
/// <param name="Posologia">Posologia.</param>
/// <param name="DataHora">Data/hora.</param>
public sealed record PrescricaoDto(Guid Id, string Item, string Posologia, DateTimeOffset DataHora);

/// <summary>Projecao de uma solicitacao de exame para leitura.</summary>
/// <param name="Id">Identificador da solicitacao.</param>
/// <param name="Procedimento">Procedimento/exame.</param>
/// <param name="Justificativa">Justificativa clinica.</param>
/// <param name="DataHora">Data/hora.</param>
public sealed record SolicitacaoExameDto(Guid Id, string Procedimento, string Justificativa, DateTimeOffset DataHora);

/// <summary>Projecao de detalhe de um atendimento para leitura.</summary>
/// <param name="Id">Identificador do atendimento.</param>
/// <param name="PacienteId">Paciente atendido.</param>
/// <param name="EstabelecimentoId">Estabelecimento (CNES).</param>
/// <param name="ProfissionalId">Profissional responsavel.</param>
/// <param name="DataHora">Data/hora do atendimento.</param>
/// <param name="Competencia">Competencia (AAAA-MM).</param>
/// <param name="Modalidade">Modalidade.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="NivelGarantia">Nivel de garantia (NGS1/NGS2).</param>
/// <param name="Assinado">Indica se o atendimento esta assinado.</param>
/// <param name="Evolucoes">Notas SOAP/adendos.</param>
/// <param name="Prescricoes">Prescricoes.</param>
/// <param name="Exames">Solicitacoes de exame.</param>
public sealed record AtendimentoDetalhe(
    Guid Id,
    Guid PacienteId,
    Guid EstabelecimentoId,
    Guid ProfissionalId,
    DateTimeOffset DataHora,
    string Competencia,
    string Modalidade,
    string Situacao,
    string NivelGarantia,
    bool Assinado,
    IReadOnlyList<EvolucaoDto> Evolucoes,
    IReadOnlyList<PrescricaoDto> Prescricoes,
    IReadOnlyList<SolicitacaoExameDto> Exames);

/// <summary>Obtem o detalhe de um atendimento (tenant-scoped; dado sensivel — LGPD art. 11).</summary>
/// <param name="AtendimentoId">Atendimento a consultar.</param>
public sealed record ObterAtendimentoPorIdQuery(Guid AtendimentoId) : IQuery<AtendimentoDetalhe?>;

/// <summary>Handler da consulta de detalhe do atendimento.</summary>
public sealed class ObterAtendimentoPorIdHandler(IAtendimentoRepository atendimentos)
    : IQueryHandler<ObterAtendimentoPorIdQuery, AtendimentoDetalhe?>
{
    /// <inheritdoc />
    public async Task<AtendimentoDetalhe?> Handle(ObterAtendimentoPorIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var atendimento = await atendimentos
            .ObterPorIdAsync(new AtendimentoId(request.AtendimentoId), cancellationToken)
            .ConfigureAwait(false);

        if (atendimento is null)
        {
            return null;
        }

        return new AtendimentoDetalhe(
            atendimento.Id.Value,
            atendimento.PacienteId.Value,
            atendimento.EstabelecimentoId.Value,
            atendimento.ProfissionalId.Value,
            atendimento.DataHora,
            atendimento.Competencia.ToCodigo(),
            atendimento.Modalidade.ToString(),
            atendimento.Situacao.ToString(),
            atendimento.NivelGarantia.ToString(),
            atendimento.Situacao is SituacaoAtendimento.Assinado or SituacaoAtendimento.Compartilhado,
            atendimento.Evolucoes
                .Select(e => new EvolucaoDto(
                    e.Id.Value,
                    e.Subjetivo,
                    e.Objetivo,
                    e.Avaliacao,
                    e.Plano,
                    e.DataHora,
                    e.Assinada,
                    e.EhAdendo))
                .ToList(),
            atendimento.Prescricoes
                .Select(p => new PrescricaoDto(p.Id.Value, p.Item, p.Posologia, p.DataHora))
                .ToList(),
            atendimento.SolicitacoesExame
                .Select(s => new SolicitacaoExameDto(s.Id.Value, s.Procedimento, s.Justificativa, s.DataHora))
                .ToList());
    }
}
