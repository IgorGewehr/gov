using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Esic;

/// <summary>
/// Abre um pedido e-SIC (LAI). PUBLICO/anonimo permitido (a LAI veda exigir motivacao — art. 10 §3o). O
/// prazo legal (20 dias uteis) e CALCULADO pelo agregado via <see cref="ICalendarioDiasUteis"/>; o
/// protocolo e gerado pelo sequencial do (tenant, ano). O tenant ja foi fixado pelo endpoint publico
/// (TenantOverride a partir do slug), entao <c>ITenantContext.TenantId</c> resolve o ente correto.
/// </summary>
/// <param name="Nome">Nome do solicitante (obrigatorio).</param>
/// <param name="Documento">Documento do solicitante (opcional; PII).</param>
/// <param name="Contato">Contato para resposta (opcional; PII).</param>
/// <param name="Descricao">Descricao do pedido (obrigatoria).</param>
/// <param name="FormaResposta">Forma de resposta desejada.</param>
public sealed record AbrirPedidoSicCommand(
    string Nome,
    string? Documento,
    string? Contato,
    string Descricao,
    FormaResposta FormaResposta) : ICommand<string>;

/// <summary>Validador do comando de abertura de pedido e-SIC.</summary>
public sealed class AbrirPedidoSicValidator : AbstractValidator<AbrirPedidoSicCommand>
{
    /// <summary>Define as regras de validacao.</summary>
    public AbrirPedidoSicValidator()
    {
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Descricao).NotEmpty().MaximumLength(4000);
        RuleFor(comando => comando.Documento).MaximumLength(20);
        RuleFor(comando => comando.Contato).MaximumLength(254);
    }
}

/// <summary>Handler que abre o pedido e retorna o protocolo gerado.</summary>
public sealed class AbrirPedidoSicHandler(
    IPedidoSicRepository pedidos,
    ICalendarioDiasUteis calendario,
    ITenantContext tenant,
    TimeProvider relogio,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AbrirPedidoSicCommand, string>
{
    /// <inheritdoc />
    public async Task<string> Handle(AbrirPedidoSicCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // TODO(fuso): trocar por IDataHojeTenant.Hoje() (prazo/data de dominio no fuso do tenant; ver W9 fix de fuso).
        var hoje = DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime);
        var sequencial = await pedidos.ProximoSequencialAsync(hoje.Year, cancellationToken).ConfigureAwait(false);
        var protocolo = ProtocoloSic.Gerar(hoje.Year, sequencial);
        var solicitante = Solicitante.Criar(request.Nome, request.Documento, request.Contato);

        var pedido = PedidoInformacaoSic.Abrir(
            tenant.TenantId,
            protocolo,
            solicitante,
            request.Descricao,
            request.FormaResposta,
            hoje,
            calendario);

        pedidos.Adicionar(pedido);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return protocolo.Valor;
    }
}
