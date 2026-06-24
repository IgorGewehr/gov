using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>
/// Materia submetida a apreciacao do Plenario da Camara Municipal (PLO, PLC, Emenda a LOM, PDL, PR,
/// Requerimento, Indicacao, Mocao). Raiz de agregado que percorre o ciclo de protocolo, distribuicao,
/// instrucao por Comissoes (pareceres), inclusao em Ordem do Dia, deliberacao, geracao de autografo e
/// remessa ao Executivo. Conformidade com CF/88 art. 29 (iniciativa e LOM), arts. 59-69 (simetria do
/// processo legislativo), a Lei Organica Municipal e o Regimento Interno. Mantem trilha de tramitacao
/// imutavel (append-only) para prova juridica e LAI.
/// </summary>
public sealed class Proposicao : AggregateRoot<ProposicaoId>, IMustHaveTenant
{
    private readonly List<Emenda> _emendas = [];
    private readonly List<Substitutivo> _substitutivos = [];
    private readonly List<Tramitacao> _tramitacoes = [];
    private readonly List<AprovacaoTurno> _aprovacoesTurno = [];

    private Proposicao()
    {
    }

    private Proposicao(
        ProposicaoId id,
        Guid tenantId,
        TipoProposicao tipo,
        Ementa ementa,
        Autoria autoria,
        RegimeTramitacao regime,
        string protocolo,
        DateOnly dataApresentacao)
        : base(id)
    {
        TenantId = tenantId;
        Tipo = tipo;
        Ementa = ementa;
        Autoria = autoria;
        Regime = regime;
        Protocolo = protocolo;
        DataApresentacao = dataApresentacao;
        Situacao = SituacaoProposicao.Apresentada;
        _tramitacoes.Add(Tramitacao.RegistrarFase(id, FaseTramitacao.Apresentacao, dataApresentacao));
        RaiseDomainEvent(new ProposicaoApresentada(id, tipo, autoria));
    }

    /// <summary>Tenant (Camara) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Especie da materia (imutavel apos a apresentacao) — define a maioria exigida.</summary>
    public TipoProposicao Tipo { get; private set; }

    /// <summary>Resumo do objeto da proposicao.</summary>
    public Ementa Ementa { get; private set; }

    /// <summary>Autoria (iniciativa) da proposicao.</summary>
    public Autoria Autoria { get; private set; }

    /// <summary>Regime (rito) de tramitacao.</summary>
    public RegimeTramitacao Regime { get; private set; }

    /// <summary>Numero de protocolo da proposicao no tenant.</summary>
    public string Protocolo { get; private set; } = default!;

    /// <summary>Data de apresentacao/protocolo.</summary>
    public DateOnly DataApresentacao { get; private set; }

    /// <summary>Situacao atual no ciclo de tramitacao.</summary>
    public SituacaoProposicao Situacao { get; private set; }

    /// <summary>Numero do autografo, quando gerado (nulo antes).</summary>
    public string? NumeroAutografo { get; private set; }

    /// <summary>
    /// Indica que um parecer CONTRARIO da CCJ (inconstitucionalidade) foi superado pelo Plenario
    /// (recurso), liberando a inclusao em Ordem do Dia apesar do vicio apontado (BUG-5).
    /// </summary>
    public bool ParecerContrarioCcjSuperado { get; private set; }

    /// <summary>Emendas apresentadas a proposicao.</summary>
    public IReadOnlyList<Emenda> Emendas => _emendas;

    /// <summary>Substitutivos apresentados a proposicao.</summary>
    public IReadOnlyList<Substitutivo> Substitutivos => _substitutivos;

    /// <summary>Fases de tramitacao (trilha imutavel, incluindo pareceres referenciados).</summary>
    public IReadOnlyList<Tramitacao> Tramitacoes => _tramitacoes;

    /// <summary>Aprovacoes de turno registradas (rito qualificado — Emenda a LOM), trilha imutavel.</summary>
    public IReadOnlyList<AprovacaoTurno> AprovacoesTurno => _aprovacoesTurno;

