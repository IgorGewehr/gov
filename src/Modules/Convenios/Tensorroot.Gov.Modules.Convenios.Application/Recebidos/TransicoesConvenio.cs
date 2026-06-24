using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Convenios.Application.Abstractions;
using Tensorroot.Gov.Modules.Convenios.Contracts;
using Tensorroot.Gov.Modules.Convenios.Domain.Comum;
using Tensorroot.Gov.Modules.Convenios.Domain.Parametros;
using Tensorroot.Gov.Modules.Convenios.Domain.Recebidos;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Convenios.Application.Recebidos;

/// <summary>Base interna: localiza o convenio pelo id no tenant atual (ou lanca).</summary>
internal static class ConvenioLookup
{
    public static async Task<ConvenioRecebido> ObterOuFalharAsync(
        IConvenioRecebidoRepository convenios, Guid convenioId, CancellationToken cancellationToken)
    {
        var convenio = await convenios.ObterPorIdAsync(new ConvenioRecebidoId(convenioId), cancellationToken).ConfigureAwait(false);
        return convenio ?? throw new InvalidOperationException($"Convenio recebido {convenioId} nao encontrado no ente.");
    }
}

/// <summary>Aprova o plano de trabalho de um convenio recebido (concedente) — A-INV-1.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
public sealed record AprovarPlanoTrabalhoConvenioCommand(Guid ConvenioId) : ICommand;

/// <summary>Handler da aprovacao do plano de trabalho.</summary>
public sealed class AprovarPlanoTrabalhoConvenioHandler(IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork)
    : ICommandHandler<AprovarPlanoTrabalhoConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarPlanoTrabalhoConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        convenio.AprovarPlanoTrabalho();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Registra a liberacao/recebimento de uma parcela (A-INV-4/5/10).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="NumeroOrdem">Numero de ordem da parcela.</param>
public sealed record RegistrarLiberacaoParcelaConvenioCommand(Guid ConvenioId, int NumeroOrdem) : ICommand;

/// <summary>Handler da liberacao de parcela.</summary>
public sealed class RegistrarLiberacaoParcelaConvenioHandler(
    IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork, IDataHojeTenant dataHoje)
    : ICommandHandler<RegistrarLiberacaoParcelaConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarLiberacaoParcelaConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        // Data da liberacao da parcela (data de lancamento) → HOJE no FUSO do tenant (UTC-3).
        var hoje = dataHoje.Hoje();
        convenio.RegistrarLiberacaoParcela(request.NumeroOrdem, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Registra um rendimento de aplicacao financeira da conta vinculada (A-INV-6).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Valor">Valor do rendimento.</param>
/// <param name="Data">Data do rendimento.</param>
public sealed record RegistrarRendimentoConvenioCommand(Guid ConvenioId, decimal Valor, DateOnly Data) : ICommand;

/// <summary>Validador do rendimento.</summary>
public sealed class RegistrarRendimentoConvenioValidator : AbstractValidator<RegistrarRendimentoConvenioCommand>
{
    /// <summary>Regras.</summary>
    public RegistrarRendimentoConvenioValidator() => RuleFor(comando => comando.Valor).GreaterThan(0);
}

/// <summary>Handler do registro de rendimento.</summary>
public sealed class RegistrarRendimentoConvenioHandler(IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarRendimentoConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarRendimentoConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        convenio.RegistrarRendimento(Dinheiro.De(request.Valor), request.Data);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Abre uma PC parcial (continua) vinculada ao numero ESTRUTURADO da etapa/parcela (A-INV-4).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="NumeroEtapa">Numero de ordem da etapa/parcela coberta (correlacao deterministica — A-INV-4).</param>
/// <param name="CompetenciaRef">Competencia/etapa de referencia (texto livre descritivo).</param>
public sealed record AbrirPrestacaoParcialConvenioCommand(Guid ConvenioId, int NumeroEtapa, string CompetenciaRef) : ICommand<Guid>;

/// <summary>Validador da PC parcial.</summary>
public sealed class AbrirPrestacaoParcialConvenioValidator : AbstractValidator<AbrirPrestacaoParcialConvenioCommand>
{
    /// <summary>Regras.</summary>
    public AbrirPrestacaoParcialConvenioValidator()
    {
        RuleFor(comando => comando.NumeroEtapa).GreaterThan(0);
        RuleFor(comando => comando.CompetenciaRef).NotEmpty().MaximumLength(60);
    }
}

/// <summary>Handler da abertura de PC parcial.</summary>
public sealed class AbrirPrestacaoParcialConvenioHandler(IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork)
    : ICommandHandler<AbrirPrestacaoParcialConvenioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirPrestacaoParcialConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        var pc = convenio.AbrirPrestacaoParcial(request.NumeroEtapa, request.CompetenciaRef);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return pc.Id;
    }
}

