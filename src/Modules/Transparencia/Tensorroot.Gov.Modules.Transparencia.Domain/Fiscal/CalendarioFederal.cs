using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

/// <summary>Identificador forte de <see cref="CalendarioFederal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CalendarioFederalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CalendarioFederalId"/>.</returns>
    public static CalendarioFederalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// <b>M7.0.1 — CalendarioFederal.</b> Prazo federal/estadual parametrizável por tenant+exercício
/// (ex.: declaração SIOPS/SIOPE bimestral, remessa APS mensal, janela Censo SUAS, prazo AgilizaSUAS).
/// <para>
/// <b>Zero prazo no domínio</b> (M7-DESIGN risco #1: prazos federais envelhecem em meses). Cada prazo é
/// um registro <c>(Chave, Exercicio, [Periodo], DataLimite)</c> — nada hardcoded. A entidade só carrega
/// e compara datas; o conteúdo é dado de configuração do tenant.
/// </para>
/// </summary>
public sealed class CalendarioFederal : AggregateRoot<CalendarioFederalId>, IMustHaveTenant
{
    private CalendarioFederal()
    {
    }

    private CalendarioFederal(
        CalendarioFederalId id,
        Guid tenantId,
        string chave,
        int exercicio,
        int? periodo,
        DateOnly dataLimite,
        string? descricao)
        : base(id)
    {
        TenantId = tenantId;
        Chave = chave;
        Exercicio = exercicio;
        Periodo = periodo;
        DataLimite = dataLimite;
        Descricao = descricao;
    }

    /// <summary>Tenant (município) dono do prazo.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave do prazo (ex.: "SIOPS", "SIOPE", "RMA", "AgilizaSUAS"). Não hardcoded em regra.</summary>
    public string Chave { get; private set; } = default!;

    /// <summary>Exercício de referência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Período dentro do exercício (ex.: bimestre 1..6, mês 1..12); <c>null</c> quando anual.</summary>
    public int? Periodo { get; private set; }

    /// <summary>Data-limite do prazo.</summary>
    public DateOnly DataLimite { get; private set; }

    /// <summary>Descrição livre (base legal/portaria vigente).</summary>
    public string? Descricao { get; private set; }

    /// <summary>Registra um prazo do calendário.</summary>
    /// <param name="tenantId">Tenant dono do prazo.</param>
    /// <param name="chave">Chave do prazo.</param>
    /// <param name="exercicio">Exercício.</param>
    /// <param name="dataLimite">Data-limite.</param>
    /// <param name="periodo">Período (bimestre/mês), opcional.</param>
    /// <param name="descricao">Descrição/base legal, opcional.</param>
    /// <returns>Novo <see cref="CalendarioFederal"/>.</returns>
    public static CalendarioFederal Registrar(
        Guid tenantId,
        string chave,
        int exercicio,
        DateOnly dataLimite,
        int? periodo = null,
        string? descricao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 1900);

        return new CalendarioFederal(
            CalendarioFederalId.New(),
            tenantId,
            chave.Trim(),
            exercicio,
            periodo,
            dataLimite,
            descricao);
    }

    /// <summary>Indica se a data de referência está vencida em relação à data-limite (reprodutível, sem relógio).</summary>
    /// <param name="referencia">Data de referência a comparar.</param>
    /// <returns><c>true</c> se a referência ultrapassou a data-limite.</returns>
    public bool EstaVencido(DateOnly referencia) => referencia > DataLimite;
}
