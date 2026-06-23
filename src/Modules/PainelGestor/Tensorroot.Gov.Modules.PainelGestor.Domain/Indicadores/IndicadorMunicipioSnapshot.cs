using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.PainelGestor.Domain.Indicadores;

/// <summary>
/// Read model materializado do Painel do Gestor (BI): consolida, por <c>(TenantId, Exercicio)</c>, os
/// dados BRUTOS de cada KPI alimentados pelos Integration Events dos módulos-fonte (Finanças, Tributos,
/// RH, Transparencia) — NUNCA lendo o interno de outro módulo. O cálculo dos indicadores derivados
/// (% executado, % da RCL/LRF) é feito no apurador de domínio sobre estes valores; aqui guardamos apenas
/// a matéria-prima reprodutível.
/// <para>
/// <b>Idempotência (I-13):</b> os campos de execução orçamentária e custo de pessoal são ACUMULADORES
/// (somam empenhos/liquidações/pagamentos/folhas, deduplicados por origem na camada de ingestão); a
/// dotação, a RCL, a posição de dívida e os mínimos são SUBSTITUÍVEIS (cada publicação do exercício
/// sobrescreve o valor vigente — reprocessar não soma duas vezes).
/// </para>
/// </summary>
public sealed class IndicadorMunicipioSnapshot : AggregateRoot<IndicadorMunicipioId>, IMustHaveTenant
{
    private readonly List<MinimoSetorialSnapshot> _minimos = [];

    private IndicadorMunicipioSnapshot()
    {
    }

    private IndicadorMunicipioSnapshot(IndicadorMunicipioId id, Guid tenantId, int exercicio)
        : base(id)
    {
        TenantId = tenantId;
        Exercicio = exercicio;
    }

    /// <summary>Tenant (ente público) dono do registro. <see cref="IMustHaveTenant"/>.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Exercício (ano) consolidado.</summary>
    public int Exercicio { get; private set; }

    // --- (a) Execução orçamentária (Finanças) ---

    /// <summary>Dotação atualizada (LOA + créditos) — denominador da execução (substituível).</summary>
    public decimal DotacaoAtualizada { get; private set; }

    /// <summary>Total empenhado acumulado no exercício (acumulador).</summary>
    public decimal Empenhado { get; private set; }

    /// <summary>Total liquidado acumulado no exercício (acumulador).</summary>
    public decimal Liquidado { get; private set; }

    /// <summary>Total pago acumulado no exercício (acumulador).</summary>
    public decimal Pago { get; private set; }

    // --- (c) Arrecadação tributária + dívida ativa (Tributos) ---

    /// <summary>Arrecadação tributária acumulada no exercício (acumulador).</summary>
    public decimal ArrecadacaoTributaria { get; private set; }

    /// <summary>Saldo inscrito em dívida ativa (estoque) — substituível.</summary>
    public decimal DividaAtivaSaldoInscrito { get; private set; }

    /// <summary>Parcela ajuizada da dívida ativa — substituível.</summary>
    public decimal DividaAtivaSaldoAjuizado { get; private set; }

    /// <summary>Dívida ativa recuperada no exercício — substituível.</summary>
    public decimal DividaAtivaRecuperada { get; private set; }

    // --- (d) Custo de pessoal + RCL (RH + Finanças) ---

    /// <summary>Despesa total com pessoal (base LRF) acumulada no exercício (acumulador).</summary>
    public decimal DespesaPessoal { get; private set; }

    /// <summary>Receita Corrente Líquida (12 meses) mais recente do exercício — substituível.</summary>
    public decimal ReceitaCorrenteLiquida { get; private set; }

    /// <summary>Mês de referência da RCL vigente (1-12; 0 se ainda não publicada).</summary>
    public int RclMesReferencia { get; private set; }

    // --- (e) Prestação de contas (Transparencia) ---

    /// <summary>Remessas ao TCE-RS transmitidas no exercício (contador, substituível por período).</summary>
    public int RemessasEnviadas { get; private set; }

    /// <summary>Remessas com prazo vencido sem envio no exercício (contador).</summary>
    public int RemessasComPrazoVencido { get; private set; }

    // --- (b) Mínimos constitucionais (Transparencia) — substituíveis ---

    /// <summary>Mínimos constitucionais setoriais materializados (Saúde/Educação).</summary>
    public IReadOnlyCollection<MinimoSetorialSnapshot> Minimos => _minimos;

