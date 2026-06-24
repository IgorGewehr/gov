using Tensorroot.Gov.Modules.Administracao.Domain.Events;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>
/// Parte do agregado <see cref="Contrato"/> dedicada ao PNCP (Lei 14.133/2021, art. 94 — eficacia pela
/// divulgacao; W9.1): divulgacao + tempestividade, a INVARIANTE DE BLOQUEIO do empenho (sem numero de
/// controle PNCP nao empenha) e a avaliacao do prazo a vencer/vencido para o Portal do Gestor. Extraida
/// para manter o arquivo principal abaixo do limite de manutenibilidade (sem god-files).
/// </summary>
public sealed partial class Contrato
{
    /// <summary>
    /// Confirma a divulgacao do contrato no PNCP, gravando o <b>numero de controle PNCP</b>, marcando
    /// <see cref="PublicadoNoPncp"/> e emitindo <see cref="ContratoPublicadoPncp"/> (condicao de eficacia —
    /// art. 94; o art. 174 apenas institui o PNCP). Se a dotacao ja estiver confirmada, transita para
    /// <see cref="SituacaoContrato.Eficaz"/>. Acionado pelo handler do Outbox APOS a transmissao efetiva
    /// pelo <c>IPncpGateway</c> (idempotente: republicar com o mesmo numero nao altera estado).
    /// </summary>
    /// <param name="numeroControlePncp">Numero de controle atribuido pelo PNCP (obrigatorio).</param>
    /// <param name="dataPublicacao">
    /// Data efetiva da divulgacao (relogio externo via handler). Comparada a <see cref="PrazoPncp.DataLimitePublicacao"/>
    /// para registrar se a publicacao foi TEMPESTIVA (art. 94): publicacao fora do prazo marca
    /// <see cref="PublicacaoPncpVencida"/> (subsidio de auditoria), sem impedir a eficacia.
    /// </param>
    /// <exception cref="ArgumentException">Se o numero de controle PNCP for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se o contrato estiver encerrado (I-14).</exception>
    public void PublicarContratoPncp(string numeroControlePncp, DateOnly dataPublicacao)
    {
        GarantirNaoEncerrado();
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroControlePncp);

        NumeroContratoPncp = numeroControlePncp;
        PublicadoNoPncp = true;

        // Tempestividade da divulgacao (art. 94): registra intempestividade quando publicado apos o
        // vencimento do prazo legal. Nao bloqueia a eficacia (o ato ja foi praticado), mas fica auditavel.
        if (PrazoPublicacaoPncp is { } prazo)
        {
            PublicacaoPncpVencida = prazo.Vencido(dataPublicacao);
        }

        RaiseDomainEvent(new ContratoPublicadoPncp(Id, numeroControlePncp));
        AvaliarEficacia();
    }

    /// <summary>
    /// INVARIANTE DE BLOQUEIO (W9.1): o contrato so pode SUSTENTAR um empenho se ja possui numero de
    /// controle PNCP (divulgado — art. 94). Empenhar despesa de contrato ainda ineficaz (sem divulgacao
    /// no PNCP) e despesa sem cobertura legal de eficacia. Fonte unica de verdade da regra; o modulo
    /// Financas consulta este estado (cross-module via Contracts) ANTES de empenhar — fail-closed.
    /// </summary>
    /// <returns><c>true</c> se ha numero de controle PNCP e o contrato nao foi extinto.</returns>
    public bool PodeEmpenhar()
        => PublicadoNoPncp
           && !string.IsNullOrWhiteSpace(NumeroContratoPncp)
           && Situacao is not (SituacaoContrato.Encerrado or SituacaoContrato.Rescindido);

    /// <summary>
    /// Avalia o prazo de divulgacao no PNCP (art. 94) em <paramref name="hoje"/> quando o contrato AINDA
    /// NAO foi divulgado (W9.1.d): vencido, a vencer (dentro da janela de antecedencia) ou nenhum.
    /// Reproduzivel: o <paramref name="hoje"/> vem do <c>TimeProvider</c> no varredor (Application), nunca
    /// do relogio interno. Consulta PURA (nao altera estado nem emite evento) — quem traduz o veredito em
    /// Integration Event para o Portal do Gestor e o varredor (Application), com idempotencia por EventId.
    /// </summary>
    /// <param name="hoje">Data de referencia (do TimeProvider).</param>
    /// <param name="antecedenciaDiasUteis">Janela de antecedencia do alerta a vencer (parametro do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns>Veredito do alerta de prazo PNCP.</returns>
    public AlertaPrazoPncp AvaliarPrazoPncp(DateOnly hoje, int antecedenciaDiasUteis, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);

        // Ja divulgado, extinto ou sem prazo resolvido (legado): nada a alertar.
        if (PublicadoNoPncp
            || Situacao is SituacaoContrato.Encerrado or SituacaoContrato.Rescindido
            || PrazoPublicacaoPncp is not { } prazo)
        {
            return AlertaPrazoPncp.Nenhum;
        }

        if (prazo.Vencido(hoje))
        {
            return AlertaPrazoPncp.Vencido;
        }

        return prazo.DentroDaJanelaDeAlerta(hoje, antecedenciaDiasUteis, calendario)
            ? AlertaPrazoPncp.AVencer
            : AlertaPrazoPncp.Nenhum;
    }
}
