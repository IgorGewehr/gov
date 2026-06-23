using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Votacoes;

/// <summary>Linha do painel: o voto de um vereador COM o nome (ou GUID cru se sem cadastro).</summary>
/// <param name="VereadorId">Identificador do vereador.</param>
/// <param name="NomeParlamentar">Nome parlamentar resolvido (ou o GUID, se nao cadastrado).</param>
/// <param name="Sentido">Sentido do voto (Sim/Nao/Abstencao).</param>
/// <param name="RegistradoEm">Momento do registro.</param>
public sealed record LinhaPainel(Guid VereadorId, string NomeParlamentar, string Sentido, DateTimeOffset RegistradoEm);

/// <summary>
/// Estado completo que um painel eletronico consome/atualiza: placar agregado (sim/nao/abstencao),
/// totais, quorum, resultado apurado em tempo real e a lista nominal de votos COM o nome do vereador.
/// Em votacao secreta a lista nominal e omitida (I-12), mas o agregado permanece visivel.
/// </summary>
/// <param name="VotacaoId">Identificador da votacao.</param>
/// <param name="SessaoId">Sessao em que ocorre.</param>
/// <param name="ProposicaoId">Materia votada.</param>
/// <param name="Tipo">Modalidade de apuracao.</param>
/// <param name="MaioriaExigida">Criterio de aprovacao exigido.</param>
/// <param name="Situacao">Situacao atual.</param>
/// <param name="Sim">Total de votos Sim.</param>
/// <param name="Nao">Total de votos Nao.</param>
/// <param name="Abstencao">Total de abstencoes.</param>
/// <param name="TotalVotos">Total de votos registrados.</param>
/// <param name="TotalMembros">Numero de vereadores da Camara.</param>
/// <param name="Presentes">Numero de presentes (base da maioria simples).</param>
/// <param name="Ausentes">Membros que ainda nao votaram (TotalMembros - TotalVotos).</param>
/// <param name="QuorumMinimo">Quorum minimo (maioria absoluta dos membros).</param>
/// <param name="QuorumAtingido">Indica se o total de votos alcanca o quorum minimo.</param>
/// <param name="ResultadoApurado">Resultado oficial apos encerrar (nulo enquanto aberta).</param>
/// <param name="ResultadoParcial">Apuracao projetada em tempo real (preview do placar; nulo se secreta).</param>
/// <param name="Votos">Lista nominal dos votos com o nome do vereador (vazia se secreta).</param>
public sealed record PainelVotacao(
    Guid VotacaoId,
    Guid SessaoId,
    Guid ProposicaoId,
    string Tipo,
    string MaioriaExigida,
    string Situacao,
    int Sim,
    int Nao,
    int Abstencao,
    int TotalVotos,
    int TotalMembros,
    int Presentes,
    int Ausentes,
    int QuorumMinimo,
    bool QuorumAtingido,
    string? ResultadoApurado,
    string ResultadoParcial,
    IReadOnlyList<LinhaPainel> Votos);

/// <summary>Obtem o estado do painel ao vivo de uma votacao (placar + nomes + quorum + apuracao).</summary>
/// <param name="VotacaoId">Votacao a consultar.</param>
public sealed record ObterPainelDaVotacaoQuery(Guid VotacaoId) : IQuery<PainelVotacao>;

/// <summary>Handler do painel ao vivo da votacao.</summary>
public sealed class ObterPainelDaVotacaoHandler(
    IVotacaoRepository votacoes,
    IVereadorRepository vereadores) : IQueryHandler<ObterPainelDaVotacaoQuery, PainelVotacao>
{
    /// <inheritdoc />
    public async Task<PainelVotacao> Handle(ObterPainelDaVotacaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var votacao = await votacoes.ObterPorIdAsync(new VotacaoId(request.VotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Votacao nao encontrada.");

        var secreta = votacao.Tipo == TipoVotacao.Secreta;

        // I-12: votacao secreta nunca expoe o voto individual — so o agregado.
        IReadOnlyList<LinhaPainel> linhas = [];
        if (!secreta)
        {
            var ids = votacao.Votos.Select(voto => voto.VereadorId).Distinct().ToList();
            var nomes = await vereadores.ResolverNomesAsync(ids, cancellationToken).ConfigureAwait(false);

            linhas = votacao.Votos
                .OrderBy(voto => voto.RegistradoEm)
                .Select(voto => new LinhaPainel(
                    voto.VereadorId.Value,
                    nomes.TryGetValue(voto.VereadorId, out var nome) ? nome : voto.VereadorId.Value.ToString(),
                    voto.Sentido.ToString(),
                    voto.RegistradoEm))
                .ToList();
        }

        var quorumMinimo = votacao.QuorumMinimo;
        var ausentes = Math.Max(0, votacao.TotalMembros - votacao.Votos.Count);
        var resultadoParcial = ApurarParcial(votacao);

        return new PainelVotacao(
            votacao.Id.Value,
            votacao.SessaoId.Value,
            votacao.ProposicaoId.Value,
            votacao.Tipo.ToString(),
            votacao.MaioriaExigida.ToString(),
            votacao.Situacao.ToString(),
            votacao.VotosSim,
            votacao.VotosNao,
            votacao.Abstencoes,
            votacao.Votos.Count,
            votacao.TotalMembros,
            votacao.Presentes,
            ausentes,
            quorumMinimo,
            votacao.Votos.Count >= quorumMinimo,
            votacao.Resultado?.ToString(),
            resultadoParcial.ToString(),
            linhas);
    }

    /// <summary>
    /// Apuracao projetada (preview) com os votos atuais. Delega a regra de maioria ao dominio
    /// (<see cref="Votacao.AtingeMaioria"/>) para NAO duplicar a formula de apuracao: a tela nunca
    /// mostra desfecho diferente do oficial. Sinaliza <see cref="ResultadoVotacao.Prejudicado"/>
    /// quando o preview indica encerramento sem quorum minimo. NAO altera o agregado.
    /// </summary>
    private static ResultadoVotacao ApurarParcial(Votacao votacao)
    {
        if (votacao.Presentes < votacao.QuorumMinimo)
        {
            return ResultadoVotacao.Prejudicado;
        }

        return votacao.AtingeMaioria(votacao.MaioriaExigida)
            ? ResultadoVotacao.Aprovado
            : ResultadoVotacao.Rejeitado;
    }
}
