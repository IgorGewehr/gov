using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;

/// <summary>
/// Lei de Diretrizes Orçamentárias (LDO — CF 165 §2º; LRF arts. 4º-5º): o "elo" anual entre PPA e LOA.
/// Seleciona prioridades do PPA, fixa metas fiscais do triênio e carrega os anexos da LRF (AMF/ARF).
/// Só a LDO <see cref="SituacaoLdo.Vigente"/> habilita a LOA do exercício.
/// </summary>
public sealed class LeiDiretrizes : AggregateRoot<LdoId>, IMustHaveTenant
{
    /// <summary>Quantidade de anos cobertos pelas metas fiscais (LRF art. 4º §1º): exercício + 2.</summary>
    public const int AnosMetaFiscal = 3;

    private readonly List<PrioridadeLdo> _prioridades = [];
    private readonly List<MetaFiscal> _metasFiscais = [];
    private readonly List<AnexoLdo> _anexos = [];

    private LeiDiretrizes()
    {
    }

    private LeiDiretrizes(LdoId id, Guid tenantId, int exercicio, PpaId ppaId, string numeroLei, int anoLei)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
        PpaId = ppaId;
        NumeroLei = numeroLei;
        AnoLei = anoLei;
        Situacao = SituacaoLdo.Elaboracao;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício de referência da LDO.</summary>
    public int Exercicio { get; private set; }

    /// <summary>PPA vigente ao qual a LDO se vincula.</summary>
    public PpaId PpaId { get; private set; }

    /// <summary>Número da lei da LDO.</summary>
    public string NumeroLei { get; private set; } = default!;

    /// <summary>Ano da lei da LDO.</summary>
    public int AnoLei { get; private set; }

    /// <summary>Situação (máquina de estados).</summary>
    public SituacaoLdo Situacao { get; private set; }

    /// <summary>Prioridades selecionadas do PPA.</summary>
    public IReadOnlyCollection<PrioridadeLdo> Prioridades => _prioridades.AsReadOnly();

    /// <summary>Metas fiscais do triênio.</summary>
    public IReadOnlyCollection<MetaFiscal> MetasFiscais => _metasFiscais.AsReadOnly();

    /// <summary>Anexos da LRF (AMF/ARF).</summary>
    public IReadOnlyCollection<AnexoLdo> Anexos => _anexos.AsReadOnly();

    /// <summary>
    /// Cria uma LDO em elaboração, vinculada a um PPA vigente que cobre o exercício.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício da LDO.</param>
    /// <param name="ppa">PPA vigente de referência.</param>
    /// <param name="numeroLei">Número/identificação da lei.</param>
    /// <param name="anoLei">Ano da lei.</param>
    /// <returns>Nova <see cref="LeiDiretrizes"/>.</returns>
    /// <exception cref="InvalidOperationException">Se o PPA não estiver vigente ou não cobrir o exercício.</exception>
    public static LeiDiretrizes Criar(Guid tenantId, int exercicio, PlanoPlurianual ppa, string numeroLei, int anoLei)
    {
        ArgumentNullException.ThrowIfNull(ppa);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLei);
        if (ppa.Situacao != SituacaoPpa.Vigente)
        {
            throw new InvalidOperationException("LDO exige um PPA vigente.");
        }

        if (!ppa.CobreExercicio(exercicio))
        {
            throw new InvalidOperationException($"O quadrienio do PPA nao cobre o exercicio {exercicio} da LDO.");
        }

