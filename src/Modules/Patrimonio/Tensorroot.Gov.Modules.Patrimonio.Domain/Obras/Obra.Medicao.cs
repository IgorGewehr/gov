using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>
/// Medição/boletim do agregado <see cref="Obra"/> (registro, aprovação que libera pagamento, rejeição).
/// Extraído como partial para manter o arquivo principal dentro do limite de manutenibilidade sem
/// dispersar as invariantes de medição (I-1, I-5, I-6, I-7, I-8, I-10, I-11).
/// </summary>
public sealed partial class Obra
{
    /// <summary>
    /// Avanço por etapa atribuído a uma medição: qual etapa, quanto de avanço físico e quanto de valor.
    /// A medição reporta o progresso por etapa para manter a coerência físico-financeira (I-5) e derivar
    /// o percentual físico acumulado da obra (I-6).
    /// </summary>
    /// <param name="EtapaId">Etapa medida.</param>
    /// <param name="PercentualFisicoNoPeriodo">Avanço físico da etapa no período (0–100).</param>
    /// <param name="ValorNoPeriodo">Valor medido da etapa no período.</param>
    public readonly record struct AvancoEtapa(EtapaCronogramaId EtapaId, decimal PercentualFisicoNoPeriodo, ValorMonetario ValorNoPeriodo);

    /// <summary>
    /// Registra um boletim de medição em rascunho. Numeração monotônica por obra (I-7) e períodos
    /// disjuntos (I-7b) são garantidos aqui. O valor da medição é a soma dos avanços por etapa; o avanço
    /// físico no período é derivado dos pesos das etapas (I-6). Exige obra em execução (I-11).
    /// </summary>
    /// <param name="competenciaAno">Ano da competência.</param>
    /// <param name="competenciaMes">Mês da competência (1–12).</param>
    /// <param name="periodoInicio">Início do período medido (inclusivo).</param>
    /// <param name="periodoFim">Fim do período medido (&gt;= início).</param>
    /// <param name="avancos">Avanços por etapa (pelo menos um).</param>
    /// <returns>Identificador da medição registrada (em rascunho).</returns>
    /// <exception cref="ArgumentException">Se não houver avanços.</exception>
    /// <exception cref="InvalidOperationException">Se a obra não estiver em execução (I-11), houver sobreposição de período (I-7b) ou etapa inexistente.</exception>
    public MedicaoId RegistrarMedicao(
        int competenciaAno,
        int competenciaMes,
        DateOnly periodoInicio,
        DateOnly periodoFim,
        IReadOnlyCollection<AvancoEtapa> avancos)
    {
        ArgumentNullException.ThrowIfNull(avancos);
        if (avancos.Count == 0)
        {
            throw new ArgumentException("A medição exige ao menos um avanço de etapa.", nameof(avancos));
        }

        GarantirEmExecucao();

        // I-7b: períodos de medições NÃO rejeitadas não se sobrepõem.
        var conflitante = _medicoes.Any(medicao =>
            medicao.Situacao != SituacaoMedicao.Rejeitada
            && medicao.SobrepoePeriodo(periodoInicio, periodoFim));
        if (conflitante)
        {
            throw new InvalidOperationException(
                $"O período {periodoInicio:yyyy-MM-dd}..{periodoFim:yyyy-MM-dd} se sobrepõe a uma medição existente (I-7b).");
        }

        // Valida que todas as etapas referenciadas existem; soma o valor e deriva o avanço físico ponderado (I-6).
        var valorTotal = ValorMonetario.Zero;
        var avancoFisicoPonderado = 0m;
        foreach (var avanco in avancos)
        {
            var etapa = _etapas.FirstOrDefault(item => item.Id == avanco.EtapaId)
                ?? throw new InvalidOperationException($"Etapa {avanco.EtapaId} inexistente no cronograma da obra.");

            ArgumentNullException.ThrowIfNull(avanco.ValorNoPeriodo);
            valorTotal = valorTotal.Somar(avanco.ValorNoPeriodo);
            // I-6: contribuição da etapa ao avanço físico da obra = avanço da etapa × peso da etapa / 100.
            avancoFisicoPonderado += avanco.PercentualFisicoNoPeriodo * etapa.PercentualFisicoPrevisto / 100m;
        }

        // I-7: numeração monotônica sem buracos (próximo = maior número existente + 1).
        var proximoNumero = _medicoes.Count == 0 ? 1 : _medicoes.Max(medicao => medicao.Numero) + 1;

        var medicao = Medicao.Registrar(
            proximoNumero,
            competenciaAno,
            competenciaMes,
            periodoInicio,
            periodoFim,
            valorTotal,
            decimal.Round(avancoFisicoPonderado, 4, MidpointRounding.AwayFromZero));

        // Persiste os avanços por etapa como linhas da medição (aplicados às etapas apenas na aprovação —
        // I-15). Lastro reprodutível: a aprovação lê de medicao.Itens, sobrevive à recarga do agregado.
        foreach (var avanco in avancos)
        {
            medicao.AdicionarItem(ItemMedicao.Criar(avanco.EtapaId, avanco.PercentualFisicoNoPeriodo, avanco.ValorNoPeriodo));
        }

        _medicoes.Add(medicao);
        return medicao.Id;
    }

