using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Legislativo.Application.Abstractions;
using Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes;
using Tensorroot.Gov.Modules.Legislativo.Domain.Vereadores;
using Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

namespace Tensorroot.Gov.Modules.Legislativo.Application.Demonstracao;

/// <summary>Resumo do que o seed de demonstracao criou (para retorno/diagnostico).</summary>
/// <param name="Vereadores">Vereadores cadastrados.</param>
/// <param name="ProposicaoId">Proposicao em tramitacao criada (vazio se ja semeado).</param>
/// <param name="SessaoId">Sessao com pauta criada (vazio se ja semeado).</param>
/// <param name="VotacaoId">Votacao nominal criada (vazio se ja semeado).</param>
/// <param name="JaSemeado">Indica que o seed era idempotente e nao recriou nada.</param>
public sealed record ResultadoSeed(int Vereadores, Guid ProposicaoId, Guid SessaoId, Guid VotacaoId, bool JaSemeado);

/// <summary>
/// Semeia o cenario de demonstracao do modulo Legislativo (idempotente, por tenant): legislatura +
/// Mesa Diretora + 9 vereadores nomeados + 1 proposicao em tramitacao (com pareceres CCJ/Financas, em
/// Ordem do Dia) + 1 sessao aberta com presencas e pauta + 1 votacao nominal com votos registrados.
/// Idempotencia: se ja existem vereadores no tenant, nao recria nada (no-op).
/// </summary>
public sealed record SemearDemonstracaoLegislativaCommand : ICommand<ResultadoSeed>;

/// <summary>Handler do seed de demonstracao legislativa.</summary>
public sealed class SemearDemonstracaoLegislativaHandler(
    IVereadorRepository vereadores,
    IProposicaoRepository proposicoes,
    ISessaoRepository sessoes,
    IVotacaoRepository votacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider) : ICommandHandler<SemearDemonstracaoLegislativaCommand, ResultadoSeed>
{
    /// <inheritdoc />
    public async Task<ResultadoSeed> Handle(SemearDemonstracaoLegislativaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotencia por tenant: presenca de qualquer vereador indica que o tenant ja foi semeado.
        if (await vereadores.ContarAsync(cancellationToken).ConfigureAwait(false) > 0)
        {
            return new ResultadoSeed(0, Guid.Empty, Guid.Empty, Guid.Empty, JaSemeado: true);
        }

        var tenantId = tenant.TenantId;
        var agora = timeProvider.GetUtcNow();
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);

        var titulares = SemearVereadores(tenantId);
        var proposicao = SemearProposicao(tenantId, hoje);
        var sessao = SemearSessao(tenantId, proposicao.Id, titulares, agora);
        var votacao = SemearVotacao(tenantId, sessao, proposicao, titulares, agora);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoSeed(
            titulares.Count,
            proposicao.Id.Value,
            sessao.Id.Value,
            votacao.Id.Value,
            JaSemeado: false);
    }

    private List<Vereador> SemearVereadores(Guid tenantId)
    {
        var titulares = new List<Vereador>();
        foreach (var def in VereadoresCatalogo.Vereadores())
        {
            var vereador = Vereador.Cadastrar(
                tenantId,
                def.NomeCivil,
                def.NomeParlamentar,
                def.Partido,
                VereadoresCatalogo.LegislaturaInicio,
                VereadoresCatalogo.LegislaturaFim,
                def.CargoMesa);

            vereadores.Adicionar(vereador);
            titulares.Add(vereador);
        }

        return titulares;
    }

    private Proposicao SemearProposicao(Guid tenantId, DateOnly hoje)
    {
        // PLO em tramitacao, ja com os pareceres obrigatorios e incluida em Ordem do Dia,
        // pronta para deliberacao na demo.
        var proposicao = Proposicao.Apresentar(
            tenantId,
            TipoProposicao.ProjetoDeLeiOrdinaria,
            Ementa.De("Dispoe sobre a denominacao de logradouro publico no Municipio e da outras providencias."),
            Autoria.De("Vereadora Ana Paula (PSDB)"),
            RegimeTramitacao.Ordinario,
            hoje.AddDays(-10),
            protocolo: "PLO-0001/2026");

        proposicao.Distribuir(hoje.AddDays(-8));
        proposicao.RegistrarParecer(Proposicao.ComissaoCcj, favoravel: true, hoje.AddDays(-5));
        proposicao.RegistrarParecer(Proposicao.ComissaoFinancasOrcamento, favoravel: true, hoje.AddDays(-3));
        proposicao.IncluirEmOrdemDoDia(hoje.AddDays(-1));

        proposicoes.Adicionar(proposicao);
        return proposicao;
    }

    private Sessao SemearSessao(
        Guid tenantId,
        ProposicaoId proposicaoId,
        List<Vereador> titulares,
        DateTimeOffset agora)
    {
        var sessao = Sessao.Agendar(tenantId, TipoSessao.Ordinaria, DataHora.De(agora), titulares.Count);

        // Quorum de instalacao: 8 dos 9 presentes (um ausente, para a demo mostrar ausencia).
        foreach (var vereador in titulares.Take(titulares.Count - 1))
        {
            sessao.RegistrarPresenca(vereador.Id, agora);
        }

        sessao.IncluirNaOrdemDoDia(proposicaoId);
        sessao.Abrir();

        sessoes.Adicionar(sessao);
        return sessao;
    }

    private Votacao SemearVotacao(
        Guid tenantId,
        Sessao sessao,
        Proposicao proposicao,
        List<Vereador> titulares,
        DateTimeOffset agora)
    {
        var presentes = sessao.Presencas.Count;
        var votacao = Votacao.Iniciar(
            tenantId,
            sessao.Id,
            proposicao.Id,
            TipoVotacao.Nominal,
            MaioriaExigida.Simples,
            sessao.TotalMembros,
            presentes,
            turno: 1);

        // Painel ao vivo (votacao ABERTA): votos parciais ja registrados, deixando alguns por votar.
        var votantes = titulares.Take(presentes).ToList();
        for (var i = 0; i < votantes.Count; i++)
        {
            var sentido = (i % 4) switch
            {
                3 => SentidoVoto.Nao,
                2 => SentidoVoto.Abstencao,
                _ => SentidoVoto.Sim,
            };

            votacao.RegistrarVoto(VotoId.New(), votantes[i].Id, sentido, agora.AddSeconds(i));
        }

        votacoes.Adicionar(votacao);
        return votacao;
    }
}