    /// <summary>Cria o consolidado de um exercício para um tenant.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="exercicio">Exercício (ano).</param>
    /// <returns>Novo snapshot vazio.</returns>
    public static IndicadorMunicipioSnapshot Criar(Guid tenantId, int exercicio)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId é obrigatório.", nameof(tenantId));
        }

        if (exercicio < 1988)
        {
            throw new ArgumentOutOfRangeException(nameof(exercicio), "Exercício inválido.");
        }

        return new IndicadorMunicipioSnapshot(IndicadorMunicipioId.New(), tenantId, exercicio);
    }

    /// <summary>Substitui a dotação orçamentária autorizada do exercício (idempotente).</summary>
    /// <param name="dotacaoAtualizada">Dotação atualizada (LOA + créditos).</param>
    public void DefinirDotacao(decimal dotacaoAtualizada)
    {
        GarantirNaoNegativo(dotacaoAtualizada);
        DotacaoAtualizada = dotacaoAtualizada;
    }

    /// <summary>Acumula um empenho (despesa empenhada) no exercício.</summary>
    /// <param name="valor">Valor empenhado (&gt;= 0).</param>
    public void AcumularEmpenhado(decimal valor)
    {
        GarantirNaoNegativo(valor);
        Empenhado += valor;
    }

    /// <summary>Acumula uma liquidação (despesa liquidada) no exercício.</summary>
    /// <param name="valor">Valor liquidado (&gt;= 0).</param>
    public void AcumularLiquidado(decimal valor)
    {
        GarantirNaoNegativo(valor);
        Liquidado += valor;
    }

    /// <summary>Acumula um pagamento efetuado no exercício.</summary>
    /// <param name="valor">Valor pago (&gt;= 0).</param>
    public void AcumularPago(decimal valor)
    {
        GarantirNaoNegativo(valor);
        Pago += valor;
    }

    /// <summary>Acumula uma receita tributária arrecadada no exercício.</summary>
    /// <param name="valor">Valor arrecadado (&gt;= 0).</param>
    public void AcumularArrecadacao(decimal valor)
    {
        GarantirNaoNegativo(valor);
        ArrecadacaoTributaria += valor;
    }

    /// <summary>Substitui a posição da dívida ativa do exercício (idempotente).</summary>
    /// <param name="saldoInscrito">Saldo inscrito (estoque).</param>
    /// <param name="saldoAjuizado">Parcela ajuizada.</param>
    /// <param name="recuperado">Recuperado no exercício.</param>
    public void DefinirPosicaoDividaAtiva(decimal saldoInscrito, decimal saldoAjuizado, decimal recuperado)
    {
        GarantirNaoNegativo(saldoInscrito);
        GarantirNaoNegativo(saldoAjuizado);
        GarantirNaoNegativo(recuperado);
        DividaAtivaSaldoInscrito = saldoInscrito;
        DividaAtivaSaldoAjuizado = saldoAjuizado;
        DividaAtivaRecuperada = recuperado;
    }

    /// <summary>Acumula despesa com pessoal (base LRF) no exercício.</summary>
    /// <param name="valor">Despesa de pessoal bruta da competência (&gt;= 0).</param>
    public void AcumularDespesaPessoal(decimal valor)
    {
        GarantirNaoNegativo(valor);
        DespesaPessoal += valor;
    }

    /// <summary>
    /// Substitui a RCL vigente do exercício SE o mês de referência informado for mais recente que o já
    /// registrado (mantém a janela de 12 meses mais atual). Idempotente: reprocessar o mesmo mês não muda.
    /// </summary>
    /// <param name="valorRcl">Valor da RCL (12 meses).</param>
    /// <param name="mesReferencia">Mês de referência (1-12).</param>
    public void DefinirReceitaCorrenteLiquida(decimal valorRcl, int mesReferencia)
    {
        GarantirNaoNegativo(valorRcl);
        if (mesReferencia is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(mesReferencia), "Mês de referência deve estar entre 1 e 12.");
        }

        if (mesReferencia >= RclMesReferencia)
        {
            ReceitaCorrenteLiquida = valorRcl;
            RclMesReferencia = mesReferencia;
        }
    }

    /// <summary>Incrementa o contador de remessas transmitidas ao TCE-RS no exercício.</summary>
    public void RegistrarRemessaEnviada() => RemessasEnviadas++;

    /// <summary>Incrementa o contador de remessas com prazo vencido no exercício.</summary>
    public void RegistrarPrazoRemessaVencido() => RemessasComPrazoVencido++;

    /// <summary>Substitui (idempotente) os mínimos constitucionais setoriais do exercício.</summary>
    /// <param name="minimos">Linhas materializadas (Saúde/Educação).</param>
    public void SubstituirMinimos(IEnumerable<MinimoSetorialSnapshot> minimos)
    {
        ArgumentNullException.ThrowIfNull(minimos);
        _minimos.Clear();
        _minimos.AddRange(minimos);
    }

    private static void GarantirNaoNegativo(decimal valor)
    {
        if (valor < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor não pode ser negativo.");
        }
    }
}
