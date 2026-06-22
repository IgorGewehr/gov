using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;

/// <summary>
/// Linha do balancete (read model / projeção): saldos por conta analítica, exercício e mês.
/// Não é agregado de domínio; é alimentada pelo handler de <c>LancamentoContabilRegistrado</c>.
/// </summary>
public sealed class LinhaBalancete : IMustHaveTenant
{
    /// <summary>Construtor exigido pelo materializador do EF Core.</summary>
    public LinhaBalancete()
    {
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Conta movimentada.</summary>
    public Guid ContaId { get; set; }

    /// <summary>Código contábil (snapshot).</summary>
    public string CodigoConta { get; set; } = default!;

    /// <summary>Título da conta (snapshot).</summary>
    public string Titulo { get; set; } = default!;

    /// <summary>Natureza do saldo da conta.</summary>
    public NaturezaSaldo NaturezaSaldo { get; set; }

    /// <summary>Natureza da informação da conta.</summary>
    public NaturezaInformacao NaturezaInformacao { get; set; }

    /// <summary>Nível hierárquico (para drill-down).</summary>
    public int Nivel { get; set; }

    /// <summary>Exercício.</summary>
    public int Exercicio { get; set; }

    /// <summary>Mês do período (1-12).</summary>
    public int PeriodoMes { get; set; }

    /// <summary>Saldo do início do período.</summary>
    public decimal SaldoAnterior { get; set; }

    /// <summary>Total de débitos do período.</summary>
    public decimal TotalDebitos { get; set; }

    /// <summary>Total de créditos do período.</summary>
    public decimal TotalCreditos { get; set; }

    /// <summary>Saldo ao fim do período (sinal conforme natureza do saldo).</summary>
    public decimal SaldoAtual { get; set; }

    /// <summary>Recalcula o saldo atual a partir do anterior e dos movimentos, conforme a natureza.</summary>
    public void RecalcularSaldo()
        => SaldoAtual = NaturezaSaldo == NaturezaSaldo.Credora
            ? SaldoAnterior + (TotalCreditos - TotalDebitos)
            : SaldoAnterior + (TotalDebitos - TotalCreditos);
}
