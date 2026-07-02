using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>
/// Parte do agregado <see cref="Contrato"/> dedicada aos termos aditivos (Lei 14.133/2021, art. 124-136):
/// derivacao do percentual pelo valorDelta (BUG-A1), tetos separados acrescimo/supressao (BUG-A2),
/// rejeicao de supressao que zere/negative (BUG-A5) e validacao do aditivo de prazo (BUG-A6).
/// Extraida para manter o arquivo principal abaixo do limite de manutenibilidade (sem god-files).
/// </summary>
public sealed partial class Contrato
{
    /// <summary>
    /// Percentual quantitativo acumulado (acrescimos + supressoes em modulo) sobre o valor original.
    /// Mantido por compatibilidade de leitura/Integration Event; o teto legal e verificado por
    /// acrescimo e por supressao SEPARADAMENTE (art. 125 §1º; BUG-A2) — ver
    /// <see cref="PercentualAcrescimoAcumulado"/> e <see cref="PercentualSupressaoAcumulado"/>.
    /// </summary>
    public decimal PercentualQuantitativoAcumulado
        => PercentualAcrescimoAcumulado + PercentualSupressaoAcumulado;

    /// <summary>Percentual ACUMULADO de acrescimos sobre o valor original — teto proprio (art. 125; BUG-A2).</summary>
    public decimal PercentualAcrescimoAcumulado
        => _aditivos.Where(a => a.Tipo == TipoAditivo.Acrescimo).Sum(a => a.PercentualSobreValorOriginal);

    /// <summary>Percentual ACUMULADO de supressoes sobre o valor original — teto proprio (art. 125 §1º; BUG-A2).</summary>
    public decimal PercentualSupressaoAcumulado
        => _aditivos.Where(a => a.Tipo == TipoAditivo.Supressao).Sum(a => a.PercentualSobreValorOriginal);

