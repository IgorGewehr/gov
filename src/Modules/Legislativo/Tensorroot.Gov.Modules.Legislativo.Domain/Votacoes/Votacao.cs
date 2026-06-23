using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;
using ProposicaoId = Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes.ProposicaoId;
using SessaoId = Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes.SessaoId;
using VereadorId = Tensorroot.Gov.Modules.Legislativo.Domain.Sessoes.VereadorId;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Votacoes;

/// <summary>
/// Deliberacao do Plenario sobre uma materia (proposicao, veto, parecer), apurada por modalidade
/// simbolica, nominal ou secreta. Aplica a maioria exigida (simples, absoluta ou qualificada)
/// conforme a especie e produz um <see cref="ResultadoVotacao"/> que alimenta a tramitacao da
/// proposicao. Registra cada <see cref="Voto"/> de forma idempotente (painel eletronico) e mantem
/// trilha imutavel para prova juridica e LAI (CF/88 art. 29; Lei Organica Municipal; Regimento Interno).
/// </summary>
public sealed class Votacao : AggregateRoot<VotacaoId>, IMustHaveTenant
{
    private readonly List<Voto> _votos = [];

    private Votacao()
    {
    }

    private Votacao(
        VotacaoId id,
        Guid tenantId,
        SessaoId sessaoId,
        ProposicaoId proposicaoId,
        TipoVotacao tipo,
        MaioriaExigida maioriaExigida,
        int totalMembros,
        int presentes,
        int turno)
        : base(id)
    {
        TenantId = tenantId;
        SessaoId = sessaoId;
        ProposicaoId = proposicaoId;
        Tipo = tipo;
        MaioriaExigida = maioriaExigida;
        TotalMembros = totalMembros;
        Presentes = presentes;
        Turno = turno;
        Situacao = SituacaoVotacao.Aberta;
        Resultado = null;
        RaiseDomainEvent(new VotacaoIniciada(id, proposicaoId, tipo, maioriaExigida));
    }

    /// <summary>Tenant (Camara) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Sessao em que ocorre a votacao.</summary>
    public SessaoId SessaoId { get; private set; }

    /// <summary>Materia (proposicao) votada.</summary>
    public ProposicaoId ProposicaoId { get; private set; }

    /// <summary>Modalidade de apuracao (simbolica / nominal / secreta).</summary>
    public TipoVotacao Tipo { get; private set; }

    /// <summary>Criterio de aprovacao exigido (simples / absoluta / qualificada).</summary>
    public MaioriaExigida MaioriaExigida { get; private set; }

    /// <summary>Numero de vereadores da Camara (base da maioria absoluta).</summary>
    public int TotalMembros { get; private set; }

    /// <summary>Numero de vereadores presentes (base da maioria simples).</summary>
    public int Presentes { get; private set; }

    /// <summary>Turno da votacao (1 ou 2; qualificada exige 2 turnos).</summary>
    public int Turno { get; private set; }

    /// <summary>Situacao atual na maquina de estados.</summary>
    public SituacaoVotacao Situacao { get; private set; }

    /// <summary>Resultado apurado (nulo enquanto <see cref="SituacaoVotacao.Aberta"/> — I-9).</summary>
    public ResultadoVotacao? Resultado { get; private set; }

    /// <summary>Votos registrados (trilha imutavel, append-only — I-11).</summary>
    public IReadOnlyList<Voto> Votos => _votos;

    /// <summary>Quantidade de votos no sentido <see cref="SentidoVoto.Sim"/>.</summary>
    public int VotosSim => _votos.Count(voto => voto.Sentido == SentidoVoto.Sim);

    /// <summary>Quantidade de votos no sentido <see cref="SentidoVoto.Nao"/>.</summary>
    public int VotosNao => _votos.Count(voto => voto.Sentido == SentidoVoto.Nao);

    /// <summary>Quantidade de abstencoes (nao compoem o numerador da maioria — I-13).</summary>
    public int Abstencoes => _votos.Count(voto => voto.Sentido == SentidoVoto.Abstencao);

    /// <summary>
    /// Quorum minimo de deliberacao = maioria absoluta dos membros (<c>TotalMembros / 2 + 1</c>),
    /// o mesmo quorum de instalacao da sessao. Abaixo dele a votacao e <see cref="ResultadoVotacao.Prejudicado"/>.
    /// </summary>
    public int QuorumMinimo => (TotalMembros / 2) + 1;