    /// <summary>
    /// Aprova um boletim de medição (gatilho da liquidação em Finanças — Lei 4.320 art. 63). Invariantes:
    /// só fiscal designado vigente aprova (I-10); a medição acumulada não pode exceder o contratado (I-1);
    /// a coerência físico-financeira por etapa é respeitada (I-5); deve haver RDOs cobrindo o período (I-8);
    /// obra em execução (I-11). Emite o Domain Event <see cref="MedicaoAprovada"/>.
    /// </summary>
    /// <param name="medicaoId">Medição a aprovar (em rascunho).</param>
    /// <param name="fiscalId">
    /// Identidade do aprovador. DEVE ser o subject autenticado (JWT "sub"), nunca um id informado pelo
    /// cliente — a camada de aplicação é responsável por derivá-lo do principal. I-10 confronta este id
    /// contra <see cref="FiscalDesignadoId"/>: só aprova se o usuário autenticado FOR o fiscal designado
    /// vigente (segregação de função — art. 117); a trilha registra exatamente quem aprovou.
    /// </param>
    /// <param name="dataAprovacao">Data da aprovação.</param>
    /// <exception cref="InvalidOperationException">Se qualquer invariante (I-1/I-5/I-8/I-10/I-11) for violada ou a medição não existir/estiver fora de rascunho.</exception>
    public void AprovarMedicao(MedicaoId medicaoId, Guid fiscalId, DateOnly dataAprovacao)
    {
        GarantirEmExecucao();

        // I-10: aprovação só pelo fiscal designado vigente.
        if (FiscalDesignadoId is null)
        {
            throw new InvalidOperationException("Não há fiscal designado para aprovar a medição (art. 117 / I-10).");
        }

        if (FiscalDesignadoId.Value != fiscalId)
        {
            throw new InvalidOperationException("Somente o fiscal designado vigente pode aprovar a medição (I-10).");
        }

        var medicao = _medicoes.FirstOrDefault(item => item.Id == medicaoId)
            ?? throw new InvalidOperationException("Medição não encontrada.");

        // I-1: a medição acumulada (já aprovada + esta) não pode exceder o valor contratado.
        var acumuladoComEsta = ValorMedidoAcumulado.Somar(medicao.ValorMedido);
        if (acumuladoComEsta.MaiorQue(ValorContratado))
        {
            throw new InvalidOperationException(
                $"A medição acumulada ({acumuladoComEsta}) excederia o valor contratado ({ValorContratado}) — exige aditivo (I-1).");
        }

        // I-8: a medição só é aprovada com RDOs cobrindo todo o período medido.
        if (!PeriodoCobertoPorRdo(medicao.PeriodoInicio, medicao.PeriodoFim))
        {
            throw new InvalidOperationException(
                "A medição exige RDOs cobrindo todos os dias úteis do período (lastro de fiscalização — I-8).");
        }

        // I-5: aplica os avanços (linhas da medição) às etapas (cada etapa valida físico ≤ 100 e valor ≤ previsto).
        foreach (var item in medicao.Itens)
        {
            var etapa = _etapas.FirstOrDefault(etapaItem => etapaItem.Id == item.EtapaId)
                ?? throw new InvalidOperationException($"Etapa {item.EtapaId} inexistente no cronograma da obra.");
            etapa.ReconhecerMedicao(item.PercentualFisicoNoPeriodo, item.ValorNoPeriodo);
        }

        medicao.Aprovar(fiscalId, dataAprovacao);
        ValorMedidoAcumulado = acumuladoComEsta;
        // I-6: percentual físico acumulado derivado das etapas (ponderado), nunca digitado.
        RecalcularPercentualFisicoAcumulado();

        RaiseDomainEvent(new MedicaoAprovada(
            Id,
            ContratoId,
            medicao.Id,
            medicao.Numero,
            medicao.ValorMedido.Valor,
            medicao.CompetenciaAno,
            medicao.CompetenciaMes,
            FornecedorId));
    }

    /// <summary>Rejeita um boletim de medição (não compõe o valor medido acumulado). Exige obra em execução.</summary>
    /// <param name="medicaoId">Medição a rejeitar (em rascunho).</param>
    /// <param name="motivo">Motivo da rejeição (obrigatório).</param>
    /// <exception cref="InvalidOperationException">Se a obra não estiver em execução ou a medição não existir/estiver fora de rascunho.</exception>
    public void RejeitarMedicao(MedicaoId medicaoId, string motivo)
    {
        GarantirEmExecucao();
        var medicao = _medicoes.FirstOrDefault(item => item.Id == medicaoId)
            ?? throw new InvalidOperationException("Medição não encontrada.");

        medicao.Rejeitar(motivo);
    }

    // I-6: percentual físico da obra = Σ (percentual executado da etapa × peso da etapa) / 100. Limitado a 100.
    private void RecalcularPercentualFisicoAcumulado()
    {
        var acumulado = _etapas.Sum(etapa => etapa.PercentualFisicoExecutado * etapa.PercentualFisicoPrevisto / 100m);
        PercentualFisicoAcumulado = Math.Min(100m, decimal.Round(acumulado, 4, MidpointRounding.AwayFromZero));
    }

    // I-8: todo dia ÚTIL entre início e fim do período possui RDO. Usa o calendário do tenant para não
    // exigir RDO em fins de semana/feriados (a fiscalização contínua cobre os dias trabalhados).
    private bool PeriodoCobertoPorRdo(DateOnly inicio, DateOnly fim)
    {
        if (_registrosDiarios.Count == 0)
        {
            return false;
        }

        var diasComRdo = _registrosDiarios.Select(rdo => rdo.Data).ToHashSet();
        for (var dia = inicio; dia <= fim; dia = dia.AddDays(1))
        {
            if (!diasComRdo.Contains(dia))
            {
                return false;
            }
        }

        return true;
    }
}