    /// <summary>
    /// Celebra um termo aditivo respeitando o limite legal de alteracao quantitativa (25%, ate 50% em
    /// reforma — art. 125; I-9). Atualiza <see cref="ValorAtual"/> e/ou <see cref="VigenciaFim"/> conforme
    /// o tipo e emite <see cref="AditivoCelebrado"/> (I-10).
    /// </summary>
    /// <remarks>
    /// BUG-A1: o percentual sobre o valor original e DERIVADO de <paramref name="valorDelta"/> /
    /// <see cref="ValorContratado"/> dentro do agregado (fonte unica) — o chamador nao informa percentual
    /// e valor de forma independente, eliminando o sobrepreco mascarado. BUG-A2: acrescimo e supressao tem
    /// tetos SEPARADOS (art. 125 §1º). BUG-A5: supressao que zere/negative o valor e rejeitada.
    /// BUG-A6: aditivo de Prazo exige nova data posterior a vigencia atual.
    /// </remarks>
    /// <param name="tipo">Tipo do aditivo.</param>
    /// <param name="valorDelta">Variacao de valor resultante (base unica do percentual nos quantitativos).</param>
    /// <param name="novaVigenciaFim">Nova data-fim de vigencia (obrigatoria e crescente no aditivo de prazo).</param>
    /// <param name="justificativa">Justificativa do aditivo (obrigatoria).</param>
    /// <param name="dataCelebracao">Data de celebracao.</param>
    /// <param name="ehReforma">Indica reforma de edificio/equipamento (limite ampliado a 50%) — I-9.</param>
    /// <returns>O <see cref="Aditivo"/> registrado.</returns>
    /// <exception cref="ArgumentException">Se a justificativa for vazia.</exception>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14), o limite for excedido (I-9), a supressao exceder o valor vigente (BUG-A5) ou a nova vigencia for invalida (BUG-A6).</exception>
    public Aditivo CelebrarAditivo(
        TipoAditivo tipo,
        ValorMonetario valorDelta,
        DateOnly? novaVigenciaFim,
        string justificativa,
        DateOnly dataCelebracao,
        bool ehReforma = false)
    {
        // I-10/I-14: apto a aditivo (nao encerrado).
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(justificativa);
        ArgumentNullException.ThrowIfNull(valorDelta);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), "Tipo de aditivo invalido.");
        }

        var ehQuantitativo = tipo is TipoAditivo.Acrescimo or TipoAditivo.Supressao;

        // BUG-A1: o percentual e DERIVADO do valorDelta real sobre o valor original (fonte unica).
        // BUG-A2: o teto e verificado por acrescimo e por supressao SEPARADAMENTE (art. 125 §1º).
        var percentual = ehQuantitativo ? PercentualDe(valorDelta) : 0m;

        if (ehQuantitativo)
        {
            // BUG-A2: o teto ampliado de reforma (50%) e EXCLUSIVO dos ACRESCIMOS (art. 125 §1º — "exclusivamente
            // para os seus acrescimos"). A supressao unilateral permanece em 25% (art. 125 caput) ainda que o
            // contrato seja reforma; supressao acima de 25% so por acordo entre as partes (§2º, caminho distinto
            // nao coberto por este aditivo unilateral). Aplicar o limite ampliado por TIPO, nunca por contrato.
            var limite = (tipo == TipoAditivo.Acrescimo && ehReforma)
                ? LimiteAditivoReforma
                : LimiteAditivoQuantitativo;
            var acumuladoDoTipo = tipo == TipoAditivo.Acrescimo
                ? PercentualAcrescimoAcumulado + percentual
                : PercentualSupressaoAcumulado + percentual;
            if (acumuladoDoTipo > limite)
            {
                throw new InvalidOperationException(
                    $"Aditivo de {tipo} excede o limite legal de alteracao quantitativa ({limite}%). Acumulado: {acumuladoDoTipo}%.");
            }
        }

        // BUG-A5 / #16: supressao OU reequilibrio-para-menor que zere/negative o valor vigente e estado
        // impossivel — rejeitar (nao clampar).
        if ((tipo is TipoAditivo.Supressao or TipoAditivo.ReequilibrioReducao) && !valorDelta.MenorQue(ValorAtual))
        {
            throw new InvalidOperationException(
                $"Reducao de {valorDelta} excede ou anula o valor vigente do contrato ({ValorAtual}); operacao rejeitada.");
        }

        // BUG-A6: aditivo de Prazo exige nova data informada e estritamente posterior a vigencia atual.
        if (tipo == TipoAditivo.Prazo)
        {
            if (novaVigenciaFim is not { } prazoFim)
            {
                throw new InvalidOperationException("Aditivo de prazo exige nova data-fim de vigencia.");
            }

            if (prazoFim <= VigenciaFim)
            {
                throw new InvalidOperationException(
                    $"Aditivo de prazo exige nova vigencia posterior a atual ({VigenciaFim:O}). Informado: {prazoFim:O}.");
            }
        }

        var numero = _aditivos.Count + 1;
        var aditivo = Aditivo.Registrar(numero, tipo, percentual, valorDelta, novaVigenciaFim, justificativa, dataCelebracao);
        _aditivos.Add(aditivo);

        switch (tipo)
        {
            case TipoAditivo.Acrescimo:
            case TipoAditivo.Reequilibrio:
            case TipoAditivo.Qualitativo:
                ValorAtual = ValorAtual.Somar(valorDelta);
                break;
            case TipoAditivo.Supressao:
            case TipoAditivo.ReequilibrioReducao:
                ValorAtual = ValorAtual.Subtrair(valorDelta);
                break;
            case TipoAditivo.Prazo:
                break;
            default:
                break;
        }

        if (novaVigenciaFim is { } novaFim && novaFim > VigenciaFim)
        {
            VigenciaFim = novaFim;
        }

        RaiseDomainEvent(new AditivoCelebrado(Id, aditivo.Id.Value, PercentualQuantitativoAcumulado));
        return aditivo;
    }

    /// <summary>
    /// Marca um aditivo como publicado no PNCP — condicao de sua EFICACIA (Lei 14.133/2021, art. 94, cujo
    /// caput abrange "os contratos e seus aditamentos"; o art. 174 apenas institui o PNCP). Acionado pelo
    /// handler do Outbox apos a transmissao efetiva do termo aditivo pela ACL <c>IPncpGateway</c>.
    /// <para>// TODO(M10): o aditivo deve transmitir com PRAZO PNCP proprio (art. 94 conta da assinatura do
    /// aditamento) pelo cliente HTTP real do PNCP; hoje a transmissao do aditivo segue o // TODO(M10) geral
    /// do gateway, sem prazo persistido proprio.</para>
    /// </summary>
    /// <param name="aditivoId">Identificador do aditivo publicado.</param>
    /// <exception cref="InvalidOperationException">Se o aditivo nao pertencer ao contrato.</exception>
    public void MarcarAditivoPublicado(AditivoId aditivoId)
    {
        var aditivo = _aditivos.SingleOrDefault(a => a.Id == aditivoId)
            ?? throw new InvalidOperationException("Aditivo nao encontrado no contrato.");
        aditivo.MarcarPublicado();
    }

    /// <summary>
    /// Percentual de uma variacao de valor sobre o valor ORIGINAL do contrato (art. 125; BUG-A1).
    /// Fonte unica de verdade do percentual quantitativo — derivado do valorDelta, nunca informado solto.
    /// </summary>
    /// <param name="valorDelta">Variacao de valor.</param>
    /// <returns>Percentual de <paramref name="valorDelta"/> sobre <see cref="ValorContratado"/> (2 casas).</returns>
    private decimal PercentualDe(ValorMonetario valorDelta)
    {
        if (ValorContratado.Valor <= 0m)
        {
            throw new InvalidOperationException("Contrato sem valor original nao admite aditivo quantitativo.");
        }

        return decimal.Round(valorDelta.Valor / ValorContratado.Valor * 100m, 2, MidpointRounding.AwayFromZero);
    }
}
