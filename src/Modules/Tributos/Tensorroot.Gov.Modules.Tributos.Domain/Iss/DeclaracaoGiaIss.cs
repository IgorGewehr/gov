using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Iss;

/// <summary>Identificador forte do agregado <see cref="DeclaracaoGiaIss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DeclaracaoGiaIssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DeclaracaoGiaIssId"/>.</returns>
    public static DeclaracaoGiaIssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situação (estado) da declaração mensal de ISS (GIA).</summary>
public enum SituacaoGiaIss
{
    /// <summary>Em elaboração (serviços sendo escriturados; ainda editável).</summary>
    EmElaboracao = 1,

    /// <summary>Entregue/transmitida (livro fechado; gera o lançamento do ISS próprio).</summary>
    Entregue = 2,

    /// <summary>Substituída por uma declaração retificadora (não exigível).</summary>
    Substituida = 3,
}

/// <summary>
/// Declaração mensal de ISS — GIA do prestador (escrituração declaratória do próprio contribuinte por
/// competência, complementar à apuração derivada das NFS-e do ADN). Consolida os SERVIÇOS PRESTADOS
/// (item da lista LC 116, base e alíquota), apura o ISS PRÓPRIO DEVIDO e, ao ser ENTREGUE, constitui o
/// crédito tributário (CTN art. 150 — lançamento por homologação). Cobre serviços SEM NFS-e nacional,
/// que a apuração derivada do ADN não alcança (PARIDADE-PoC SW-A10). NÃO emitimos NFS-e (ADR-0003) —
/// aqui o contribuinte DECLARA. Domínio rico: a declaração protege seus invariantes (CLAUDE.md §7).
/// </summary>
public sealed class DeclaracaoGiaIss : AggregateRoot<DeclaracaoGiaIssId>, IMustHaveTenant
{
    private readonly List<ItemGiaIss> _itens = [];

    private DeclaracaoGiaIss()
    {
    }

    private DeclaracaoGiaIss(
        DeclaracaoGiaIssId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        Competencia competencia,
        string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        Competencia = competencia;
        FundamentoLegal = fundamentoLegal;
        Situacao = SituacaoGiaIss.EmElaboracao;
        TotalServicos = ValorMonetario.Zero;
        IssDevido = ValorMonetario.Zero;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte declarante (prestador).</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Competência (mês/ano) declarada.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Fundamento legal da obrigação acessória (CTM/decreto de obrigações acessórias).</summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoGiaIss Situacao { get; private set; }

    /// <summary>Total dos serviços prestados declarados na competência (R$).</summary>
    public ValorMonetario TotalServicos { get; private set; } = default!;

    /// <summary>ISS próprio devido apurado da declaração (R$).</summary>
    public ValorMonetario IssDevido { get; private set; } = default!;

    /// <summary>Data de entrega (transmissão) da declaração — data do fato; nula enquanto em elaboração.</summary>
    public DateOnly? DataEntrega { get; private set; }

    /// <summary>Linhas declaradas: um serviço prestado por item (escrituração detalhada e auditável).</summary>
    public IReadOnlyCollection<ItemGiaIss> Itens => _itens;

    /// <summary>Quantidade de serviços declarados.</summary>
    public int QuantidadeServicos => _itens.Count;

    /// <summary>Abre uma declaração mensal de ISS (GIA) em elaboração para um contribuinte/competência.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte declarante (prestador).</param>
    /// <param name="competencia">Competência mês/ano.</param>
    /// <param name="fundamentoLegal">Fundamento legal da obrigação acessória.</param>
    /// <returns>Nova <see cref="DeclaracaoGiaIss"/>.</returns>
    public static DeclaracaoGiaIss Abrir(Guid tenantId, ContribuinteId contribuinteId, Competencia competencia, string fundamentoLegal)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        return new DeclaracaoGiaIss(DeclaracaoGiaIssId.New(), tenantId, contribuinteId, competencia, fundamentoLegal.Trim());
    }

    /// <summary>
    /// Declara um serviço prestado (linha da GIA): item da lista, base e alíquota; o ISS da linha é
    /// calculado pelo agregado e acumulado no total devido. Só é possível enquanto EM ELABORAÇÃO.
    /// </summary>
    /// <param name="itemListaServico">Item da lista de serviços LC 116 (ex.: "7.02").</param>
    /// <param name="descricao">Descrição do serviço prestado.</param>
    /// <param name="baseCalculo">Base de cálculo (valor do serviço, R$).</param>
    /// <param name="aliquotaPercentual">Alíquota aplicável (%), conforme o item (lei municipal).</param>
    /// <param name="retidoNaFonte">Se o ISS desta linha foi retido pelo tomador (não compõe o devido próprio).</param>
    /// <exception cref="InvalidOperationException">Se a declaração não estiver em elaboração.</exception>
    public void DeclararServico(
        string itemListaServico,
        string descricao,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual,
        bool retidoNaFonte)
    {
        ArgumentNullException.ThrowIfNull(baseCalculo);
        if (Situacao != SituacaoGiaIss.EmElaboracao)
        {
            throw new InvalidOperationException($"Só é possível declarar serviços em uma GIA em elaboração. Situação atual: {Situacao}.");
        }

        if (aliquotaPercentual is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquotaPercentual), aliquotaPercentual, "A alíquota do ISS deve estar entre 0 e 100.");
        }

        var issLinha = baseCalculo.AplicarPercentual(aliquotaPercentual);
        var item = ItemGiaIss.Criar(Id, itemListaServico, descricao, baseCalculo, aliquotaPercentual, retidoNaFonte, issLinha);
        _itens.Add(item);

        TotalServicos = TotalServicos.Somar(baseCalculo);

        // O ISS DEVIDO PRÓPRIO exclui o que foi retido na fonte pelo tomador (responsabilidade de
        // terceiro): o prestador não recolhe novamente o que já lhe foi retido (LC 116 art. 6º §2º).
        if (!retidoNaFonte)
        {
            IssDevido = IssDevido.Somar(issLinha);
        }
    }

    /// <summary>
    /// ENTREGA (transmite) a declaração: fecha o livro e constitui o crédito por homologação
    /// (CTN art. 150). Emite o evento com o ISS próprio devido, que dispara o lançamento a jusante.
    /// </summary>
    /// <param name="dataEntrega">Data da entrega (data do fato — "hoje" administrativo).</param>
    /// <exception cref="InvalidOperationException">Se a declaração não estiver em elaboração ou estiver vazia.</exception>
    public void Entregar(DateOnly dataEntrega)
    {
        if (Situacao != SituacaoGiaIss.EmElaboracao)
        {
            throw new InvalidOperationException($"Só é possível entregar uma GIA em elaboração. Situação atual: {Situacao}.");
        }

        if (_itens.Count == 0)
        {
            throw new InvalidOperationException("Não é possível entregar uma GIA sem serviços declarados.");
        }

        Situacao = SituacaoGiaIss.Entregue;
        DataEntrega = dataEntrega;
        RaiseDomainEvent(new DeclaracaoGiaIssEntregue(Id, TenantId, ContribuinteId, IssDevido.Valor));
    }

    /// <summary>
    /// Marca a declaração como SUBSTITUÍDA por uma retificadora (deixa de ser exigível). A nova
    /// declaração retificadora é um agregado distinto da mesma competência.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a declaração não estiver entregue.</exception>
    public void MarcarSubstituida()
    {
        if (Situacao != SituacaoGiaIss.Entregue)
        {
            throw new InvalidOperationException($"Só uma GIA entregue pode ser substituída. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoGiaIss.Substituida;
    }
}
