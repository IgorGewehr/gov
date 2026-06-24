using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Contracts;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Mrosc;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Application.Mrosc;

/// <summary>Base interna: localiza a parceria pelo id no tenant atual (ou lanca).</summary>
internal static class ParceriaLookup
{
    public static async Task<ParceriaOsc> ObterOuFalharAsync(
        IParceriaOscRepository parcerias, Guid parceriaId, CancellationToken cancellationToken)
    {
        var parceria = await parcerias.ObterPorIdAsync(new ParceriaOscId(parceriaId), cancellationToken).ConfigureAwait(false);
        return parceria ?? throw new InvalidOperationException($"Parceria OSC {parceriaId} nao encontrada no ente.");
    }
}

/// <summary>Registra o plano de trabalho da parceria (metas + cronograma) — Acordo de Cooperacao sem repasse (B-INV-2).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="Objeto">Objeto.</param>
/// <param name="ValorGlobal">Valor global (zero no Acordo de Cooperacao).</param>
/// <param name="Metas">Metas com indicadores.</param>
/// <param name="Parcelas">Cronograma de desembolso.</param>
public sealed record RegistrarPlanoTrabalhoParceriaCommand(
    Guid ParceriaId,
    string Objeto,
    decimal ValorGlobal,
    IReadOnlyList<MetaPayload> Metas,
    IReadOnlyList<ParcelaRepassePayload> Parcelas) : ICommand;

/// <summary>Validador do registro de plano.</summary>
public sealed class RegistrarPlanoTrabalhoParceriaValidator : AbstractValidator<RegistrarPlanoTrabalhoParceriaCommand>
{
    /// <summary>Regras.</summary>
    public RegistrarPlanoTrabalhoParceriaValidator()
    {
        RuleFor(comando => comando.Objeto).NotEmpty().MaximumLength(1000);
        RuleFor(comando => comando.ValorGlobal).GreaterThanOrEqualTo(0);
        RuleFor(comando => comando.Metas).NotEmpty();
    }
}

/// <summary>Handler do registro de plano de trabalho.</summary>
public sealed class RegistrarPlanoTrabalhoParceriaHandler(IParceriaOscRepository parcerias, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarPlanoTrabalhoParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarPlanoTrabalhoParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);

        var metas = request.Metas
            .Select(meta => new MetaOsc(meta.Descricao, meta.Indicador, meta.ParametroEsperado))
            .ToList();
        var parcelas = request.Parcelas
            .Select(parcela => new ParcelaRepasse(parcela.NumeroOrdem, Dinheiro.De(parcela.Valor), parcela.DataPrevista, parcela.Condicionantes))
            .ToList();

        parceria.RegistrarPlanoTrabalho(request.Objeto, Dinheiro.De(request.ValorGlobal), metas, parcelas);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Aprova o plano de trabalho da parceria (B-INV-4).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record AprovarPlanoTrabalhoParceriaCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler da aprovacao do plano.</summary>
public sealed class AprovarPlanoTrabalhoParceriaHandler(IParceriaOscRepository parcerias, IUnitOfWork unitOfWork)
    : ICommandHandler<AprovarPlanoTrabalhoParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarPlanoTrabalhoParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.AprovarPlanoTrabalho();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Homologa o edital do chamamento (so quando a selecao e por chamamento — B-INV-1).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record HomologarChamamentoParceriaCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler da homologacao do chamamento.</summary>
