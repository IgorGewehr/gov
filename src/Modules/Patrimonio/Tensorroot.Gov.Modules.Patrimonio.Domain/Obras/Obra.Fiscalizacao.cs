using Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Events;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;

/// <summary>
/// Fiscalização, ciclo de vida (paralisação/reinício, conclusão, incorporação, rescisão) e o relógio do
/// art. 94 §3 do agregado <see cref="Obra"/>. Extraído como partial para manter o arquivo principal dentro
/// do limite de manutenibilidade sem dispersar as invariantes (I-10/I-12/I-13/I-14/I-15).
/// </summary>
public sealed partial class Obra
{
    /// <summary>
    /// Designa (ou redesigna) o fiscal/gestor do contrato de obra (Lei 14.133/2021, art. 117), tornando-o
    /// o fiscal vigente — habilita a aprovação de medições (I-10). Não permitido após o encerramento.
    /// </summary>
    /// <param name="fiscalId">Servidor designado (obrigatório).</param>
    /// <param name="desde">Data de início da vigência.</param>
    /// <param name="atoDesignacao">Ato de designação (obrigatório).</param>
    /// <exception cref="InvalidOperationException">Se a obra estiver encerrada.</exception>
    public void DesignarFiscal(Guid fiscalId, DateOnly desde, string atoDesignacao)
    {
        GarantirExecucaoNaoEncerrada();
        var designacao = DesignacaoFiscal.Criar(fiscalId, desde, atoDesignacao);
        _designacoesFiscais.Add(designacao);
        FiscalDesignadoId = designacao.FiscalId;
    }

    /// <summary>Registra uma ocorrência de fiscalização (notificação/advertência/registro técnico — art. 117).</summary>
    /// <param name="data">Data da ocorrência.</param>
    /// <param name="tipo">Tipo da ocorrência.</param>
    /// <param name="descricao">Descrição do fato (obrigatória).</param>
    /// <param name="registradaPorId">Servidor que registra (obrigatório).</param>
    /// <returns>Identificador da ocorrência registrada.</returns>
    /// <exception cref="InvalidOperationException">Se a obra estiver encerrada.</exception>
    public OcorrenciaFiscalizacaoId RegistrarOcorrencia(
        DateOnly data,
        TipoOcorrenciaFiscalizacao tipo,
        string descricao,
        Guid registradaPorId)
    {
        GarantirExecucaoNaoEncerrada();
        var ocorrencia = OcorrenciaFiscalizacao.Criar(data, tipo, descricao, registradaPorId);
        _ocorrencias.Add(ocorrencia);
        return ocorrencia.Id;
    }

    /// <summary>
    /// Paralisa a obra (suspende novas medições/RDOs), registrando o motivo e a data. I-15: paralisar não
    /// apaga medições aprovadas nem reduz o valor medido acumulado. Só a partir de <see cref="SituacaoObra.EmExecucao"/>.
    /// </summary>
    /// <param name="motivo">Motivo da paralisação.</param>
    /// <param name="data">Data da paralisação.</param>
    /// <exception cref="InvalidOperationException">Se a obra não estiver em execução.</exception>
    public void Paralisar(MotivoParalisacao motivo, DateOnly data)
    {
        if (Situacao != SituacaoObra.EmExecucao)
        {
            throw new InvalidOperationException(
                $"Só é possível paralisar obra em execução. Situação atual: {Situacao}.");
        }

        _paralisacoes.Add(EventoParalisacao.Abrir(motivo, data));
        Situacao = SituacaoObra.Paralisada;
    }

    /// <summary>Reinicia a obra paralisada, encerrando o evento de paralisação em aberto.</summary>
    /// <param name="data">Data do reinício (&gt;= data da paralisação).</param>
    /// <exception cref="InvalidOperationException">Se a obra não estiver paralisada.</exception>
    public void Reiniciar(DateOnly data)
    {
        if (Situacao != SituacaoObra.Paralisada)
        {
            throw new InvalidOperationException(
                $"Só é possível reiniciar obra paralisada. Situação atual: {Situacao}.");
        }

        var emAberto = _paralisacoes.LastOrDefault(paralisacao => paralisacao.EmAberto)
            ?? throw new InvalidOperationException("Não há paralisação em aberto para reiniciar.");

        emAberto.Reiniciar(data);
        Situacao = SituacaoObra.EmExecucao;
    }

