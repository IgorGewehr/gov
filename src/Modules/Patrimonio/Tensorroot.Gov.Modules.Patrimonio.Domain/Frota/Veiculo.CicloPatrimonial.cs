using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Frota;

/// <summary>
/// Ciclo patrimonial do <see cref="Veiculo"/> (reavaliação/impairment e saída do acervo por
/// baixa/alienação). Extraído como partial para manter o arquivo principal dentro do limite de
/// manutenibilidade (sem god-files) sem dispersar a invariante de ciclo de vida do agregado.
/// </summary>
public sealed partial class Veiculo
{
    /// <summary>
    /// Reavalia o veículo ao valor justo informado (somente ativo no acervo). O novo valor contábil
    /// passa a ser a base prospectiva da depreciação (MCASP / NBC TSP 07): a parcela mensal recalcula
    /// sobre o contábil corrente e a vida útil remanescente, sem retroagir competências já reconhecidas.
    /// </summary>
    /// <param name="novoValorJusto">Novo valor justo (não negativo).</param>
    /// <param name="laudoUri">Referência (URI) do laudo de reavaliação (obrigatório).</param>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o novo valor justo for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o veículo não estiver ativo no acervo.</exception>
    public void Reavaliar(decimal novoValorJusto, string laudoUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        ArgumentOutOfRangeException.ThrowIfNegative(novoValorJusto);
        GarantirAtivoNoAcervo();

        ValorContabil = ValorMonetario.De(novoValorJusto);
        RaiseDomainEvent(new VeiculoReavaliado(Id, ValorContabil.Valor));
    }

    /// <summary>
    /// Reconhece perda por impairment quando o valor recuperável é inferior ao contábil (somente ativo
    /// no acervo). O recuperável vira o novo contábil e baliza prospectivamente a depreciação.
    /// </summary>
    /// <param name="valorRecuperavel">Valor recuperável apurado (não negativo).</param>
    /// <param name="laudoUri">Referência (URI) do laudo/teste de recuperabilidade (obrigatório).</param>
    /// <exception cref="ArgumentException">Se o laudo não for informado.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se o valor recuperável for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o veículo não estiver ativo no acervo ou o recuperável não for inferior ao contábil.</exception>
    public void RegistrarImpairment(decimal valorRecuperavel, string laudoUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        ArgumentOutOfRangeException.ThrowIfNegative(valorRecuperavel);
        GarantirAtivoNoAcervo();

        var recuperavel = ValorMonetario.De(valorRecuperavel);
        // Só reconhece perda se recuperável < contábil (impairment não inflaciona o valor).
        if (!recuperavel.MenorQue(ValorContabil))
        {
            throw new InvalidOperationException("Impairment requer valor recuperável inferior ao valor contábil.");
        }

        ValorContabil = recuperavel;
        RaiseDomainEvent(new VeiculoReavaliado(Id, ValorContabil.Valor));
    }

    /// <summary>
    /// Baixa o veículo do acervo (sinistro, perda total, obsolescência), encerrando-o (terminal). Exige
    /// laudo e autorização. Após a baixa o veículo deixa de depreciar e de aceitar operações de frota.
    /// </summary>
    /// <param name="motivoBaixa">Motivo da baixa.</param>
    /// <param name="laudoUri">Referência (URI) do laudo/parecer (obrigatório).</param>
    /// <param name="autorizacaoId">Identificador da autorização (obrigatório).</param>
    /// <exception cref="ArgumentException">Se motivo ou laudo não forem informados.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a autorização não for informada.</exception>
    /// <exception cref="InvalidOperationException">Se o veículo já estiver encerrado.</exception>
    public void Baixar(string motivoBaixa, string laudoUri, Guid autorizacaoId)
    {
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(motivoBaixa);
        // Sem laudo/autorização a baixa é rejeitada e nenhuma saída contábil é emitida.
        ArgumentException.ThrowIfNullOrWhiteSpace(laudoUri);
        if (autorizacaoId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(autorizacaoId), "Autorização é obrigatória para baixa.");
        }

        Situacao = SituacaoBemPatrimonial.Baixada;
        RaiseDomainEvent(new VeiculoBaixado(Id, motivoBaixa, ValorContabil.Valor));
    }

    /// <summary>
    /// Aliena o veículo (em regra por leilão — Lei 14.133 art. 31/76), encerrando-o (terminal). Exige
    /// avaliação prévia. Após a alienação o veículo deixa de depreciar e de aceitar operações de frota.
    /// </summary>
    /// <param name="avaliacaoPreviaId">Identificador da avaliação prévia (obrigatório).</param>
    /// <param name="porLeilao">Indica se a alienação foi por leilão.</param>
    /// <param name="valorAlienacao">Valor da alienação (não negativo).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a avaliação prévia não for informada ou o valor for negativo.</exception>
    /// <exception cref="InvalidOperationException">Se o veículo já estiver encerrado.</exception>
    public void Alienar(Guid avaliacaoPreviaId, bool porLeilao, decimal valorAlienacao)
    {
        GarantirNaoEncerrado();
        ArgumentOutOfRangeException.ThrowIfNegative(valorAlienacao);
        // Sem avaliação prévia a alienação é rejeitada.
        if (avaliacaoPreviaId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(avaliacaoPreviaId), "Avaliação prévia é obrigatória para alienação.");
        }

        _ = porLeilao;
        Situacao = SituacaoBemPatrimonial.Alienada;
        RaiseDomainEvent(new VeiculoBaixado(Id, "Alienacao", ValorContabil.Valor));
    }

    private void GarantirNaoEncerrado()
    {
        if (Situacao is SituacaoBemPatrimonial.Baixada or SituacaoBemPatrimonial.Alienada)
        {
            throw new InvalidOperationException(
                $"Veículo encerrado não admite novas transições. Situação atual: {Situacao}.");
        }
    }
}
