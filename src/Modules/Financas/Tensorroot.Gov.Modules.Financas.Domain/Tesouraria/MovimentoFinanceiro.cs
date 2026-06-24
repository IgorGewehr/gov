using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Tesouraria;

/// <summary>
/// Movimento financeiro de uma conta da tesouraria (uma linha do extrato): recebimento,
/// pagamento ou perna de transferência. Imutável após o registro; guarda o saldo APÓS o
/// movimento (snapshot) para o boletim e a conciliação sem recálculo.
/// </summary>
public sealed class MovimentoFinanceiro : Entity<MovimentoFinanceiroId>
{
    private MovimentoFinanceiro()
    {
    }

    private MovimentoFinanceiro(
        MovimentoFinanceiroId id,
        ContaFinanceiraId contaId,
        TipoMovimentoFinanceiro tipo,
        DateOnly data,
        ValorMonetario valor,
        ValorMonetario saldoApos,
        string historico,
        string? documento,
        Guid? origemReferenciaId,
        ContaFinanceiraId? contraparteContaId)
        : base(id)
    {
        ContaId = contaId;
        Tipo = tipo;
        Data = data;
        Valor = valor;
        SaldoApos = saldoApos;
        Historico = historico;
        Documento = documento;
        OrigemReferenciaId = origemReferenciaId;
        ContraparteContaId = contraparteContaId;
        Conciliado = false;
    }

    /// <summary>Conta dona do movimento.</summary>
    public ContaFinanceiraId ContaId { get; private set; }

    /// <summary>Natureza do movimento.</summary>
    public TipoMovimentoFinanceiro Tipo { get; private set; }

    /// <summary>Data do movimento.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Valor (sempre positivo).</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Saldo da conta APÓS este movimento (snapshot).</summary>
    public ValorMonetario SaldoApos { get; private set; } = default!;

    /// <summary>Histórico (narrativa).</summary>
    public string Historico { get; private set; } = default!;

    /// <summary>Documento de referência (nº OP, guia, recibo) — opcional.</summary>
    public string? Documento { get; private set; }

    /// <summary>Id do fato de origem (OP/arrecadação) para idempotência/vínculo — opcional.</summary>
    public Guid? OrigemReferenciaId { get; private set; }

    /// <summary>Conta contraparte numa transferência (origem↔destino) — opcional.</summary>
    public ContaFinanceiraId? ContraparteContaId { get; private set; }

    /// <summary>Indica se já foi conciliado com o extrato bancário.</summary>
    public bool Conciliado { get; private set; }

    /// <summary>Data da conciliação (quando casado com o extrato) — opcional.</summary>
    public DateOnly? DataConciliacao { get; private set; }

    /// <summary>Cria um movimento validado (valor positivo).</summary>
    internal static MovimentoFinanceiro Criar(
        ContaFinanceiraId contaId,
        TipoMovimentoFinanceiro tipo,
        DateOnly data,
        ValorMonetario valor,
        ValorMonetario saldoApos,
        string historico,
        string? documento,
        Guid? origemReferenciaId,
        ContaFinanceiraId? contraparteContaId)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentNullException.ThrowIfNull(saldoApos);
        ArgumentException.ThrowIfNullOrWhiteSpace(historico);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor do movimento deve ser positivo.");
        }

        return new MovimentoFinanceiro(
            MovimentoFinanceiroId.New(),
            contaId,
            tipo,
            data,
            valor,
            saldoApos,
            historico.Trim(),
            string.IsNullOrWhiteSpace(documento) ? null : documento.Trim(),
            origemReferenciaId,
            contraparteContaId);
    }

    /// <summary>
    /// Marca o movimento como conciliado contra o extrato bancário (casamento manual — a
    /// importação de extrato OFX fica // TODO(M10), mas o casamento já opera).
    /// </summary>
    /// <param name="dataConciliacao">Data em que o casamento foi confirmado.</param>
    /// <exception cref="InvalidOperationException">Se já conciliado.</exception>
    public void Conciliar(DateOnly dataConciliacao)
    {
        if (Conciliado)
        {
            throw new InvalidOperationException("Movimento ja conciliado.");
        }

        Conciliado = true;
        DataConciliacao = dataConciliacao;
    }
}
