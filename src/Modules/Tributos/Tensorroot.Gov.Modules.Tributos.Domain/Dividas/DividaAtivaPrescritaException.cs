namespace Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

/// <summary>
/// Erro de domínio: tentativa de COBRAR (emitir CDA, protestar, ajuizar execução fiscal ou parcelar)
/// crédito inscrito em Dívida Ativa já alcançado pela PRESCRIÇÃO (CTN art. 174). A prescrição extingue
/// o próprio crédito tributário (CTN art. 156, V); o protesto/execução de dívida prescrita gera
/// indenização ao cidadão (Lei 9.492/97 + jurisprudência) e extinção da execução com ônus ao município.
/// Por isso a cobrança é barrada (fail-closed).
/// </summary>
public sealed class DividaAtivaPrescritaException : InvalidOperationException
{
    /// <summary>Cria a exceção com a mensagem padronizada de prescrição.</summary>
    /// <param name="dividaAtivaId">Dívida ativa cuja cobrança foi recusada.</param>
    /// <param name="dataPrescricao">Data em que a dívida prescreveu (CTN art. 174).</param>
    /// <param name="dataReferencia">Data de referência do ato de cobrança.</param>
    public DividaAtivaPrescritaException(DividaAtivaId dividaAtivaId, DateOnly dataPrescricao, DateOnly dataReferencia)
        : base($"Cobrança recusada por prescrição (CTN art. 174): a dívida {dividaAtivaId} prescreveu em {dataPrescricao:dd/MM/yyyy}; cobrança tentada em {dataReferencia:dd/MM/yyyy}.")
    {
        DividaAtivaId = dividaAtivaId;
        DataPrescricao = dataPrescricao;
        DataReferencia = dataReferencia;
    }

    /// <summary>Dívida ativa cuja cobrança foi recusada.</summary>
    public DividaAtivaId DividaAtivaId { get; }

    /// <summary>Data em que a dívida prescreveu (CTN art. 174).</summary>
    public DateOnly DataPrescricao { get; }

    /// <summary>Data de referência do ato de cobrança.</summary>
    public DateOnly DataReferencia { get; }
}