public sealed class HomologarChamamentoParceriaHandler(IParceriaOscRepository parcerias, IUnitOfWork unitOfWork)
    : ICommandHandler<HomologarChamamentoParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(HomologarChamamentoParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.HomologarChamamento();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Celebra a parceria (B-INV-1/3/4/10) e publica <see cref="ParceriaOscCelebradaIntegrationEvent"/> e, quando
/// ha repasse, <see cref="RepasseOscAEmpenharIntegrationEvent"/> por parcela (gatilho do empenho em Financas).
/// </summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="GestorParceriaId">Gestor da parceria designado (art. 35 §4).</param>
/// <param name="ComissaoMonitoramentoId">Comissao de monitoramento referenciada (art. 35 §5).</param>
/// <param name="ClassificacaoSugerida">Classificacao orcamentaria sugerida dos repasses (opcional).</param>
public sealed record CelebrarParceriaCommand(
    Guid ParceriaId,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    Guid GestorParceriaId,
    Guid ComissaoMonitoramentoId,
    string? ClassificacaoSugerida) : ICommand;

/// <summary>Validador da celebracao.</summary>
public sealed class CelebrarParceriaValidator : AbstractValidator<CelebrarParceriaCommand>
{
    /// <summary>Regras.</summary>
    public CelebrarParceriaValidator()
    {
        RuleFor(comando => comando.VigenciaFim).GreaterThanOrEqualTo(comando => comando.VigenciaInicio);
        RuleFor(comando => comando.GestorParceriaId).NotEmpty();
        RuleFor(comando => comando.ComissaoMonitoramentoId).NotEmpty();
    }
}

/// <summary>Handler da celebracao da parceria.</summary>
public sealed class CelebrarParceriaHandler(
    IParceriaOscRepository parcerias,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje,
    TimeProvider relogio)
    : ICommandHandler<CelebrarParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(CelebrarParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        // Data de celebracao (data de lancamento) → HOJE no FUSO do tenant. O timestamp dos eventos
        // abaixo permanece UTC (instante absoluto da trilha/Outbox).
        var hoje = dataHoje.Hoje();

        var vigencia = Vigencia.Criar(request.VigenciaInicio, request.VigenciaFim);
        parceria.Celebrar(vigencia, request.GestorParceriaId, request.ComissaoMonitoramentoId, hoje);

        var agora = relogio.GetUtcNow().UtcDateTime;
        integrationEvents.Enfileirar(new ParceriaOscCelebradaIntegrationEvent(
            Guid.NewGuid(),
            agora,
            tenant.TenantId,
            parceria.Id.Value,
            parceria.Osc.Cnpj.Digitos,
            parceria.Osc.RazaoSocial,
            parceria.TipoInstrumento.ToString(),
            parceria.Plano!.ValorGlobal.Valor,
            request.VigenciaInicio,
            request.VigenciaFim));

        // Um pedido de empenho por parcela de repasse (Acordo de Cooperacao nao gera repasses — B-INV-2).
        foreach (var repasse in parceria.Repasses)
        {
            integrationEvents.Enfileirar(new RepasseOscAEmpenharIntegrationEvent(
                Guid.NewGuid(),
                agora,
                tenant.TenantId,
                parceria.Id.Value,
                repasse.NumeroOrdem,
                repasse.Valor.Valor,
                request.ClassificacaoSugerida));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Vincula o espelho de execucao orcamentaria (empenho/liquidacao/pagamento) a uma parcela (B-INV-5).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="NumeroOrdem">Numero de ordem da parcela.</param>
/// <param name="EmpenhoId">Empenho (opcional).</param>
/// <param name="LiquidacaoId">Liquidacao (opcional).</param>
/// <param name="PagamentoId">Pagamento (opcional).</param>
public sealed record VincularExecucaoRepasseCommand(
    Guid ParceriaId, int NumeroOrdem, Guid? EmpenhoId, Guid? LiquidacaoId, Guid? PagamentoId) : ICommand;

/// <summary>Handler do vinculo de execucao orcamentaria.</summary>
public sealed class VincularExecucaoRepasseHandler(IParceriaOscRepository parcerias, IUnitOfWork unitOfWork)
    : ICommandHandler<VincularExecucaoRepasseCommand>
{
    /// <inheritdoc />
    public async Task Handle(VincularExecucaoRepasseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.VincularExecucaoOrcamentaria(request.NumeroOrdem, request.EmpenhoId, request.LiquidacaoId, request.PagamentoId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Libera um repasse a OSC (B-INV-2/4/5/9).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="NumeroOrdem">Numero de ordem da parcela.</param>
public sealed record LiberarRepasseParceriaCommand(Guid ParceriaId, int NumeroOrdem) : ICommand;

/// <summary>Handler da liberacao de repasse.</summary>
public sealed class LiberarRepasseParceriaHandler(
    IParceriaOscRepository parcerias, IUnitOfWork unitOfWork, IDataHojeTenant dataHoje)
    : ICommandHandler<LiberarRepasseParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(LiberarRepasseParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        // Data da liberacao do repasse (data de lancamento) → HOJE no FUSO do tenant (UTC-3).
        var hoje = dataHoje.Hoje();
        parceria.LiberarRepasse(request.NumeroOrdem, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Abre a PC da OSC (B-INV-6): calcula o prazo de entrega (90 d) via parametro do tenant.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record AbrirPrestacaoOscCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler da abertura da PC da OSC.</summary>
public sealed class AbrirPrestacaoOscHandler(
    IParceriaOscRepository parcerias,
    IConveniosParametros parametros,
    ICalendarioDiasUteis calendario,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirPrestacaoOscCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirPrestacaoOscCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        var prazoEntrega = parametros.PrazoEntregaPcOsc(tenant.TenantId);
        parceria.AbrirPrestacaoOsc(prazoEntrega, calendario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Prorroga UMA vez o prazo de entrega da PC da OSC (+30 d — B-INV-6).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record ProrrogarEntregaPrestacaoOscCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler da prorrogacao de entrega.</summary>
public sealed class ProrrogarEntregaPrestacaoOscHandler(
    IParceriaOscRepository parcerias,
    IConveniosParametros parametros,
    ICalendarioDiasUteis calendario,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ProrrogarEntregaPrestacaoOscCommand>
{
    /// <inheritdoc />
    public async Task Handle(ProrrogarEntregaPrestacaoOscCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        var prazoProrrogacao = parametros.PrazoProrrogacaoPcOsc(tenant.TenantId);
        parceria.ProrrogarEntregaPrestacao(prazoProrrogacao, calendario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Recebe a PC da OSC (B-INV-7): calcula o prazo de analise (150 d).</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record ReceberPrestacaoOscCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler do recebimento da PC.</summary>
public sealed class ReceberPrestacaoOscHandler(
    IParceriaOscRepository parcerias,
    IConveniosParametros parametros,
    ICalendarioDiasUteis calendario,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<ReceberPrestacaoOscCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReceberPrestacaoOscCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        // Prazo de analise (B-INV-7) corre por dia civil → HOJE no FUSO do tenant (UTC-3).
        var hoje = dataHoje.Hoje();
        var prazoAnalise = parametros.PrazoAnalisePcOsc(tenant.TenantId);
        parceria.ReceberPrestacaoOsc(hoje, prazoAnalise, calendario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Inicia a analise da PC da OSC.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record IniciarAnaliseParceriaCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler do inicio de analise.</summary>
public sealed class IniciarAnaliseParceriaHandler(IParceriaOscRepository parcerias, IUnitOfWork unitOfWork)
    : ICommandHandler<IniciarAnaliseParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(IniciarAnaliseParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.IniciarAnalisePrestacao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Abre o saneamento da PC da OSC (B-INV-8): concede o prazo (45 d) do tenant, uma vez.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record AbrirSaneamentoParceriaCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler da abertura de saneamento.</summary>
public sealed class AbrirSaneamentoParceriaHandler(
    IParceriaOscRepository parcerias,
    IConveniosParametros parametros,
    ICalendarioDiasUteis calendario,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<AbrirSaneamentoParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirSaneamentoParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        // Prazo de saneamento (B-INV-8) corre por dia civil → HOJE no FUSO do tenant (UTC-3).
        var hoje = dataHoje.Hoje();
        var prazoSaneamento = parametros.PrazoSaneamento(tenant.TenantId);
        parceria.AbrirSaneamentoPrestacao(hoje, prazoSaneamento, calendario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Retoma a analise da PC da OSC apos o saneamento.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
public sealed record RetomarAnaliseParceriaCommand(Guid ParceriaId) : ICommand;

/// <summary>Handler da retomada de analise.</summary>
public sealed class RetomarAnaliseParceriaHandler(IParceriaOscRepository parcerias, IUnitOfWork unitOfWork)
    : ICommandHandler<RetomarAnaliseParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RetomarAnaliseParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.RetomarAnalisePrestacao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Conclui a analise da PC da OSC (B-INV-11) e publica o resultado.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="Resultado">Resultado (aprovada/ressalva/rejeitada).</param>
public sealed record ConcluirAnaliseParceriaCommand(Guid ParceriaId, ResultadoAnalise Resultado) : ICommand;

/// <summary>Validador da conclusao.</summary>
public sealed class ConcluirAnaliseParceriaValidator : AbstractValidator<ConcluirAnaliseParceriaCommand>
{
    /// <summary>Regras.</summary>
    public ConcluirAnaliseParceriaValidator() => RuleFor(comando => comando.Resultado).IsInEnum();
}

/// <summary>Handler da conclusao de analise.</summary>
public sealed class ConcluirAnaliseParceriaHandler(
    IParceriaOscRepository parcerias,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider relogio)
    : ICommandHandler<ConcluirAnaliseParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConcluirAnaliseParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.ConcluirAnalisePrestacao(request.Resultado);

        integrationEvents.Enfileirar(new PrestacaoContasOscAnaliseConcluidaIntegrationEvent(
            Guid.NewGuid(),
            relogio.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            parceria.Id.Value,
            request.Resultado.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Declara a inadimplencia da parceria (B-INV-9 — gatilho LRF, bloqueia repasses) e publica o evento.</summary>
/// <param name="ParceriaId">Identificador da parceria.</param>
/// <param name="Motivo">Motivo da inadimplencia.</param>
public sealed record DeclararInadimplenciaParceriaCommand(Guid ParceriaId, string Motivo) : ICommand;

/// <summary>Validador da inadimplencia.</summary>
public sealed class DeclararInadimplenciaParceriaValidator : AbstractValidator<DeclararInadimplenciaParceriaCommand>
{
    /// <summary>Regras.</summary>
    public DeclararInadimplenciaParceriaValidator() => RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(2000);
}

/// <summary>Handler da declaracao de inadimplencia.</summary>
public sealed class DeclararInadimplenciaParceriaHandler(
    IParceriaOscRepository parcerias,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider relogio)
    : ICommandHandler<DeclararInadimplenciaParceriaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeclararInadimplenciaParceriaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parceria = await ParceriaLookup.ObterOuFalharAsync(parcerias, request.ParceriaId, cancellationToken).ConfigureAwait(false);
        parceria.DeclararInadimplencia(request.Motivo);

        integrationEvents.Enfileirar(new ParceriaOscInadimplenteIntegrationEvent(
            Guid.NewGuid(),
            relogio.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            parceria.Id.Value,
            parceria.Osc.Cnpj.Digitos,
            request.Motivo));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