    /// <summary>Maioria exigida na deliberacao, derivada do <see cref="Tipo"/> (CF/88 art. 29).</summary>
    public MaioriaProposicao MaioriaExigida => Tipo switch
    {
        TipoProposicao.ProjetoDeLeiComplementar => MaioriaProposicao.Absoluta,
        TipoProposicao.ProjetoDeResolucao => MaioriaProposicao.Absoluta,
        TipoProposicao.EmendaALOM => MaioriaProposicao.Qualificada,
        _ => MaioriaProposicao.Simples,
    };

    /// <summary>
    /// Numero de turnos de votacao exigidos pela especie da materia. A Emenda a Lei Organica Municipal
    /// exige DOIS turnos (CF/88 art. 29, caput, e LOM — simetria do art. 60 §2º); as demais materias se
    /// deliberam em turno unico. Derivado do <see cref="Tipo"/> — fonte unica da regra de rito.
    /// </summary>
    public int TurnosExigidos => Tipo switch
    {
        TipoProposicao.EmendaALOM => DoisTurnos,
        _ => TurnoUnico,
    };

    /// <summary>Indica se a materia exige mais de um turno de votacao (rito qualificado).</summary>
    public bool ExigeDoisTurnos => TurnosExigidos > TurnoUnico;

    /// <summary>Quantidade de turnos ja aprovados (cada um com a maioria exigida, em datas distintas).</summary>
    public int TurnosAprovados => _aprovacoesTurno.Count;

    /// <summary>Indica se a proposicao esta em tramitacao (nao terminal e nao <see cref="SituacaoProposicao.AutografoEnviado"/>).</summary>
    public bool EmTramitacao
        => Situacao is not (SituacaoProposicao.Rejeitada
            or SituacaoProposicao.Arquivada
            or SituacaoProposicao.AutografoEnviado);

