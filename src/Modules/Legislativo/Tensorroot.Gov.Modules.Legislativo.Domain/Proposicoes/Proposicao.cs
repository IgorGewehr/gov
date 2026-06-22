using Tensorroot.Gov.Modules.Legislativo.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Legislativo.Domain.Proposicoes;

/// <summary>Identificador forte do agregado <see cref="Proposicao"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ProposicaoId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ProposicaoId"/>.</returns>
    public static ProposicaoId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Maioria exigida pela especie da materia (CF/88 art. 29) — derivada do <see cref="TipoProposicao"/>.</summary>
public enum MaioriaProposicao
{
    /// <summary>Maioria simples: maior que 50% dos presentes (lei ordinaria).</summary>
    Simples = 1,

    /// <summary>Maioria absoluta: maior que 50% dos membros (LC, Regimento, derrubada de veto).</summary>
    Absoluta = 2,

    /// <summary>Maioria qualificada: 2/3 em dois turnos (Emenda a LOM).</summary>
    Qualificada = 3,
}

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

    /// <summary>Emendas apresentadas a proposicao.</summary>
    public IReadOnlyList<Emenda> Emendas => _emendas;

    /// <summary>Substitutivos apresentados a proposicao.</summary>
    public IReadOnlyList<Substitutivo> Substitutivos => _substitutivos;

    /// <summary>Fases de tramitacao (trilha imutavel, incluindo pareceres referenciados).</summary>
    public IReadOnlyList<Tramitacao> Tramitacoes => _tramitacoes;

    /// <summary>Maioria exigida na deliberacao, derivada do <see cref="Tipo"/> (CF/88 art. 29).</summary>
    public MaioriaProposicao MaioriaExigida => Tipo switch
    {
        TipoProposicao.ProjetoDeLeiComplementar => MaioriaProposicao.Absoluta,
        TipoProposicao.ProjetoDeResolucao => MaioriaProposicao.Absoluta,
        TipoProposicao.EmendaALOM => MaioriaProposicao.Qualificada,
        _ => MaioriaProposicao.Simples,
    };

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

        Situacao = SituacaoProposicao.EmOrdemDoDia;
        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.OrdemDoDia, data));
    }

    /// <summary>Aprova a proposicao conforme a maioria exigida pelo <see cref="Tipo"/> (I-8).</summary>
    /// <param name="resultado">Resultado da deliberacao (maioria efetivamente atingida).</param>
    /// <param name="data">Data da deliberacao.</param>
    /// <exception cref="InvalidOperationException">
    /// Se a situacao nao for <see cref="SituacaoProposicao.EmOrdemDoDia"/> ou a maioria exigida nao for atingida.
    /// </exception>
    public void Aprovar(ResultadoDeliberacao resultado, DateOnly data)
    {
        if (Situacao != SituacaoProposicao.EmOrdemDoDia)
        {
            throw new InvalidOperationException($"A aprovacao exige situacao EmOrdemDoDia. Situacao atual: {Situacao}.");
        }

        if (!resultado.Satisfaz(MaioriaExigida))
        {
            throw new InvalidOperationException(
                $"Maioria exigida ({MaioriaExigida}) nao atingida para o tipo {Tipo}.");
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

        _tramitacoes.Add(Tramitacao.RegistrarFase(Id, FaseTramitacao.Veto, data));
        return true;
    }

    private bool PossuiParecer(string comissao)
        => _tramitacoes.Any(tramitacao =>
            tramitacao.Fase == FaseTramitacao.Parecer
            && string.Equals(tramitacao.Comissao, comissao, StringComparison.OrdinalIgnoreCase));

    private void GarantirEmTramitacao()
    {
        if (!EmTramitacao)
        {
            throw new InvalidOperationException($"Proposicao nao esta em tramitacao. Situacao atual: {Situacao}.");
        }
    }

    /// <summary>Identificador da Comissao de Constituicao e Justica (parecer de constitucionalidade obrigatorio).</summary>
    public const string ComissaoCcj = "Ccj";

    /// <summary>Identificador da Comissao de Financas e Orcamento (parecer financeiro/orcamentario obrigatorio).</summary>
    public const string ComissaoFinancasOrcamento = "FinancasOrcamento";
}

/// <summary>
/// Resultado de uma deliberacao plenaria: indica se a materia foi aprovada e qual a maior
/// maioria efetivamente alcancada na votacao. Alimenta <see cref="Proposicao.Aprovar"/>,
/// que confronta a maioria atingida com a exigida pelo tipo (I-8).
/// </summary>
public readonly record struct ResultadoDeliberacao
{
    private ResultadoDeliberacao(bool aprovado, MaioriaProposicao maioriaAtingida)
    {
        Aprovado = aprovado;
        MaioriaAtingida = maioriaAtingida;
    }

    /// <summary>Indica se a deliberacao aprovou a materia.</summary>
    public bool Aprovado { get; }

    /// <summary>Maior maioria efetivamente atingida na votacao.</summary>
    public MaioriaProposicao MaioriaAtingida { get; }

    /// <summary>Cria um resultado aprovado com a maioria efetivamente atingida.</summary>
    /// <param name="maioriaAtingida">Maioria alcancada na votacao.</param>
    /// <returns>Resultado aprovado.</returns>
    public static ResultadoDeliberacao Aprovada(MaioriaProposicao maioriaAtingida)
        => new(aprovado: true, maioriaAtingida);

    /// <summary>Cria um resultado rejeitado (maioria nao alcancada).</summary>
    /// <returns>Resultado rejeitado.</returns>
    public static ResultadoDeliberacao Rejeitada()
        => new(aprovado: false, MaioriaProposicao.Simples);

    /// <summary>Verifica se o resultado satisfaz a maioria exigida pelo tipo da proposicao.</summary>
    /// <param name="exigida">Maioria exigida.</param>
    /// <returns><c>true</c> se aprovado e a maioria atingida for igual ou superior a exigida.</returns>
    public bool Satisfaz(MaioriaProposicao exigida)
        => Aprovado && MaioriaAtingida >= exigida;
}
