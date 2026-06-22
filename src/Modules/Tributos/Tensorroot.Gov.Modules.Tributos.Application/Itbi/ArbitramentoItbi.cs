using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Itbi;

/// <summary>
/// Instaura um processo de arbitramento da base de cálculo do ITBI (CTN art. 148) sobre uma transmissão.
/// NÃO altera o tributo: a guia segue pela base declarada até a eventual conclusão do processo. Exige
/// motivo individualizado (a declaração não merece fé) — não basta "menor que a pauta" (Tema 1.113/STJ).
/// </summary>
/// <param name="TransmissaoId">Transmissão imobiliária sob arbitramento.</param>
/// <param name="NumeroProcesso">Número/protocolo do processo administrativo.</param>
/// <param name="MotivoInstauracao">Motivo individualizado da instauração.</param>
/// <param name="ValorPropostoFisco">Valor proposto pelo fisco como base (R$).</param>
/// <param name="FundamentacaoFisco">Fundamentação técnica/legal do fisco (ônus da prova do fisco).</param>
/// <param name="ResponsavelId">Servidor responsável pela instauração.</param>
/// <param name="DataInstauracao">Data de instauração.</param>
public sealed record InstaurarArbitramentoItbiCommand(
    Guid TransmissaoId,
    string NumeroProcesso,
    string MotivoInstauracao,
    decimal ValorPropostoFisco,
    string FundamentacaoFisco,
    Guid ResponsavelId,
    DateOnly DataInstauracao) : ICommand<Guid>;

/// <summary>Regras de validação da instauração do arbitramento.</summary>
public sealed class InstaurarArbitramentoItbiValidator : AbstractValidator<InstaurarArbitramentoItbiCommand>
{
    /// <summary>Define as regras.</summary>
    public InstaurarArbitramentoItbiValidator()
    {
        RuleFor(c => c.TransmissaoId).NotEmpty();
        RuleFor(c => c.NumeroProcesso).NotEmpty().MaximumLength(60);
        RuleFor(c => c.MotivoInstauracao).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.ValorPropostoFisco).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.FundamentacaoFisco).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.ResponsavelId).NotEmpty();
    }
}