        return new LeiDiretrizes(LdoId.New(), tenantId, exercicio, ppa.Id, numeroLei.Trim(), anoLei);
    }

    /// <summary>
    /// Prioriza uma ação do PPA (CF 165 §2º). A ação DEVE existir e estar vigente no PPA referenciado.
    /// </summary>
    /// <param name="ppa">PPA referenciado (mesmo de <see cref="PpaId"/>).</param>
    /// <param name="acaoPpaId">Ação a priorizar.</param>
    /// <param name="ordem">Ordem de prioridade.</param>
    /// <param name="justificativa">Justificativa.</param>
    /// <exception cref="InvalidOperationException">Se o PPA não bater ou a ação não existir/vigente.</exception>
    public void PriorizarAcao(PlanoPlurianual ppa, AcaoPpaId acaoPpaId, int ordem, string justificativa)
    {
        GarantirEditavel(nameof(PriorizarAcao));
        ArgumentNullException.ThrowIfNull(ppa);
        if (ppa.Id != PpaId)
        {
            throw new InvalidOperationException("PPA informado nao corresponde ao PPA da LDO.");
        }

        if (!ppa.ContemAcaoVigente(acaoPpaId))
        {
            throw new InvalidOperationException("A acao priorizada nao existe ou nao esta vigente no PPA (CF 165 §2º).");
        }

        if (_prioridades.Exists(p => p.AcaoPpaId == acaoPpaId))
        {
            return; // Idempotente: ação já priorizada.
        }

        _prioridades.Add(PrioridadeLdo.Criar(Id, acaoPpaId, ordem, justificativa));
    }

    /// <summary>Define (cria/atualiza) a meta fiscal de um ano do triênio (LRF art. 4º §1º).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Se o ano estiver fora do triênio.</exception>
    public void DefinirMetaFiscal(
        int ano,
        ValorMonetario receitaTotal,
        ValorMonetario despesaTotal,
        decimal resultadoPrimario,
        decimal resultadoNominal,
        ValorMonetario dividaConsolidada)
    {
        GarantirEditavel(nameof(DefinirMetaFiscal));
        if (ano < Exercicio || ano > Exercicio + AnosMetaFiscal - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ano), $"Ano {ano} fora do trienio da LDO [{Exercicio}, {Exercicio + AnosMetaFiscal - 1}].");
        }

        var existente = _metasFiscais.Find(m => m.Ano == ano);
        if (existente is not null)
        {
            existente.Atualizar(receitaTotal, despesaTotal, resultadoPrimario, resultadoNominal, dividaConsolidada);
            return;
        }

        _metasFiscais.Add(MetaFiscal.Criar(Id, ano, receitaTotal, despesaTotal, resultadoPrimario, resultadoNominal, dividaConsolidada));
    }

    /// <summary>Anexa (idempotente por tipo) um anexo da LRF (AMF/ARF).</summary>
    public void Anexar(TipoAnexoLdo tipo, string referenciaDocumento, bool obrigatorio)
    {
        GarantirEditavel(nameof(Anexar));
        if (_anexos.Exists(a => a.Tipo == tipo))
        {
            return;
        }

        _anexos.Add(AnexoLdo.Criar(Id, tipo, referenciaDocumento, obrigatorio));
    }

    /// <summary>Coloca a LDO em tramitação no Legislativo.</summary>
    public void ColocarEmTramitacao()
    {
        if (Situacao != SituacaoLdo.Elaboracao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiDiretrizes), Situacao.ToString(), nameof(ColocarEmTramitacao));
        }

        if (_metasFiscais.Find(m => m.Ano == Exercicio) is null)
        {
            throw new InvalidOperationException("LDO exige a meta fiscal do exercicio para tramitar (LRF art. 4º).");
        }

        Situacao = SituacaoLdo.EmTramitacao;
    }

    /// <summary>
    /// Coloca a LDO em vigor. Valida a obrigatoriedade dos anexos AMF/ARF (parametrizável por tenant):
    /// quando exigidos, devem estar anexados.
    /// </summary>
    /// <param name="numeroLei">Número da lei sancionada.</param>
    /// <param name="anoLei">Ano da lei.</param>
    /// <param name="exigirAmf">Se o AMF é obrigatório para o ente.</param>
    /// <param name="exigirArf">Se o ARF é obrigatório para o ente.</param>
    public void Vigorar(string numeroLei, int anoLei, bool exigirAmf, bool exigirArf)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroLei);
        if (Situacao != SituacaoLdo.EmTramitacao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiDiretrizes), Situacao.ToString(), nameof(Vigorar));
        }

        if (exigirAmf && !_anexos.Exists(a => a.Tipo == TipoAnexoLdo.AnexoMetasFiscais))
        {
            throw new InvalidOperationException("LDO exige o Anexo de Metas Fiscais (AMF — LRF art. 4º §1º).");
        }

        if (exigirArf && !_anexos.Exists(a => a.Tipo == TipoAnexoLdo.AnexoRiscosFiscais))
        {
            throw new InvalidOperationException("LDO exige o Anexo de Riscos Fiscais (ARF — LRF art. 4º §3º).");
        }

        NumeroLei = numeroLei.Trim();
        AnoLei = anoLei;
        Situacao = SituacaoLdo.Vigente;
        RaiseDomainEvent(new LdoVigente(Id, Exercicio));
    }

    /// <summary>Encerra a LDO.</summary>
    public void Encerrar()
    {
        if (Situacao != SituacaoLdo.Vigente)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiDiretrizes), Situacao.ToString(), nameof(Encerrar));
        }

        Situacao = SituacaoLdo.Encerrada;
    }

    /// <summary>
    /// Query de domínio: indica se a LDO está vigente e priorizou a ação informada
    /// (base da validação LOA ⊆ LDO).
    /// </summary>
    /// <param name="acaoPpaId">Ação a verificar.</param>
    /// <returns><c>true</c> se vigente e a ação está priorizada.</returns>
    public bool ContemPrioridade(AcaoPpaId acaoPpaId)
        => Situacao == SituacaoLdo.Vigente && _prioridades.Exists(p => p.AcaoPpaId == acaoPpaId);

    /// <summary>Obtém a meta fiscal do exercício de referência, se definida.</summary>
    /// <returns>A meta fiscal do exercício, ou <c>null</c>.</returns>
    public MetaFiscal? MetaFiscalDoExercicio() => _metasFiscais.Find(m => m.Ano == Exercicio);

    private void GarantirEditavel(string operacao)
    {
        if (Situacao != SituacaoLdo.Elaboracao)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(LeiDiretrizes), Situacao.ToString(), operacao);
        }
    }
}
