using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Application.Integracoes;
using Xunit;

namespace Tensorroot.Gov.Modules.Transparencia.Tests;

/// <summary>
/// Cobertura da ponte (ACL de entrada) Financas → Transparencia: o consumo do
/// <see cref="MSCGeradaIntegrationEvent"/> deve DEGRADAR GRACIOSAMENTE quando a MSC nao traz linhas de
/// saldo final (competencia vazia) — sem disparar um comando invalido que ENVENENARIA o Outbox (a
/// validacao <c>Linhas NotEmpty</c> reprovaria e a mensagem entraria em retry infinito).
/// </summary>
public sealed class ReceberMscGeradaTests
{
    private const int TipoValorSaldoInicial = 1;
    private const int TipoValorSaldoFinal = 3;
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static MSCGeradaIntegrationEvent Evento(IReadOnlyList<LinhaMscDto> linhas)
        => new(Guid.NewGuid(), DateTime.UtcNow, TenantA, 2026, 6, TipoMatriz: 1, linhas);

    [Fact] // MSC sem nenhuma linha de saldo final ⇒ nao consolida (nada e enviado ao MediatR).
    public async Task Msc_sem_saldo_final_nao_dispara_consolidacao()
    {
        var sender = new SpiaoSender();
        var handler = new ReceberMSCGeradaHandler(sender, NullLogger<ReceberMSCGeradaHandler>.Instance);

        // So ha movimento/saldo inicial — nenhum SaldoFinal (TipoValor 3).
        await handler.Handle(
            Evento([new LinhaMscDto("111110100", 1, TipoValorSaldoInicial, 1_000m, null, null)]),
            CancellationToken.None);

        sender.Enviados.Should().BeEmpty("MSC sem saldo de fechamento nao produz declaracao (sem poison no Outbox)");
    }

    [Fact] // MSC totalmente vazia ⇒ idem: ack idempotente sem consolidar.
    public async Task Msc_vazia_nao_dispara_consolidacao()
    {
        var sender = new SpiaoSender();
        var handler = new ReceberMSCGeradaHandler(sender, NullLogger<ReceberMSCGeradaHandler>.Instance);

        await handler.Handle(Evento([]), CancellationToken.None);

        sender.Enviados.Should().BeEmpty();
    }

    [Fact] // MSC com saldo final ⇒ consolida (envia ConsolidarDeclaracaoFiscalCommand com as linhas).
    public async Task Msc_com_saldo_final_dispara_consolidacao()
    {
        var sender = new SpiaoSender();
        var handler = new ReceberMSCGeradaHandler(sender, NullLogger<ReceberMSCGeradaHandler>.Instance);

        await handler.Handle(
            Evento(
            [
                new LinhaMscDto("111110100", 1, TipoValorSaldoFinal, 1_000m, null, null),
                new LinhaMscDto("211110000", 2, TipoValorSaldoFinal, 1_000m, null, null),
                // movimento entra na MSC mas NAO compoe a declaracao (filtrado por TipoValor).
                new LinhaMscDto("111110100", 1, TipoValorSaldoInicial, 500m, null, null),
            ]),
            CancellationToken.None);

        var comando = sender.Enviados.Should().ContainSingle()
            .Which.Should().BeOfType<ConsolidarDeclaracaoFiscalCommand>().Subject;
        comando.TipoDeclaracao.Should().Be(Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais.TipoDeclaracaoFiscal.Msc);
        comando.Exercicio.Should().Be(2026);
        comando.Mes.Should().Be(6);
        comando.Linhas.Should().HaveCount(2, "apenas as linhas de saldo final compoem a declaracao");
    }

    /// <summary>Espião de <see cref="ISender"/> que captura os comandos enviados (sem executá-los).</summary>
    private sealed class SpiaoSender : ISender
    {
        public List<object> Enviados { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Enviados.Add(request);
            return Task.FromResult<TResponse>(default!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            Enviados.Add(request!);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Enviados.Add(request);
            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