/// <summary>Handler da instauração do arbitramento do ITBI.</summary>
public sealed class InstaurarArbitramentoItbiHandler(
    ITransmissaoImobiliariaRepository transmissoes,
    IProcessoArbitramentoItbiRepository processos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<InstaurarArbitramentoItbiCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(InstaurarArbitramentoItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transmissao = await transmissoes.ObterPorIdAsync(new TransmissaoImobiliariaId(request.TransmissaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Transmissão imobiliária não encontrada.");

        var processo = ProcessoArbitramentoItbi.Instaurar(
            tenant.TenantId,
            transmissao.Id,
            request.NumeroProcesso,
            request.MotivoInstauracao,
            ValorMonetario.De(request.ValorPropostoFisco),
            request.FundamentacaoFisco,
            request.ResponsavelId,
            request.DataInstauracao);

        processos.Adicionar(processo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return processo.Id.Value;
    }
}

/// <summary>Notifica o contribuinte e abre o contraditório (passo obrigatório — art. 148, parte final).</summary>
/// <param name="ProcessoId">Processo de arbitramento.</param>
/// <param name="DataNotificacao">Data da notificação do contribuinte.</param>
public sealed record AbrirContraditorioArbitramentoItbiCommand(Guid ProcessoId, DateOnly DataNotificacao) : ICommand;

/// <summary>Handler da abertura do contraditório.</summary>
public sealed class AbrirContraditorioArbitramentoItbiHandler(
    IProcessoArbitramentoItbiRepository processos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AbrirContraditorioArbitramentoItbiCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirContraditorioArbitramentoItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.AbrirContraditorio(request.DataNotificacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<ProcessoArbitramentoItbi> ObterAsync(
        IProcessoArbitramentoItbiRepository processos, Guid id, CancellationToken cancellationToken)
        => await processos.ObterPorIdAsync(new ProcessoArbitramentoItbiId(id), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo de arbitramento não encontrado.");
}

/// <summary>Registra a defesa/avaliação contraditória do contribuinte e encaminha à análise do fisco.</summary>
/// <param name="ProcessoId">Processo de arbitramento.</param>
/// <param name="JustificativaContribuinte">Texto da defesa/avaliação contraditória.</param>
/// <param name="DataApresentacao">Data de apresentação da defesa.</param>
public sealed record RegistrarContraditorioArbitramentoItbiCommand(
    Guid ProcessoId,
    string JustificativaContribuinte,
    DateOnly DataApresentacao) : ICommand;

/// <summary>Regras de validação do registro do contraditório.</summary>
public sealed class RegistrarContraditorioArbitramentoItbiValidator : AbstractValidator<RegistrarContraditorioArbitramentoItbiCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarContraditorioArbitramentoItbiValidator()
    {
        RuleFor(c => c.ProcessoId).NotEmpty();
        RuleFor(c => c.JustificativaContribuinte).NotEmpty().MaximumLength(4000);
    }
}

/// <summary>Handler do registro do contraditório.</summary>
public sealed class RegistrarContraditorioArbitramentoItbiHandler(
    IProcessoArbitramentoItbiRepository processos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarContraditorioArbitramentoItbiCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarContraditorioArbitramentoItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await AbrirContraditorioArbitramentoItbiHandler.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.RegistrarContraditorio(request.JustificativaContribuinte, request.DataApresentacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Cancela o processo de arbitramento — a declaração do contribuinte prevaleceu.</summary>
/// <param name="ProcessoId">Processo de arbitramento.</param>
/// <param name="DataCancelamento">Data do cancelamento.</param>
public sealed record CancelarArbitramentoItbiCommand(Guid ProcessoId, DateOnly DataCancelamento) : ICommand;

/// <summary>Handler do cancelamento do arbitramento.</summary>
public sealed class CancelarArbitramentoItbiHandler(
    IProcessoArbitramentoItbiRepository processos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarArbitramentoItbiCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarArbitramentoItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await AbrirContraditorioArbitramentoItbiHandler.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.Cancelar(request.DataCancelamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Resultado da conclusão do arbitramento (lançamento de ofício complementar).</summary>
/// <param name="ProcessoId">Processo de arbitramento concluído.</param>
/// <param name="TransmissaoId">Transmissão recalculada.</param>
/// <param name="BaseArbitrada">Nova base de cálculo (valor arbitrado), R$.</param>
/// <param name="ImpostoDevidoArbitrado">ITBI devido após o arbitramento (R$).</param>
/// <param name="LancamentoComplementarId">Lançamento de ofício complementar da diferença, se houver; senão nulo.</param>
/// <param name="DiferencaImposto">Diferença de ITBI lançada de ofício (R$).</param>
public sealed record ResultadoConclusaoArbitramentoItbi(
    Guid ProcessoId,
    Guid TransmissaoId,
    decimal BaseArbitrada,
    decimal ImpostoDevidoArbitrado,
    Guid? LancamentoComplementarId,
    decimal DiferencaImposto);

/// <summary>
/// Conclui o arbitramento (CTN art. 148) com a decisão final e o valor arbitrado: recalcula o ITBI pela
/// base ARBITRADA, aplica-a à transmissão (origem ArbitradaArt148) e — havendo diferença a maior —
/// constitui o lançamento de ofício complementar com a guia (DAM). SÓ ocorre após o contraditório
/// (estado EmAnalise). É a única via de elevação da base.
/// </summary>
/// <param name="ProcessoId">Processo de arbitramento em análise.</param>
/// <param name="AdquirenteId">Contribuinte adquirente (sujeito passivo do ITBI — CTN art. 42).</param>
/// <param name="ValorArbitrado">Valor arbitrado pela decisão (R$).</param>
/// <param name="DataDecisao">Data da decisão.</param>
/// <param name="VencimentoComplementar">Vencimento da guia complementar (quando houver diferença).</param>
public sealed record ConcluirArbitramentoItbiCommand(
    Guid ProcessoId,
    Guid AdquirenteId,
    decimal ValorArbitrado,
    DateOnly DataDecisao,
    DateOnly VencimentoComplementar) : ICommand<ResultadoConclusaoArbitramentoItbi>;

/// <summary>Regras de validação da conclusão do arbitramento.</summary>
public sealed class ConcluirArbitramentoItbiValidator : AbstractValidator<ConcluirArbitramentoItbiCommand>
{
    /// <summary>Define as regras.</summary>
    public ConcluirArbitramentoItbiValidator()
    {
        RuleFor(c => c.ProcessoId).NotEmpty();
        RuleFor(c => c.AdquirenteId).NotEmpty();
        RuleFor(c => c.ValorArbitrado).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da conclusão do arbitramento + lançamento de ofício complementar.</summary>
public sealed class ConcluirArbitramentoItbiHandler(
    IProcessoArbitramentoItbiRepository processos,
    ITransmissaoImobiliariaRepository transmissoes,
    IAliquotaItbiRepository aliquotas,
    ILancamentoRepository lancamentos,
    IDamRepository dams,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ConcluirArbitramentoItbiCommand, ResultadoConclusaoArbitramentoItbi>
{
    /// <inheritdoc />
    public async Task<ResultadoConclusaoArbitramentoItbi> Handle(ConcluirArbitramentoItbiCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var processo = await AbrirContraditorioArbitramentoItbiHandler.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);

        var transmissao = await transmissoes.ObterPorIdAsync(processo.TransmissaoImobiliariaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Transmissão imobiliária do processo não encontrada.");

        var aliquota = await aliquotas.ObterVigenteAsync(transmissao.Exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há alíquota de ITBI vigente para o exercício {transmissao.Exercicio}.");

        var impostoAnterior = transmissao.ImpostoDevido;

        // Conclui o processo (gera o resultado) e recalcula o ITBI pela base ARBITRADA — única via de elevação.
        var resultado = processo.Concluir(ValorMonetario.De(request.ValorArbitrado), request.DataDecisao);
        var memoriaArbitrada = CalculadoraItbi.RecalcularComArbitramento(
            transmissao.ValorVenalReferencia,
            transmissao.ValorDeclarado,
            resultado,
            aliquota.AliquotaGeralPercentual,
            aliquota.AliquotaSfhFinanciadaPercentual);

        transmissao.AplicarArbitramento(resultado, memoriaArbitrada);

        // Lançamento de ofício COMPLEMENTAR apenas da diferença a maior (CTN art. 149).
        Guid? lancamentoComplementarId = null;
        var diferenca = memoriaArbitrada.ImpostoDevido.Valor - impostoAnterior.Valor;
        if (diferenca > 0m)
        {
            var adquirenteId = new ContribuinteId(request.AdquirenteId);
            var valorDiferenca = ValorMonetario.De(diferenca);
            var lancamento = Lancamento.Lancar(
                tenant.TenantId,
                adquirenteId,
                TipoTributo.Itbi,
                Competencia.De(transmissao.Exercicio, request.VencimentoComplementar.Month),
                valorDiferenca,
                request.VencimentoComplementar);
            var dam = Dam.Gerar(tenant.TenantId, lancamento.Id, adquirenteId, valorDiferenca, numeroParcelas: 1, request.VencimentoComplementar);

            lancamentos.Adicionar(lancamento);
            dams.Adicionar(dam);
            lancamentoComplementarId = lancamento.Id.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoConclusaoArbitramentoItbi(
            processo.Id.Value,
            transmissao.Id.Value,
            memoriaArbitrada.BaseCalculo.Valor,
            memoriaArbitrada.ImpostoDevido.Valor,
            lancamentoComplementarId,
            diferenca > 0m ? diferenca : 0m);
    }
}