    /// <summary>Apresenta (protocola) uma nova proposicao — situacao inicial <see cref="SituacaoProposicao.Apresentada"/> (I-1, I-2).</summary>
    /// <param name="tenantId">Tenant (Camara) dono do registro.</param>
    /// <param name="tipo">Especie da materia.</param>
    /// <param name="ementa">Resumo do objeto (obrigatorio).</param>
    /// <param name="autoria">Autoria/iniciativa (obrigatoria).</param>
    /// <param name="regime">Regime de tramitacao.</param>
    /// <param name="protocolo">Numero de protocolo no tenant.</param>
    /// <param name="dataApresentacao">Data de apresentacao/protocolo.</param>
    /// <returns>Nova <see cref="Proposicao"/> valida.</returns>
    /// <exception cref="ArgumentException">Se o protocolo for vazio ou o tipo invalido.</exception>
    public static Proposicao Apresentar(
        Guid tenantId,
        TipoProposicao tipo,
        Ementa ementa,
        Autoria autoria,
        RegimeTramitacao regime,
        DateOnly dataApresentacao,
        string protocolo = "")
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de proposicao invalido.", nameof(tipo));
        }

        if (!Enum.IsDefined(regime))
        {
            throw new ArgumentException("Regime de tramitacao invalido.", nameof(regime));
        }

        var id = ProposicaoId.New();
        var numeroProtocolo = string.IsNullOrWhiteSpace(protocolo)
            ? id.Value.ToString("N")[..12].ToUpperInvariant()
            : protocolo.Trim();

        return new Proposicao(id, tenantId, tipo, ementa, autoria, regime, numeroProtocolo, dataApresentacao);
    }

    /// <summary>Distribui a proposicao as Comissoes para instrucao (I-3).</summary>
    /// <param name="data">Data da distribuicao.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoProposicao.Apresentada"/>.</exception>
    public void Distribuir(DateOnly data)
    {
        if (Situacao != SituacaoProposicao.Apresentada)
        {
            throw new InvalidOperationException($"A distribuicao so ocorre a partir de Apresentada. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoProposicao.Distribuida;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Distribuicao, data));
        RaiseDomainEvent(new ProposicaoDistribuida(Id));
    }

    /// <summary>Apresenta uma emenda pontual a proposicao em curso (I-4).</summary>
    /// <param name="texto">Texto da emenda.</param>
    /// <param name="autoria">Autoria da emenda.</param>
    /// <param name="data">Data de apresentacao.</param>
    /// <returns>Identificador da emenda criada.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a proposicao nao estiver em tramitacao.</exception>
    public EmendaId ApresentarEmenda(string texto, Autoria autoria, DateOnly data)
    {
        GarantirEmTramitacao();
        var emenda = Emenda.Registrar(Id, texto, autoria, data);
        _emendas.Add(emenda);
        RaiseDomainEvent(new EmendaApresentada(Id, emenda.Id));
        return emenda.Id;
    }

    /// <summary>Apresenta um substitutivo (modificacao integral) a proposicao em curso (I-4).</summary>
    /// <param name="texto">Texto integral do substitutivo.</param>
    /// <param name="autoria">Autoria do substitutivo.</param>
    /// <param name="data">Data de apresentacao.</param>
    /// <returns>Identificador (correlato de emenda) emitido no evento.</returns>
    /// <exception cref="ArgumentException">Se o texto for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a proposicao nao estiver em tramitacao.</exception>
    public EmendaId ApresentarSubstitutivo(string texto, Autoria autoria, DateOnly data)
    {
        GarantirEmTramitacao();
        var substitutivo = Substitutivo.Registrar(Id, texto, autoria, data);
        _substitutivos.Add(substitutivo);
        var emendaId = new EmendaId(substitutivo.Id.Value);
        RaiseDomainEvent(new EmendaApresentada(Id, emendaId));
        return emendaId;
    }

    /// <summary>Registra o parecer de uma Comissao na trilha imutavel (I-5).</summary>
    /// <param name="comissao">Comissao emitente (ex.: Ccj, FinancasOrcamento).</param>
    /// <param name="favoravel">Sentido do parecer.</param>
    /// <param name="data">Data do parecer.</param>
    /// <exception cref="ArgumentException">Se a comissao for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se a proposicao nao estiver em tramitacao.</exception>
    public void RegistrarParecer(string comissao, bool favoravel, DateOnly data)
    {
        GarantirEmTramitacao();
        ArgumentException.ThrowIfNullOrWhiteSpace(comissao);
        _tramitacoes.Add(Tramitacao.RegistrarParecer(Id, comissao, favoravel, data));
        RaiseDomainEvent(new ParecerEmitido(Id, comissao.Trim(), favoravel));
    }

    /// <summary>Inclui a proposicao em Ordem do Dia (I-6, I-7), exigindo pareceres obrigatorios.</summary>
    /// <param name="data">Data da inclusao.</param>
    /// <exception cref="InvalidOperationException">
    /// Se a situacao nao for <see cref="SituacaoProposicao.Distribuida"/>, ou por vicio de tramitacao
    /// (ausencia de parecer da CCJ ou de Financas e Orcamento).
    /// </exception>
    public void IncluirEmOrdemDoDia(DateOnly data)
    {
        if (Situacao != SituacaoProposicao.Distribuida)
        {
            throw new InvalidOperationException($"A inclusao em Ordem do Dia exige situacao Distribuida. Situacao atual: {Situacao}.");
        }

        // I-6: pareceres obrigatorios (CCJ e Financas/Orcamento) — ausencia vicia a tramitacao.
        if (!PossuiParecer(ComissaoCcj))
        {
            throw new InvalidOperationException("Vicio de tramitacao: ausencia de parecer da CCJ (constitucionalidade).");
        }

        if (!PossuiParecer(ComissaoFinancasOrcamento))
        {
            throw new InvalidOperationException("Vicio de tramitacao: ausencia de parecer de Financas e Orcamento.");
        }

        // BUG-5: parecer CONTRARIO da CCJ (inconstitucionalidade) e impedimento juridico — nao basta
        // "existir" um parecer. A materia so segue a Ordem do Dia com a superacao explicita do parecer
        // (recurso ao Plenario), via SuperarParecerContrarioCcj. Sem isso, o vicio fica silenciado.
        if (PossuiParecerContrarioCcj() && !ParecerContrarioCcjSuperado)
        {
            throw new InvalidOperationException(
                "Vicio de tramitacao: parecer da CCJ pela inconstitucionalidade nao superado pelo Plenario.");
        }

        Situacao = SituacaoProposicao.EmOrdemDoDia;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.OrdemDoDia, data));
    }

    /// <summary>
    /// Aprova um TURNO de votacao da proposicao conforme a maioria exigida pelo <see cref="Tipo"/> (I-8).
    /// Materias de turno unico sao aprovadas no primeiro (e unico) turno favoravel. Materias de rito
    /// qualificado (Emenda a LOM) so transitam para <see cref="SituacaoProposicao.Aprovada"/> apos DOIS
    /// turnos favoraveis, cada qual com a maioria exigida, observado o <paramref name="intersticio"/>
    /// minimo entre eles (CF/88 art. 29, caput; simetria do art. 60 §2º). O <paramref name="turno"/> deixa
    /// de ser dado morto: e consumido e materializado em <see cref="AprovacoesTurno"/>. Ate concluir todos
    /// os turnos, a proposicao permanece em Ordem do Dia aguardando o turno seguinte.
    /// </summary>
    /// <param name="resultado">Resultado da deliberacao (maioria efetivamente atingida).</param>
    /// <param name="turno">Numero do turno deliberado (1 ou 2), proveniente da votacao apurada.</param>
    /// <param name="intersticio">Intervalo minimo (parametrizavel por tenant) exigido entre turnos.</param>
    /// <param name="data">Data da deliberacao.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se o <paramref name="turno"/> nao for 1 ou 2.</exception>
    /// <exception cref="InvalidOperationException">
    /// Se a situacao nao for <see cref="SituacaoProposicao.EmOrdemDoDia"/>; se a maioria exigida nao for
    /// atingida; se o turno violar a sequencia (turno fora de ordem, turno repetido, turno alem do exigido);
    /// ou se o intersticio minimo nao for observado entre o turno anterior e este.
    /// </exception>
    public void Aprovar(ResultadoDeliberacao resultado, int turno, Interstico intersticio, DateOnly data)
    {
        if (Situacao != SituacaoProposicao.EmOrdemDoDia)
        {
            throw new InvalidOperationException($"A aprovacao exige situacao EmOrdemDoDia. Situacao atual: {Situacao}.");
        }

        if (turno is not (PrimeiroTurno or SegundoTurno))
        {
            throw new ArgumentOutOfRangeException(nameof(turno), turno, "Turno deve ser 1 ou 2.");
        }

        // A maioria exigida pelo tipo tem de ser atingida em CADA turno (nao basta no turno final).
        if (!resultado.Satisfaz(MaioriaExigida))
        {
            throw new InvalidOperationException(
                $"Maioria exigida ({MaioriaExigida}) nao atingida no turno {turno} para o tipo {Tipo}.");
        }

        // O turno informado nao pode exceder o numero de turnos exigidos pela especie (ex.: turno 2 num
        // PLO de turno unico e estado impossivel de rito).
        if (turno > TurnosExigidos)
        {
            throw new InvalidOperationException(
                $"O tipo {Tipo} exige {TurnosExigidos} turno(s); turno {turno} nao se aplica.");
        }

        // L-1: o turno tem de ser o IMEDIATAMENTE seguinte ao ultimo aprovado (turno 2 sem o 1, ou um turno
        // ja vencido reaberto, fabricaria o rito).
        var turnoEsperado = TurnosAprovados + 1;
        if (turno != turnoEsperado)
        {
            throw new InvalidOperationException(
                $"Turno fora de sequencia: esperado turno {turnoEsperado}, recebido turno {turno}.");
        }

        // L-1: entre turnos deve transcorrer o intersticio minimo do Regimento (parametrizavel); aprovar os
        // dois turnos no mesmo dia (ou abaixo do intervalo) e vicio de rito.
        if (TurnosAprovados > 0)
        {
            var dataTurnoAnterior = _aprovacoesTurno[^1].Data;
            if (!intersticio.Respeitado(dataTurnoAnterior, data))
            {
                throw new InvalidOperationException(
                    $"Intersticio minimo de {intersticio.Dias} dia(s) entre turnos nao observado "
                    + $"(turno anterior em {dataTurnoAnterior:yyyy-MM-dd}, turno atual em {data:yyyy-MM-dd}).");
            }
        }

        _aprovacoesTurno.Add(AprovacaoTurno.Registrar(Id, turno, resultado.MaioriaAtingida, data));

        // So apos concluir TODOS os turnos exigidos a materia esta efetivamente aprovada. Antes disso,
        // permanece em Ordem do Dia aguardando o turno seguinte (sinalizado por TurnoAprovado).
        if (TurnosAprovados < TurnosExigidos)
        {
            RaiseDomainEvent(new TurnoAprovado(Id, turno, TurnosExigidos));
            return;
        }

        Situacao = SituacaoProposicao.Aprovada;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Aprovacao, data));
        RaiseDomainEvent(new ProposicaoAprovada(Id));
    }

    /// <summary>Rejeita a proposicao em Plenario (terminal) — I-9.</summary>
    /// <param name="data">Data da deliberacao.</param>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoProposicao.EmOrdemDoDia"/>.</exception>
    public void Rejeitar(DateOnly data)
    {
        if (Situacao != SituacaoProposicao.EmOrdemDoDia)
        {
            throw new InvalidOperationException($"A rejeicao exige situacao EmOrdemDoDia. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoProposicao.Rejeitada;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Rejeicao, data));
        RaiseDomainEvent(new ProposicaoRejeitada(Id));
    }

    /// <summary>Gera o autografo e o remete ao Executivo (assinado ICP-Brasil) — I-10.</summary>
    /// <param name="numeroAutografo">Numero do autografo (nao vazio).</param>
    /// <param name="data">Data da geracao/remessa.</param>
    /// <exception cref="ArgumentException">Se o numero do autografo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a situacao nao for <see cref="SituacaoProposicao.Aprovada"/>.</exception>
    public void GerarAutografo(string numeroAutografo, DateOnly data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroAutografo);
        if (Situacao != SituacaoProposicao.Aprovada)
        {
            throw new InvalidOperationException($"A geracao de autografo exige situacao Aprovada. Situacao atual: {Situacao}.");
        }

        NumeroAutografo = numeroAutografo.Trim();
        Situacao = SituacaoProposicao.AutografoEnviado;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Autografo, data));
        RaiseDomainEvent(new AutografoEnviado(Id, NumeroAutografo));
    }

    /// <summary>Arquiva a proposicao (terminal), permitido apenas sobre proposicao nao terminal (I-11).</summary>
    /// <param name="data">Data do arquivamento.</param>
    /// <exception cref="InvalidOperationException">Se a proposicao ja estiver em estado terminal.</exception>
    public void Arquivar(DateOnly data)
    {
        if (Situacao is SituacaoProposicao.Rejeitada or SituacaoProposicao.Arquivada)
        {
            throw new InvalidOperationException($"Proposicao terminal nao admite arquivamento. Situacao atual: {Situacao}.");
        }

        Situacao = SituacaoProposicao.Arquivada;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Arquivamento, data));
        RaiseDomainEvent(new ProposicaoArquivada(Id));
    }

    /// <summary>Registra a sancao recebida do Executivo na trilha (I-13), apenas em <see cref="SituacaoProposicao.AutografoEnviado"/>.</summary>
    /// <param name="data">Data da sancao.</param>
    /// <returns><c>true</c> se a sancao foi registrada; <c>false</c> se ignorada (situacao incompativel).</returns>
    public bool RegistrarSancao(DateOnly data)
    {
        // I-13/B-12: aplica-se apenas a proposicoes em AutografoEnviado; caso contrario ignorado/auditado.
        if (Situacao != SituacaoProposicao.AutografoEnviado)
        {
            return false;
        }

        // Sancao e veto sao a decisao FINAL e UNICA do Executivo: nao se registra "Sancao, Veto, Sancao".
        if (PossuiDecisaoDoExecutivo())
        {
            return false;
        }

        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Sancao, data));
        return true;
    }

    /// <summary>Registra o veto recebido do Executivo na trilha (I-13), apenas em <see cref="SituacaoProposicao.AutografoEnviado"/>.</summary>
    /// <param name="data">Data do veto.</param>
    /// <returns><c>true</c> se o veto foi registrado; <c>false</c> se ignorado (situacao incompativel).</returns>
    public bool RegistrarVeto(DateOnly data)
    {
        if (Situacao != SituacaoProposicao.AutografoEnviado)
        {
            return false;
        }

        // Sancao e veto sao a decisao FINAL e UNICA do Executivo: veto apos sancao (ou novo veto) e recusado.
        if (PossuiDecisaoDoExecutivo())
        {
            return false;
        }

        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Veto, data));
        return true;
    }

    // A decisao do Executivo (sancao OU veto) ja consta na trilha — e final e unica.
    private bool PossuiDecisaoDoExecutivo()
        => _tramitacoes.Any(tramitacao =>
            tramitacao.Fase is FaseTramitacao.Sancao or FaseTramitacao.Veto);

    /// <summary>
    /// Supera (por recurso ao Plenario) o parecer contrario da CCJ pela inconstitucionalidade,
    /// permitindo a inclusao em Ordem do Dia (BUG-5). So aplicavel quando ha parecer contrario da CCJ
    /// e a proposicao ainda esta em tramitacao.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se nao houver parecer contrario da CCJ a superar, ou a proposicao nao estiver em tramitacao.</exception>
    public void SuperarParecerContrarioCcj()
    {
        GarantirEmTramitacao();
        if (!PossuiParecerContrarioCcj())
        {
            throw new InvalidOperationException("Nao ha parecer contrario da CCJ a superar.");
        }

        ParecerContrarioCcjSuperado = true;
    }

    private bool PossuiParecer(string comissao)
        => _tramitacoes.Any(tramitacao =>
            tramitacao.Fase == FaseTramitacao.Parecer
            && string.Equals(tramitacao.Comissao, comissao, StringComparison.OrdinalIgnoreCase));

    // BUG-5: o sentido vigente do parecer da CCJ e o do ULTIMO parecer emitido pela comissao (a CCJ
    // pode reanalisar). Contrario => impedimento de constitucionalidade.
    private bool PossuiParecerContrarioCcj()
    {
        var ultimoCcj = _tramitacoes
            .Where(tramitacao =>
                tramitacao.Fase == FaseTramitacao.Parecer
                && string.Equals(tramitacao.Comissao, ComissaoCcj, StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();

        return ultimoCcj is not null && ultimoCcj.ParecerFavoravel == false;
    }

    private void GarantirEmTramitacao()
    {
        if (!EmTramitacao)
        {
            throw new InvalidOperationException($"Proposicao nao esta em tramitacao. Situacao atual: {Situacao}.");
        }
    }

    // Turnos de votacao: materias comuns em turno unico; Emenda a LOM em dois turnos (CF/88 art. 29, caput).
    private const int TurnoUnico = 1;
    private const int DoisTurnos = 2;
    private const int PrimeiroTurno = 1;
    private const int SegundoTurno = 2;

    /// <summary>Identificador da Comissao de Constituicao e Justica (parecer de constitucionalidade obrigatorio).</summary>
    public const string ComissaoCcj = "Ccj";

    /// <summary>Identificador da Comissao de Financas e Orcamento (parecer financeiro/orcamentario obrigatorio).</summary>
    public const string ComissaoFinancasOrcamento = "FinancasOrcamento";
}
