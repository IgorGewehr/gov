using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

/// <summary>
/// Item do catalogo de imunobiologicos (vacinas) do PNI: nome, sigla, total de doses do esquema e o
/// intervalo padrao (dias) ate a proxima dose — base do aprazamento. Master data local, raiz de
/// agregado. Pode opcionalmente apontar para um <see cref="MedicamentoId"/> do estoque (a vacina
/// modelada como item de estoque) para que a aplicacao baixe o lote (design 2.2). Parametrizavel: os
/// intervalos do calendario PNI sao configuracao do tenant, nunca hardcoded.
/// </summary>
public sealed class Imunobiologico : AggregateRoot<ImunobiologicoId>, IMustHaveTenant
{
    private Imunobiologico()
    {
    }

    private Imunobiologico(
        ImunobiologicoId id,
        Guid tenantId,
        string nome,
        string sigla,
        int totalDoses,
        int intervaloDiasProximaDose,
        bool doseUnica,
        MedicamentoId? medicamentoEstoqueId)
        : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Sigla = sigla;
        TotalDoses = totalDoses;
        IntervaloDiasProximaDose = intervaloDiasProximaDose;
        DoseUnica = doseUnica;
        MedicamentoEstoqueId = medicamentoEstoqueId;
        Ativo = true;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome do imunobiologico (ex.: "Triplice viral (SCR)").</summary>
    public string Nome { get; private set; } = default!;

    /// <summary>Sigla do imunobiologico (ex.: "SCR", "dTpa", "VIP").</summary>
    public string Sigla { get; private set; } = default!;

    /// <summary>Total de doses previstas no esquema completo (1 para dose unica).</summary>
    public int TotalDoses { get; private set; }

    /// <summary>Intervalo padrao (dias) entre doses, base do aprazamento da proxima.</summary>
    public int IntervaloDiasProximaDose { get; private set; }

    /// <summary>Indica esquema de dose unica (sem proxima dose a aprazar).</summary>
    public bool DoseUnica { get; private set; }

    /// <summary>Medicamento de estoque associado (vacina como item de estoque) — opcional.</summary>
    public MedicamentoId? MedicamentoEstoqueId { get; private set; }

    /// <summary>Indica se o item esta ativo no catalogo.</summary>
    public bool Ativo { get; private set; }

    /// <summary>Cadastra um imunobiologico no catalogo do PNI.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="nome">Nome do imunobiologico.</param>
    /// <param name="sigla">Sigla.</param>
    /// <param name="totalDoses">Total de doses do esquema (>= 1).</param>
    /// <param name="intervaloDiasProximaDose">Intervalo padrao em dias (>= 0; 0 quando dose unica).</param>
    /// <param name="doseUnica">Indica dose unica.</param>
    /// <param name="medicamentoEstoqueId">Item de estoque associado (opcional).</param>
    /// <returns>Novo <see cref="Imunobiologico"/>.</returns>
    /// <exception cref="ArgumentException">Se nome ou sigla forem vazios.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se totalDoses &lt; 1 ou intervalo negativo.</exception>
    public static Imunobiologico Cadastrar(
        Guid tenantId,
        string nome,
        string sigla,
        int totalDoses,
        int intervaloDiasProximaDose,
        bool doseUnica,
        MedicamentoId? medicamentoEstoqueId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(sigla);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalDoses, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(intervaloDiasProximaDose);

        return new Imunobiologico(
            ImunobiologicoId.New(),
            tenantId,
            nome.Trim(),
            sigla.Trim().ToUpperInvariant(),
            doseUnica ? 1 : totalDoses,
            doseUnica ? 0 : intervaloDiasProximaDose,
            doseUnica,
            medicamentoEstoqueId);
    }

    /// <summary>Inativa o imunobiologico no catalogo. Idempotente.</summary>
    public void Inativar() => Ativo = false;
}
