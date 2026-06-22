using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Atas;

/// <summary>Linha de presenca na ata: vereador presente (com nome) ou ausente.</summary>
/// <param name="VereadorId">Identificador do vereador.</param>
/// <param name="NomeParlamentar">Nome parlamentar (ou GUID se sem cadastro).</param>
/// <param name="Presente">Indica se registrou presenca.</param>
public sealed record AtaPresenca(Guid VereadorId, string NomeParlamentar, bool Presente);

/// <summary>Resultado de uma votacao na ata (placar + apuracao + nomes por sentido).</summary>
/// <param name="ProposicaoId">Materia votada.</param>
/// <param name="Ementa">Ementa da materia (resumo legivel na ata).</param>
/// <param name="Tipo">Modalidade de apuracao.</param>
/// <param name="Sim">Total de votos Sim.</param>
/// <param name="Nao">Total de votos Nao.</param>
/// <param name="Abstencao">Total de abstencoes.</param>
/// <param name="Resultado">Resultado apurado (ou "Em andamento" se ainda aberta).</param>
/// <param name="VotantesSim">Nomes que votaram Sim (vazio se secreta).</param>
/// <param name="VotantesNao">Nomes que votaram Nao (vazio se secreta).</param>
public sealed record AtaVotacao(
    Guid ProposicaoId,
    string Ementa,
    string Tipo,
    int Sim,
    int Nao,
    int Abstencao,
    string Resultado,
    IReadOnlyList<string> VotantesSim,
    IReadOnlyList<string> VotantesNao);

/// <summary>Item da Ordem do Dia na ata (com a ementa da materia).</summary>
/// <param name="Ordem">Posicao na pauta.</param>
/// <param name="ProposicaoId">Materia pautada.</param>
/// <param name="Ementa">Ementa da materia.</param>
public sealed record AtaItemOrdemDoDia(int Ordem, Guid ProposicaoId, string Ementa);

/// <summary>
/// Ata estruturada da sessao, montada a partir do que ja existe no agregado (presenca, Ordem do
/// Dia, votacoes e resultados) — artefato palpavel exigido em editais de Camara. Read-only/derivada:
/// nao altera estado nem depende de encerramento, mas indica a situacao da sessao.
/// </summary>
/// <param name="SessaoId">Identificador da sessao.</param>
/// <param name="Tipo">Especie da sessao.</param>
/// <param name="DataHora">Momento da sessao.</param>
/// <param name="Situacao">Situacao da sessao.</param>
/// <param name="TotalMembros">Numero de vereadores.</param>
/// <param name="QuorumInstalacao">Quorum de instalacao.</param>
/// <param name="Presentes">Quantidade de presentes.</param>
/// <param name="Ausentes">Quantidade de ausentes.</param>
/// <param name="QuorumAtingido">Indica se o quorum de instalacao foi atingido.</param>
/// <param name="Presencas">Lista nominal de presencas (presentes e ausentes).</param>
/// <param name="OrdemDoDia">Itens pautados.</param>
/// <param name="Votacoes">Votacoes da sessao com placar e resultado.</param>
public sealed record AtaSessao(
    Guid SessaoId,
    string Tipo,
    DateTimeOffset DataHora,
    string Situacao,
    int TotalMembros,
    int QuorumInstalacao,
    int Presentes,
    int Ausentes,
    bool QuorumAtingido,
    IReadOnlyList<AtaPresenca> Presencas,
    IReadOnlyList<AtaItemOrdemDoDia> OrdemDoDia,
    IReadOnlyList<AtaVotacao> Votacoes);

/// <summary>Gera a ata estruturada de uma sessao (tenant-scoped).</summary>
/// <param name="SessaoId">Sessao a relatar.</param>
public sealed record GerarAtaDaSessaoQuery(Guid SessaoId) : IQuery<AtaSessao>;