/// <summary>Encerra a vigencia e abre a PC FINAL (A-INV-7).</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="CompetenciaRef">Referencia da PC final (ex.: "Final").</param>
public sealed record AbrirPrestacaoFinalConvenioCommand(Guid ConvenioId, string CompetenciaRef) : ICommand<Guid>;

/// <summary>Handler da abertura de PC final.</summary>
public sealed class AbrirPrestacaoFinalConvenioHandler(
    IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork, IDataHojeTenant dataHoje)
    : ICommandHandler<AbrirPrestacaoFinalConvenioCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirPrestacaoFinalConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        // Encerramento de vigencia/abertura de PC final: HOJE no FUSO do tenant (UTC-3), nao o UTC cru.
        var hoje = dataHoje.Hoje();
        var pc = convenio.EncerrarVigenciaAbrirPrestacaoFinal(hoje, request.CompetenciaRef);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return pc.Id;
    }
}

/// <summary>Submete uma PC (parcial/final): calcula o prazo de analise (A-INV-8) e publica o evento.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="PrestacaoId">Identificador da PC.</param>
public sealed record SubmeterPrestacaoConvenioCommand(Guid ConvenioId, Guid PrestacaoId) : ICommand;

/// <summary>Handler da submissao de PC.</summary>
public sealed class SubmeterPrestacaoConvenioHandler(
    IConvenioRecebidoRepository convenios,
    IConveniosParametros parametros,
    ICalendarioDiasUteis calendario,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje,
    TimeProvider relogio)
    : ICommandHandler<SubmeterPrestacaoConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(SubmeterPrestacaoConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        // Prazo de analise (A-INV-8) corre por dia civil → HOJE no FUSO do tenant. O timestamp do evento
        // abaixo permanece UTC (instante absoluto da trilha/Outbox).
        var hoje = dataHoje.Hoje();

        var prazoParcial = parametros.PrazoAnalisePcParcial(tenant.TenantId);
        var prazoFinal = parametros.PrazoAnalisePcFinal(tenant.TenantId);
        convenio.SubmeterPrestacao(request.PrestacaoId, hoje, prazoParcial, prazoFinal, calendario);

        var pc = convenio.Prestacoes.First(p => p.Id == request.PrestacaoId);
        integrationEvents.Enfileirar(new PrestacaoContasConvenioSubmetidaIntegrationEvent(
            Guid.NewGuid(),
            relogio.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            convenio.Id.Value,
            pc.Tipo.ToString(),
            hoje,
            pc.PrazoAnalise!.Vencimento));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Inicia a analise de uma PC submetida.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="PrestacaoId">Identificador da PC.</param>
public sealed record IniciarAnaliseConvenioCommand(Guid ConvenioId, Guid PrestacaoId) : ICommand;

/// <summary>Handler do inicio de analise.</summary>
public sealed class IniciarAnaliseConvenioHandler(IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork)
    : ICommandHandler<IniciarAnaliseConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(IniciarAnaliseConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        convenio.IniciarAnalisePrestacao(request.PrestacaoId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Abre o saneamento de uma PC em analise (A-INV-9): concede o prazo do tenant, uma vez.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="PrestacaoId">Identificador da PC.</param>
public sealed record AbrirSaneamentoConvenioCommand(Guid ConvenioId, Guid PrestacaoId) : ICommand;

/// <summary>Handler da abertura de saneamento.</summary>
public sealed class AbrirSaneamentoConvenioHandler(
    IConvenioRecebidoRepository convenios,
    IConveniosParametros parametros,
    ICalendarioDiasUteis calendario,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<AbrirSaneamentoConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(AbrirSaneamentoConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        // Prazo de saneamento (A-INV-9) corre por dia civil → HOJE no FUSO do tenant (UTC-3).
        var hoje = dataHoje.Hoje();
        var prazoSaneamento = parametros.PrazoSaneamento(tenant.TenantId);
        convenio.AbrirSaneamentoPrestacao(request.PrestacaoId, hoje, prazoSaneamento, calendario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Retoma a analise de uma PC apos o saneamento.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="PrestacaoId">Identificador da PC.</param>
public sealed record RetomarAnaliseConvenioCommand(Guid ConvenioId, Guid PrestacaoId) : ICommand;

/// <summary>Handler da retomada de analise.</summary>
public sealed class RetomarAnaliseConvenioHandler(IConvenioRecebidoRepository convenios, IUnitOfWork unitOfWork)
    : ICommandHandler<RetomarAnaliseConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(RetomarAnaliseConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        convenio.RetomarAnalisePrestacao(request.PrestacaoId);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Conclui a analise de uma PC (A-INV-11) e publica o resultado.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="PrestacaoId">Identificador da PC.</param>
/// <param name="Resultado">Resultado (aprovada/ressalva/rejeitada).</param>
public sealed record ConcluirAnaliseConvenioCommand(Guid ConvenioId, Guid PrestacaoId, ResultadoAnalise Resultado) : ICommand;

/// <summary>Validador da conclusao de analise.</summary>
public sealed class ConcluirAnaliseConvenioValidator : AbstractValidator<ConcluirAnaliseConvenioCommand>
{
    /// <summary>Regras.</summary>
    public ConcluirAnaliseConvenioValidator() => RuleFor(comando => comando.Resultado).IsInEnum();
}

/// <summary>Handler da conclusao de analise.</summary>
public sealed class ConcluirAnaliseConvenioHandler(
    IConvenioRecebidoRepository convenios,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider relogio)
    : ICommandHandler<ConcluirAnaliseConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConcluirAnaliseConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        convenio.ConcluirAnalisePrestacao(request.PrestacaoId, request.Resultado);

        var pc = convenio.Prestacoes.First(p => p.Id == request.PrestacaoId);
        integrationEvents.Enfileirar(new PrestacaoContasConvenioAnaliseConcluidaIntegrationEvent(
            Guid.NewGuid(),
            relogio.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            convenio.Id.Value,
            pc.Tipo.ToString(),
            request.Resultado.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Declara a inadimplencia do convenio (A-INV-10 — gatilho LRF) e publica o evento.</summary>
/// <param name="ConvenioId">Identificador do convenio.</param>
/// <param name="Motivo">Motivo da inadimplencia.</param>
public sealed record DeclararInadimplenciaConvenioCommand(Guid ConvenioId, string Motivo) : ICommand;

/// <summary>Validador da inadimplencia.</summary>
public sealed class DeclararInadimplenciaConvenioValidator : AbstractValidator<DeclararInadimplenciaConvenioCommand>
{
    /// <summary>Regras.</summary>
    public DeclararInadimplenciaConvenioValidator() => RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(2000);
}

/// <summary>Handler da declaracao de inadimplencia.</summary>
public sealed class DeclararInadimplenciaConvenioHandler(
    IConvenioRecebidoRepository convenios,
    IIntegrationEventWriter integrationEvents,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider relogio)
    : ICommandHandler<DeclararInadimplenciaConvenioCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeclararInadimplenciaConvenioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var convenio = await ConvenioLookup.ObterOuFalharAsync(convenios, request.ConvenioId, cancellationToken).ConfigureAwait(false);
        convenio.DeclararInadimplencia(request.Motivo);

        integrationEvents.Enfileirar(new ConvenioInadimplenteIntegrationEvent(
            Guid.NewGuid(),
            relogio.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            convenio.Id.Value,
            request.Motivo));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