    /// <summary>
    /// Conclui a obra fisicamente. I-12: exige percentual físico acumulado = 100 (com tolerância) e
    /// valor medido acumulado consistente. Transita para <see cref="SituacaoObra.Concluida"/>, fixa a data
    /// de conclusão (base do relógio art. 94 §3 — 45 d.u.) e emite o Domain Event <see cref="ObraConcluida"/>
    /// (a incorporação patrimonial interna é disparada pelo handler do evento).
    /// </summary>
    /// <param name="dataConclusao">Data de conclusão.</param>
    /// <exception cref="InvalidOperationException">Se a obra não estiver em execução ou não estiver 100% executada (I-12).</exception>
    public void Concluir(DateOnly dataConclusao)
    {
        if (Situacao != SituacaoObra.EmExecucao)
        {
            throw new InvalidOperationException(
                $"A conclusão exige obra em execução. Situação atual: {Situacao}.");
        }

        // I-12: conclusão exige execução física completa (com tolerância de arredondamento).
        if (Math.Abs(PercentualFisicoAcumulado - 100m) > ToleranciaArredondamento)
        {
            throw new InvalidOperationException(
                $"A conclusão exige execução física de 100% (atual: {PercentualFisicoAcumulado}%) — I-12.");
        }

        DataConclusao = dataConclusao;
        Situacao = SituacaoObra.Concluida;
        RaiseDomainEvent(new ObraConcluida(Id, ContratoId, ValorMedidoAcumulado.Valor, dataConclusao));
    }

    /// <summary>
    /// Incorpora a obra concluída ao acervo como bem patrimonial (imobilizado), atribuindo o
    /// <see cref="BemPatrimonialId"/> UMA só vez (I-13 — idempotente: reincorporar é no-op). Transita
    /// <see cref="SituacaoObra.Concluida"/> → <see cref="SituacaoObra.Incorporada"/> e emite
    /// <see cref="ObraIncorporada"/>.
    /// </summary>
    /// <param name="bemPatrimonialId">Bem patrimonial criado pela incorporação.</param>
    /// <exception cref="InvalidOperationException">Se a obra não estiver concluída.</exception>
    public void Incorporar(BemPatrimonialId bemPatrimonialId)
    {
        // I-13: incorporação única e idempotente.
        if (Situacao == SituacaoObra.Incorporada)
        {
            return;
        }

        if (Situacao != SituacaoObra.Concluida)
        {
            throw new InvalidOperationException(
                $"A incorporação exige obra concluída. Situação atual: {Situacao}.");
        }

        BemPatrimonialId = bemPatrimonialId;
        Situacao = SituacaoObra.Incorporada;
        RaiseDomainEvent(new ObraIncorporada(Id, bemPatrimonialId.Value, ValorMedidoAcumulado.Valor));
    }

    /// <summary>Rescinde a obra antes da conclusão (terminal). Preserva medições aprovadas (I-15).</summary>
    /// <param name="motivo">Motivo da rescisão (obrigatório).</param>
    /// <exception cref="ArgumentException">Se o motivo for vazio.</exception>
    /// <exception cref="InvalidOperationException">Se a obra já estiver concluída/incorporada/rescindida.</exception>
    public void Rescindir(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        GarantirExecucaoNaoEncerrada();
        Situacao = SituacaoObra.Rescindida;
    }

    /// <summary>
    /// Calcula o prazo do art. 94 §3 (Lei 14.133/2021) de PUBLICAÇÃO/REGISTRO vinculado à obra (I-14),
    /// usando o calendário transversal de dias úteis do tenant (W9.1). A base é a assinatura do contrato
    /// (<see cref="TipoPrazoArt94.Assinatura25"/>, padrão 25 d.u.) ou a conclusão da obra
    /// (<see cref="TipoPrazoArt94.Conclusao45"/>, padrão 45 d.u.). Os números (25/45), a unidade e a
    /// norma-fonte vêm do <paramref name="parametro"/> (parâmetro por tenant — sem número mágico, §16);
    /// os feriados, do <paramref name="calendario"/>. O resultado é um <see cref="PrazoLegal"/> imutável.
    /// </summary>
    /// <param name="tipo">Tipo do prazo (assinatura/conclusão).</param>
    /// <param name="parametro">Parâmetro de prazo do tenant (quantidade/unidade/norma-fonte).</param>
    /// <param name="calendario">Calendário de dias úteis do tenant (W9.1).</param>
    /// <returns>O prazo legal resolvido (com vencimento calculado).</returns>
    /// <exception cref="ArgumentNullException">Se parâmetro/calendário forem nulos.</exception>
    /// <exception cref="InvalidOperationException">Se o prazo de conclusão for pedido sem data de conclusão definida.</exception>
    public PrazoLegal CalcularPrazoArt94(TipoPrazoArt94 tipo, PrazoArt94Parametro parametro, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(parametro);
        ArgumentNullException.ThrowIfNull(calendario);

        var inicio = tipo switch
        {
            TipoPrazoArt94.Assinatura25 => DataAssinaturaContrato,
            TipoPrazoArt94.Conclusao45 => DataConclusao
                ?? throw new InvalidOperationException("O prazo de conclusão (art. 94 §3) exige obra concluída."),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de prazo do art. 94 §3 inválido."),
        };

        return PrazoLegal.Criar(inicio, parametro.Quantidade, parametro.Unidade, parametro.NormaFonte, calendario);
    }
}