/// <summary>Handler da geracao da ata da sessao.</summary>
public sealed class GerarAtaDaSessaoHandler(
    ISessaoRepository sessoes,
    IVotacaoRepository votacoes,
    IVereadorRepository vereadores,
    IProposicaoRepository proposicoes) : IQueryHandler<GerarAtaDaSessaoQuery, AtaSessao>
{
    /// <inheritdoc />
    public async Task<AtaSessao> Handle(GerarAtaDaSessaoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessao = await sessoes.ObterPorIdAsync(new SessaoId(request.SessaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sessao nao encontrada.");

        var listaVotacoes = await votacoes.ListarAsync(sessao.Id, cancellationToken).ConfigureAwait(false);

        // Resolve nomes de todos os vereadores do tenant (presencas + votos) num so passe.
        var todosVereadores = await vereadores.ListarAsync(cancellationToken).ConfigureAwait(false);
        var nomes = todosVereadores.ToDictionary(v => v.Id, v => v.NomeParlamentar);

        var presentesIds = sessao.Presencas.Select(presenca => presenca.VereadorId).ToHashSet();
        var presencas = MontarPresencas(todosVereadores, sessao, presentesIds, nomes);

        var ementasPorProposicao = await ResolverEmentasAsync(sessao, listaVotacoes, cancellationToken).ConfigureAwait(false);

        var ordemDoDia = sessao.OrdemDoDia
            .OrderBy(item => item.Ordem)
            .Select(item => new AtaItemOrdemDoDia(
                item.Ordem,
                item.ProposicaoId.Value,
                ementasPorProposicao.GetValueOrDefault(item.ProposicaoId, string.Empty)))
            .ToList();

        var atasVotacoes = listaVotacoes
            .Select(votacao => MontarAtaVotacao(votacao, ementasPorProposicao, nomes))
            .ToList();

        var ausentes = Math.Max(0, sessao.TotalMembros - sessao.Presencas.Count);

        return new AtaSessao(
            sessao.Id.Value,
            sessao.Tipo.ToString(),
            sessao.DataHora.Valor,
            sessao.Situacao.ToString(),
            sessao.TotalMembros,
            sessao.QuorumInstalacao,
            sessao.Presencas.Count,
            ausentes,
            sessao.Presencas.Count >= sessao.QuorumInstalacao,
            presencas,
            ordemDoDia,
            atasVotacoes);
    }

    private static List<AtaPresenca> MontarPresencas(
        IReadOnlyList<Domain.Vereadores.Vereador> todosVereadores,
        Sessao sessao,
        HashSet<VereadorId> presentesIds,
        IReadOnlyDictionary<VereadorId, string> nomes)
    {
        // Base nominal: o cadastro de vereadores. Presentes nao cadastrados entram pelo GUID cru.
        var presencas = todosVereadores
            .Select(vereador => new AtaPresenca(
                vereador.Id.Value,
                vereador.NomeParlamentar,
                presentesIds.Contains(vereador.Id)))
            .ToList();

        var conhecidos = todosVereadores.Select(vereador => vereador.Id).ToHashSet();
        foreach (var presenca in sessao.Presencas.Where(presenca => !conhecidos.Contains(presenca.VereadorId)))
        {
            presencas.Add(new AtaPresenca(
                presenca.VereadorId.Value,
                nomes.GetValueOrDefault(presenca.VereadorId, presenca.VereadorId.Value.ToString()),
                Presente: true));
        }

        return presencas;
    }

    private async Task<Dictionary<ProposicaoId, string>> ResolverEmentasAsync(
        Sessao sessao,
        IReadOnlyList<Votacao> listaVotacoes,
        CancellationToken cancellationToken)
    {
        var ids = sessao.OrdemDoDia.Select(item => item.ProposicaoId)
            .Concat(listaVotacoes.Select(votacao => votacao.ProposicaoId))
            .Distinct()
            .ToList();

        var ementas = new Dictionary<ProposicaoId, string>();
        foreach (var id in ids)
        {
            var proposicao = await proposicoes.ObterPorIdAsync(id, cancellationToken).ConfigureAwait(false);
            ementas[id] = proposicao?.Ementa.Valor ?? string.Empty;
        }

        return ementas;
    }

    private static AtaVotacao MontarAtaVotacao(
        Votacao votacao,
        IReadOnlyDictionary<ProposicaoId, string> ementas,
        IReadOnlyDictionary<VereadorId, string> nomes)
    {
        var secreta = votacao.Tipo == TipoVotacao.Secreta;
        var nominais = secreta ? [] : votacao.Votos;

        List<string> NomesPorSentido(SentidoVoto sentido) => nominais
            .Where(voto => voto.Sentido == sentido)
            .Select(voto => nomes.GetValueOrDefault(voto.VereadorId, voto.VereadorId.Value.ToString()))
            .ToList();

        var resultado = votacao.Resultado?.ToString() ?? "EmAndamento";

        return new AtaVotacao(
            votacao.ProposicaoId.Value,
            ementas.GetValueOrDefault(votacao.ProposicaoId, string.Empty),
            votacao.Tipo.ToString(),
            votacao.VotosSim,
            votacao.VotosNao,
            votacao.Abstencoes,
            resultado,
            NomesPorSentido(SentidoVoto.Sim),
            NomesPorSentido(SentidoVoto.Nao));
    }
}
