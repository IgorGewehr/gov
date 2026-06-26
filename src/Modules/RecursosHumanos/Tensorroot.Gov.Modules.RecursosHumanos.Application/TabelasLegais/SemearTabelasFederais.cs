using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;

/// <summary>
/// Semeia, para o tenant atual, as tabelas legais FEDERAIS oficiais (INSS/IRRF) por competencia. Os
/// numeros sao DADO parametrizado (nunca hardcoded no motor; podem ser sobrescritos por competencia)
/// — CLAUDE.md S7/S16. As tabelas RPPS NAO sao semeadas (dependem de lei municipal: fail-closed).
/// Fontes: verificacao-folha-calculo.md (auditoria 2026-06-22).
/// </summary>
public sealed record SemearTabelasFederaisCommand : ICommand;

/// <summary>Handler do seed das tabelas federais (INSS/IRRF).</summary>
public sealed class SemearTabelasFederaisHandler(
    ITabelasLegaisRepository tabelas,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SemearTabelasFederaisCommand>
{
    /// <inheritdoc />
    public async Task Handle(SemearTabelasFederaisCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        foreach (var inss in TabelasFederaisSeed.Inss(tenantId))
        {
            tabelas.Adicionar(inss);
        }

        foreach (var irrf in TabelasFederaisSeed.Irrf(tenantId))
        {
            tabelas.Adicionar(irrf);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Valores OFICIAIS das tabelas legais federais (INSS/IRRF) por competencia. Centraliza o seed para
/// reuso por handler e testes. Os numeros aqui sao seed de DADO parametrizado — o motor jamais os
/// embute em logica (CLAUDE.md S16). RPPS nao entra: e municipal (fail-closed).
/// </summary>
public static class TabelasFederaisSeed
{
    /// <summary>Tabelas INSS oficiais (2025 e 2026).</summary>
    /// <param name="tenantId">Tenant destino.</param>
    /// <returns>Tabelas INSS para persistir.</returns>
    public static IReadOnlyList<TabelaInss> Inss(Guid tenantId)
    {
        // INSS 2025 — Portaria Interministerial MPS/MF nº 6/2025 (DOU 13/01/2025). Teto R$ 8.157,41.
        // FONTE: gov.br/inss (verificacao-folha-calculo §1 — CONFIRMADO).
        var inss2025 = TabelaInss.Criar(
            tenantId,
            Competencia.De(2025, 1),
            new[]
            {
                FaixaProgressiva.De(0m, 1518.00m, 0.075m),
                FaixaProgressiva.De(1518.00m, 2793.88m, 0.09m),
                FaixaProgressiva.De(2793.88m, 4190.83m, 0.12m),
                FaixaProgressiva.De(4190.83m, 8157.41m, 0.14m),
            },
            teto: 8157.41m,
            baseLegal: "Portaria Interministerial MPS/MF nº 6/2025");

        // INSS 2026 — Portaria Interministerial MPS/MF nº 13 (09/01/2026), efeitos desde 01/01/2026.
        // Teto R$ 8.475,55. FONTE: gov.br/inss (verificacao-folha-calculo §1b — CONFIRMADO).
        var inss2026 = TabelaInss.Criar(
            tenantId,
            Competencia.De(2026, 1),
            new[]
            {
                FaixaProgressiva.De(0m, 1621.00m, 0.075m),
                FaixaProgressiva.De(1621.00m, 2902.84m, 0.09m),
                FaixaProgressiva.De(2902.84m, 4354.27m, 0.12m),
                FaixaProgressiva.De(4354.27m, 8475.55m, 0.14m),
            },
            teto: 8475.55m,
            baseLegal: "Portaria Interministerial MPS/MF nº 13/2026");

        return new[] { inss2025, inss2026 };
    }

    /// <summary>Tabelas IRRF oficiais (jan-abr/2025, mai-dez/2025 e 2026 com redutor da Lei 15.270/2025).</summary>
    /// <param name="tenantId">Tenant destino.</param>
    /// <returns>Tabelas IRRF para persistir.</returns>
    public static IReadOnlyList<TabelaIrrf> Irrf(Guid tenantId)
    {
        // IRRF jan-abr/2025 — tabela progressiva mensal. FONTE: Receita Federal (verificacao §3 — CONFIRMADO).
        var irrfJan2025 = TabelaIrrf.Criar(
            tenantId,
            Competencia.De(2025, 1),
            new[]
            {
                FaixaIrrf.De(2259.20m, 0.0m, 0.0m),
                FaixaIrrf.De(2826.65m, 0.075m, 169.44m),
                FaixaIrrf.De(3751.05m, 0.15m, 381.44m),
                FaixaIrrf.De(4664.68m, 0.225m, 662.77m),
                FaixaIrrf.De(decimal.MaxValue, 0.275m, 896.00m),
            },
            deducaoPorDependente: 189.59m,
            descontoSimplificado: 564.80m,
            baseLegal: "Tabela progressiva mensal IRRF jan-abr/2025 (Receita Federal)");

        // IRRF mai-dez/2025 — isencao R$ 2.428,80 (MP 1.294/2025 -> Lei 15.191/2025).
        // FONTE: Receita Federal / Câmara (verificacao §3 — tabela CONFIRMADA; instrumento corrigido).
        var irrfMai2025 = TabelaIrrf.Criar(
            tenantId,
            Competencia.De(2025, 5),
            new[]
            {
                FaixaIrrf.De(2428.80m, 0.0m, 0.0m),
                FaixaIrrf.De(2826.65m, 0.075m, 182.16m),
                FaixaIrrf.De(3751.05m, 0.15m, 394.16m),
                FaixaIrrf.De(4664.68m, 0.225m, 675.49m),
                FaixaIrrf.De(decimal.MaxValue, 0.275m, 908.73m),
            },
            deducaoPorDependente: 189.59m,
            descontoSimplificado: 607.20m,
            baseLegal: "Tabela progressiva mensal IRRF mai-dez/2025 (Lei 15.191/2025, ex-MP 1.294/2025)");

        // IRRF 2026 — Lei 15.270/2025 (vigencia 01/01/2026). A tabela progressiva mensal mantem os mesmos
        // limites/parcelas de mai-dez/2025 (Lei 15.191/2025); a novidade e o REDUTOR MENSAL do art. 3o-A da
        // Lei 9.250/1995 (acrescido pela Lei 15.270/2025): isencao efetiva ate R$ 5.000,00 e reducao
        // decrescente ate R$ 7.350,00. FONTE: RFB (orientacao 12/2025) — formula e teto CONFIRMADOS ao vivo;
        // os limites/parcelas exatos da tabela 2026 ainda dependem de ato infralegal anual da Receita.
        // // TODO(M10-validate): confirmar limites/parcelas 2026 e coeficientes do redutor no ato RFB do exercicio.
        var irrf2026 = TabelaIrrf.Criar(
            tenantId,
            Competencia.De(2026, 1),
            new[]
            {
                FaixaIrrf.De(2428.80m, 0.0m, 0.0m),
                FaixaIrrf.De(2826.65m, 0.075m, 182.16m),
                FaixaIrrf.De(3751.05m, 0.15m, 394.16m),
                FaixaIrrf.De(4664.68m, 0.225m, 675.49m),
                FaixaIrrf.De(decimal.MaxValue, 0.275m, 908.73m),
            },
            deducaoPorDependente: 189.59m,
            descontoSimplificado: 607.20m,
            baseLegal: "Tabela IRRF 2026 + redutor mensal (Lei 15.270/2025, art. 3o-A Lei 9.250/1995)",
            // Redutor mensal 2026 (RFB): redutor = min(312,89; max(0; 978,62 - 0,133145 x rendimento)),
            // aplicado ate rendimento bruto de R$ 7.350,00; em R$ 5.000,00 a formula ~ 312,895 (coberto pelo teto).
            redutor: RedutorIrrf.De(
                coeficienteBase: 978.62m,
                coeficienteRendimento: 0.133145m,
                tetoRedutor: 312.89m,
                limiteRendimento: 7350.00m));

        return new[] { irrfJan2025, irrfMai2025, irrf2026 };
    }
}