    /// <summary>
    /// Abre uma nova votacao para registro de votos (situacao inicial <see cref="SituacaoVotacao.Aberta"/>).
    /// Emite <see cref="VotacaoIniciada"/> (I-1).
    /// </summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="sessaoId">Sessao em que ocorre a votacao.</param>
    /// <param name="proposicaoId">Materia (proposicao) votada.</param>
    /// <param name="tipo">Modalidade de apuracao.</param>
    /// <param name="maioriaExigida">Criterio de aprovacao exigido.</param>
    /// <param name="totalMembros">Numero de vereadores da Camara (maior que zero).</param>
    /// <param name="presentes">Numero de vereadores presentes (maior que zero).</param>
    /// <param name="turno">Turno da votacao (1 ou 2).</param>
    /// <returns>Nova <see cref="Votacao"/> em situacao <see cref="SituacaoVotacao.Aberta"/>.</returns>
    /// <exception cref="ArgumentException">Se o tipo ou a maioria forem invalidos, ou o turno nao for 1 ou 2.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se <paramref name="totalMembros"/> ou <paramref name="presentes"/> nao forem positivos.</exception>
    public static Votacao Iniciar(
        Guid tenantId,
        SessaoId sessaoId,
        ProposicaoId proposicaoId,
        TipoVotacao tipo,
        MaioriaExigida maioriaExigida,
        int totalMembros,
        int presentes,
        int turno)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalMembros);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(presentes);

        // BUG-8(a): estado fisicamente impossivel — nao podem comparecer mais vereadores do que a
        // composicao da Camara. Aceitar presentes > totalMembros contamina as bases das maiorias.
        if (presentes > totalMembros)
        {
            throw new ArgumentOutOfRangeException(
                nameof(presentes),
                presentes,
                $"Presentes ({presentes}) nao pode exceder o total de membros ({totalMembros}).");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de votacao invalido.", nameof(tipo));
        }

        if (!Enum.IsDefined(maioriaExigida))
        {
            throw new ArgumentException("Maioria exigida invalida.", nameof(maioriaExigida));
        }

        if (turno is not (1 or 2))
        {
            throw new ArgumentException("Turno deve ser 1 ou 2.", nameof(turno));
        }

        return new Votacao(
            VotacaoId.New(),
            tenantId,
            sessaoId,
            proposicaoId,
            tipo,
            maioriaExigida,
            totalMembros,
            presentes,
            turno);
    }

    /// <summary>
    /// Registra um voto na trilha imutavel. Idempotente por <paramref name="votoId"/> (I-3):
    /// o reenvio do mesmo <see cref="VotoId"/> e no-op (sem novo voto nem novo evento). Em votacao
    /// nominal, cada vereador vota uma unica vez (I-4). Emite <see cref="VotoRegistrado"/>.
    /// </summary>
    /// <param name="votoId">Identificador do voto (origem do painel, chave de idempotencia).</param>
    /// <param name="vereadorId">Vereador autor do voto.</param>
    /// <param name="sentido">Sentido do voto.</param>
    /// <param name="registradoEm">Momento do registro.</param>
    /// <exception cref="ArgumentException">Se o sentido for invalido.</exception>
    /// <exception cref="InvalidOperationException">Se a votacao nao estiver aberta (I-2/I-10) ou o vereador ja votou em nominal (I-4).</exception>
    public void RegistrarVoto(VotoId votoId, VereadorId vereadorId, SentidoVoto sentido, DateTimeOffset registradoEm)
    {
        if (!Enum.IsDefined(sentido))
        {
            throw new ArgumentException("Sentido do voto invalido.", nameof(sentido));
        }

        if (Situacao != SituacaoVotacao.Aberta)
        {
            throw new InvalidOperationException($"Votos so podem ser registrados com a votacao Aberta. Situacao atual: {Situacao}.");
        }

        // I-3: idempotencia por votoId — reenvio do mesmo voto e no-op.
        if (_votos.Any(voto => voto.Id == votoId))
        {
            return;
        }

        // I-4 / BUG-2: um vereador, um voto — em TODA modalidade (nominal, simbolica e secreta).
        // O sigilo da secreta nao implica permitir repeticao: a identidade existe internamente para
        // controle de unicidade, apenas nao e projetada no painel/lista. Permitir o mesmo VereadorId
        // votar N vezes infla VotosSim e fabrica aprovacao.
        if (_votos.Any(voto => voto.VereadorId == vereadorId))
        {
            throw new InvalidOperationException("O vereador ja votou nesta votacao.");
        }

        // BUG-7(a): teto de votos — o numero de votos nao pode exceder os presentes (logo, tambem nao
        // excede o total de membros, pois presentes <= totalMembros). "13 votos numa Camara de 11" e
        // estado impossivel que fabrica quorum/maioria.
        if (_votos.Count >= Presentes)
        {
            throw new InvalidOperationException(
                $"Total de votos ({_votos.Count}) atingiu o numero de presentes ({Presentes}); nao ha mais votos a registrar.");
        }

        _votos.Add(Voto.Registrar(votoId, vereadorId, sentido, registradoEm));
        RaiseDomainEvent(new VotoRegistrado(Id, votoId));
    }

    /// <summary>
    /// Encerra a votacao, apura o <see cref="ResultadoVotacao"/> conforme a maioria exigida,
    /// transita para <see cref="SituacaoVotacao.Encerrada"/> (terminal) e emite
    /// <see cref="VotacaoEncerrada"/> (I-5).
    /// </summary>
    /// <returns>Resultado apurado da votacao.</returns>
    /// <exception cref="InvalidOperationException">Se a votacao nao estiver aberta (I-5/I-10).</exception>
    public ResultadoVotacao Encerrar()
    {
        if (Situacao != SituacaoVotacao.Aberta)
        {
            throw new InvalidOperationException($"O encerramento so ocorre com a votacao Aberta. Situacao atual: {Situacao}.");
        }

        var resultado = Apurar();
        Resultado = resultado;
        Situacao = SituacaoVotacao.Encerrada;
        RaiseDomainEvent(new VotacaoEncerrada(Id, resultado));
        return resultado;
    }

    /// <summary>Cancela a votacao (terminal), sem apuracao de resultado (I-10).</summary>
    /// <exception cref="InvalidOperationException">Se a votacao nao estiver aberta.</exception>
    public void Cancelar()
    {
        if (Situacao != SituacaoVotacao.Aberta)
        {
            throw new InvalidOperationException($"O cancelamento so ocorre com a votacao Aberta. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoVotacao.Cancelada;
    }

    /// <summary>
    /// Indica se os votos <see cref="SentidoVoto.Sim"/> satisfazem a maioria informada.
    /// Abstencoes nao compoem o numerador (I-13).
    /// </summary>
    /// <param name="maioria">Criterio de maioria a verificar.</param>
    /// <returns><c>true</c> se a maioria foi atingida.</returns>
    public bool AtingeMaioria(MaioriaExigida maioria) => maioria switch
    {
        // I-6: maioria simples — maior que 50% dos presentes.
        MaioriaExigida.Simples => VotosSim > Presentes / 2,

        // I-7: maioria absoluta — maior que 50% dos membros.
        MaioriaExigida.Absoluta => VotosSim >= (TotalMembros / 2) + 1,

        // I-8: maioria qualificada — 2/3 dos membros (aprovacao final exige 2 turnos, externos a esta votacao).
        MaioriaExigida.Qualificada => VotosSim >= (int)Math.Ceiling(2.0 * TotalMembros / 3.0),

        _ => false,
    };

    /// <summary>
    /// Maior maioria efetivamente atingida pelo placar de Sim (Qualificada &gt; Absoluta &gt; Simples),
    /// ou <c>null</c> se nem a maioria simples foi alcancada. Fonte unica para o vinculo
    /// votacao -> proposicao (BUG-1): a aprovacao da proposicao usa a maioria ATINGIDA, nao a exigida.
    /// </summary>
    /// <returns>Maior maioria atingida, ou <c>null</c> se nenhuma.</returns>
    public MaioriaExigida? MaioriaAtingida()
    {
        if (AtingeMaioria(MaioriaExigida.Qualificada))
        {
            return MaioriaExigida.Qualificada;
        }

        if (AtingeMaioria(MaioriaExigida.Absoluta))
        {
            return MaioriaExigida.Absoluta;
        }

        if (AtingeMaioria(MaioriaExigida.Simples))
        {
            return MaioriaExigida.Simples;
        }

        return null;
    }

    /// <summary>
    /// Aplica a maioria exigida sobre os votos <see cref="SentidoVoto.Sim"/> e retorna o resultado.
    /// Abstencoes nao compoem o numerador (I-13). Sem o quorum minimo de deliberacao, a votacao e
    /// <see cref="ResultadoVotacao.Prejudicado"/> (BUG-7(b)).
    /// </summary>
    /// <returns>Resultado apurado.</returns>
    private ResultadoVotacao Apurar()
    {
        // BUG-7(b): quorum de deliberacao = maioria absoluta dos membros. Sem ele, nao se delibera o
        // merito — evita aprovar/rejeitar contradizendo o painel (que ja exibe QuorumAtingido=false).
        if (Presentes < QuorumMinimo)
        {
            return ResultadoVotacao.Prejudicado;
        }

        return AtingeMaioria(MaioriaExigida) ? ResultadoVotacao.Aprovado : ResultadoVotacao.Rejeitado;
    }
}
