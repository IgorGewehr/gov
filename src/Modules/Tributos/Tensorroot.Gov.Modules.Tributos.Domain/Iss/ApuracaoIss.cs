using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Iss;

/// <summary>Identificador forte do agregado <see cref="ApuracaoIss"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ApuracaoIssId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ApuracaoIssId"/>.</returns>
    public static ApuracaoIssId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Apuração mensal do ISS de um contribuinte (livro/escrituração eletrônica do ISS): consolida, por
/// competência, o ISS apurado das NFS-e vigentes ingeridas do ADN, separado por modalidade — próprio
/// (a recolher pelo prestador), retido na fonte e substituição tributária. Deriva do acervo do ADN,
/// reduzindo declaração manual (read model gerado da ingestão). É a base do lançamento do ISS próprio.
/// Ver M6-DESIGN §2.2. NÃO emitimos NFS-e (passiva — ADR-0003).
/// </summary>
public sealed class ApuracaoIss : AggregateRoot<ApuracaoIssId>, IMustHaveTenant
{
    private readonly List<ItemApuracaoIss> _itens = [];

    private ApuracaoIss()
    {
    }

    private ApuracaoIss(ApuracaoIssId id, Guid tenantId, ContribuinteId contribuinteId, Competencia competencia)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        Competencia = competencia;
        IssProprio = ValorMonetario.Zero;
        IssRetido = ValorMonetario.Zero;
        IssSubstituicao = ValorMonetario.Zero;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte apurado (prestador).</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Competência (mês/ano) da apuração.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>ISS próprio total a recolher pelo prestador no período (R$).</summary>
    public ValorMonetario IssProprio { get; private set; } = default!;

    /// <summary>ISS retido na fonte total no período (R$).</summary>
    public ValorMonetario IssRetido { get; private set; } = default!;

    /// <summary>ISS por substituição tributária total no período (R$).</summary>
    public ValorMonetario IssSubstituicao { get; private set; } = default!;

    /// <summary>Linhas do livro: uma por NFS-e apurada (escrituração detalhada e auditável).</summary>
    public IReadOnlyCollection<ItemApuracaoIss> Itens => _itens;

    /// <summary>Quantidade de NFS-e escrituradas na apuração.</summary>
    public int QuantidadeNotas => _itens.Count;

    /// <summary>Abre uma apuração mensal vazia para um contribuinte numa competência.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte (prestador) apurado.</param>
    /// <param name="competencia">Competência mês/ano.</param>
    /// <returns>Nova <see cref="ApuracaoIss"/>.</returns>
    public static ApuracaoIss Abrir(Guid tenantId, ContribuinteId contribuinteId, Competencia competencia)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        return new ApuracaoIss(ApuracaoIssId.New(), tenantId, contribuinteId, competencia);
    }

    /// <summary>
    /// Escritura no livro a memória de ISS de uma NFS-e e acumula o total por modalidade. A linha é
    /// imutável após registrada (auditoria).
    /// </summary>
    /// <param name="memoria">Memória de cálculo do ISS da nota (do <see cref="CalculadoraIss"/>).</param>
    public void Escriturar(MemoriaIssNota memoria)
    {
        ArgumentNullException.ThrowIfNull(memoria);

        _itens.Add(ItemApuracaoIss.Criar(
            Id,
            memoria.ChaveAcesso,
            memoria.ItemListaServico,
            memoria.BaseCalculo,
            memoria.AliquotaPercentual,
            memoria.Modalidade,
            memoria.IssApurado));

        switch (memoria.Modalidade)
        {
            case ModalidadeIss.Proprio:
                IssProprio = IssProprio.Somar(memoria.IssApurado);
                break;
            case ModalidadeIss.RetidoNaFonte:
                IssRetido = IssRetido.Somar(memoria.IssApurado);
                break;
            case ModalidadeIss.SubstituicaoTributaria:
                IssSubstituicao = IssSubstituicao.Somar(memoria.IssApurado);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(memoria), memoria.Modalidade, "Modalidade de ISS desconhecida.");
        }
    }

    /// <summary>
    /// Encerra a apuração (escrituração concluída): emite o evento de domínio com o ISS próprio a
    /// recolher, que dispara o lançamento do crédito tributário a jusante.
    /// </summary>
    public void Encerrar()
    {
        RaiseDomainEvent(new ApuracaoIssEncerrada(Id, TenantId, ContribuinteId, IssProprio.Valor));
    }
}
