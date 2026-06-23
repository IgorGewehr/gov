using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Esic;

/// <summary>Base interna: localiza o pedido pelo id no tenant atual (ou lanca).</summary>
internal static class PedidoSicLookup
{
    public static async Task<PedidoInformacaoSic> ObterOuFalharAsync(
        IPedidoSicRepository pedidos, Guid pedidoId, CancellationToken cancellationToken)
    {
        var pedido = await pedidos.ObterPorIdAsync(new PedidoInformacaoSicId(pedidoId), cancellationToken).ConfigureAwait(false);
        return pedido ?? throw new InvalidOperationException($"Pedido e-SIC {pedidoId} nao encontrado no ente.");
    }
}

/// <summary>INTERNO: inicia o atendimento de um pedido (Aberto -&gt; EmAtendimento).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
public sealed record IniciarAtendimentoSicCommand(Guid PedidoId) : ICommand;

/// <summary>Handler que inicia o atendimento.</summary>
public sealed class IniciarAtendimentoSicHandler(IPedidoSicRepository pedidos, IUnitOfWork unitOfWork)
    : ICommandHandler<IniciarAtendimentoSicCommand>
{
    /// <inheritdoc />
    public async Task Handle(IniciarAtendimentoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await PedidoSicLookup.ObterOuFalharAsync(pedidos, request.PedidoId, cancellationToken).ConfigureAwait(false);
        pedido.IniciarAtendimento();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>INTERNO: responde o pedido (acesso concedido — LAI art. 11).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Texto">Texto da resposta.</param>
/// <param name="ReferenciaAnexo">Referencia opcional a anexo.</param>
public sealed record ResponderPedidoSicCommand(Guid PedidoId, string Texto, string? ReferenciaAnexo) : ICommand;

/// <summary>Validador da resposta.</summary>
public sealed class ResponderPedidoSicValidator : AbstractValidator<ResponderPedidoSicCommand>
{
    /// <summary>Regras.</summary>
    public ResponderPedidoSicValidator() => RuleFor(comando => comando.Texto).NotEmpty().MaximumLength(8000);
}

/// <summary>Handler que responde o pedido.</summary>
public sealed class ResponderPedidoSicHandler(
    IPedidoSicRepository pedidos, TimeProvider relogio, IUnitOfWork unitOfWork)
    : ICommandHandler<ResponderPedidoSicCommand>
{
    /// <inheritdoc />
    public async Task Handle(ResponderPedidoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await PedidoSicLookup.ObterOuFalharAsync(pedidos, request.PedidoId, cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        pedido.Responder(RespostaSic.Criar(request.Texto, hoje, request.ReferenciaAnexo));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>INTERNO: prorroga o prazo (+10 dias uteis, unica, justificada — LAI art. 11 §2o).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Motivo">Justificativa (obrigatoria).</param>
public sealed record ProrrogarPedidoSicCommand(Guid PedidoId, string Motivo) : ICommand;

/// <summary>Validador da prorrogacao.</summary>
public sealed class ProrrogarPedidoSicValidator : AbstractValidator<ProrrogarPedidoSicCommand>
{
    /// <summary>Regras.</summary>
    public ProrrogarPedidoSicValidator() => RuleFor(comando => comando.Motivo).NotEmpty().MaximumLength(2000);
}

/// <summary>Handler que prorroga o prazo.</summary>
public sealed class ProrrogarPedidoSicHandler(
    IPedidoSicRepository pedidos, ICalendarioDiasUteis calendario, TimeProvider relogio, IUnitOfWork unitOfWork)
    : ICommandHandler<ProrrogarPedidoSicCommand>
{
    /// <inheritdoc />
    public async Task Handle(ProrrogarPedidoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await PedidoSicLookup.ObterOuFalharAsync(pedidos, request.PedidoId, cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        pedido.Prorrogar(request.Motivo, hoje, calendario);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>INTERNO: indefere o pedido com fundamento legal (LAI art. 11 §1o).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="FundamentoLegal">Fundamento legal (obrigatorio).</param>
public sealed record IndeferirPedidoSicCommand(Guid PedidoId, string FundamentoLegal) : ICommand;

/// <summary>Validador do indeferimento.</summary>
public sealed class IndeferirPedidoSicValidator : AbstractValidator<IndeferirPedidoSicCommand>
{
    /// <summary>Regras.</summary>
    public IndeferirPedidoSicValidator() => RuleFor(comando => comando.FundamentoLegal).NotEmpty().MaximumLength(4000);
}

/// <summary>Handler que indefere o pedido.</summary>
public sealed class IndeferirPedidoSicHandler(
    IPedidoSicRepository pedidos, TimeProvider relogio, IUnitOfWork unitOfWork)
    : ICommandHandler<IndeferirPedidoSicCommand>
{
    /// <inheritdoc />
    public async Task Handle(IndeferirPedidoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await PedidoSicLookup.ObterOuFalharAsync(pedidos, request.PedidoId, cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        pedido.Indeferir(request.FundamentoLegal, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// PUBLICO: o cidadao interpoe recurso pelo protocolo (LAI art. 15). Resolve o pedido por protocolo no
/// tenant ja fixado pelo endpoint publico.
/// </summary>
/// <param name="Protocolo">Protocolo do pedido.</param>
/// <param name="Instancia">Instancia recorrida.</param>
/// <param name="Fundamento">Fundamento do recurso (obrigatorio).</param>
public sealed record InterporRecursoSicCommand(string Protocolo, InstanciaRecurso Instancia, string Fundamento) : ICommand;

/// <summary>Validador do recurso.</summary>
public sealed class InterporRecursoSicValidator : AbstractValidator<InterporRecursoSicCommand>
{
    /// <summary>Regras.</summary>
    public InterporRecursoSicValidator()
    {
        RuleFor(comando => comando.Protocolo).NotEmpty();
        RuleFor(comando => comando.Fundamento).NotEmpty().MaximumLength(4000);
    }
}

/// <summary>Handler que interpoe recurso (pelo protocolo).</summary>
public sealed class InterporRecursoSicHandler(
    IPedidoSicRepository pedidos, TimeProvider relogio, IUnitOfWork unitOfWork)
    : ICommandHandler<InterporRecursoSicCommand>
{
    /// <inheritdoc />
    public async Task Handle(InterporRecursoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await pedidos.ObterPorProtocoloAsync(request.Protocolo, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pedido e-SIC {request.Protocolo} nao encontrado.");
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        pedido.InterporRecurso(request.Instancia, request.Fundamento, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>INTERNO: decide o recurso (RecursoAberto -&gt; RecursoRespondido).</summary>
/// <param name="PedidoId">Identificador do pedido.</param>
/// <param name="Resultado">Resultado da decisao.</param>
/// <param name="Decisao">Texto da decisao (obrigatorio).</param>
public sealed record DecidirRecursoSicCommand(Guid PedidoId, ResultadoRecurso Resultado, string Decisao) : ICommand;

/// <summary>Validador da decisao de recurso.</summary>
public sealed class DecidirRecursoSicValidator : AbstractValidator<DecidirRecursoSicCommand>
{
    /// <summary>Regras.</summary>
    public DecidirRecursoSicValidator() => RuleFor(comando => comando.Decisao).NotEmpty().MaximumLength(8000);
}

/// <summary>Handler que decide o recurso.</summary>
public sealed class DecidirRecursoSicHandler(
    IPedidoSicRepository pedidos, TimeProvider relogio, IUnitOfWork unitOfWork)
    : ICommandHandler<DecidirRecursoSicCommand>
{
    /// <inheritdoc />
    public async Task Handle(DecidirRecursoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var pedido = await PedidoSicLookup.ObterOuFalharAsync(pedidos, request.PedidoId, cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        pedido.DecidirRecurso(request.Resultado, request.Decisao, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
