using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>
/// Divulga o contrato no PNCP — condicao de eficacia (Lei 14.133/2021, art. 94). NAO recebe mais o numero
/// de controle de fora: a transmissao e feita pela ACL (<see cref="IPncpGateway"/>), que devolve o numero
/// de controle PNCP oficial. (CORRECAO LEGAL: art. 174 institui o PNCP; a eficacia e do art. 94.)
/// </summary>
/// <param name="ContratoId">Contrato a divulgar.</param>
/// <param name="CnpjOrgao">CNPJ do orgao/entidade comprador (pre-cadastro PNCP).</param>
/// <param name="CodigoUnidade">Codigo da unidade administrativa compradora.</param>
/// <param name="NumeroContratoInterno">Numero do contrato no ente (ex.: "0012/2026").</param>
/// <param name="DocumentoFornecedor">CPF/CNPJ do fornecedor contratado.</param>
public sealed record PublicarContratoNoPncpCommand(
    Guid ContratoId,
    string CnpjOrgao,
    string CodigoUnidade,
    string NumeroContratoInterno,
    string DocumentoFornecedor) : ICommand;

/// <summary>Regras de validacao da publicacao no PNCP.</summary>
public sealed class PublicarContratoNoPncpValidator : AbstractValidator<PublicarContratoNoPncpCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarContratoNoPncpValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
        RuleFor(comando => comando.CnpjOrgao).NotEmpty().MaximumLength(14);
        RuleFor(comando => comando.CodigoUnidade).NotEmpty().MaximumLength(30);
        RuleFor(comando => comando.NumeroContratoInterno).NotEmpty().MaximumLength(30);
        RuleFor(comando => comando.DocumentoFornecedor).NotEmpty().MaximumLength(14);
    }
}

/// <summary>
/// Handler da divulgacao no PNCP: transmite via <see cref="IPncpGateway"/> (idempotente + Polly), grava o
/// numero de controle PNCP no contrato (eficacia — art. 94), registra a tempestividade e enfileira o
/// <see cref="ContratoPublicadoPncpIntegrationEvent"/> no OUTBOX (consistencia transacional; entrega
/// cross-module em escopo dedicado por modulo na drenagem — guarda H5).
/// <para>
/// FRONTEIRA M9/M10: no M9 a <see cref="IPncpGateway"/> e a impl. SIMULADA (numero deterministico,
/// testavel contra WireMock). // TODO(M10): transmissao real ao PNCP de producao (JWT/credenciais).
/// </para>
/// </summary>
public sealed class PublicarContratoNoPncpHandler(
    IContratoRepository contratos,
    IPncpGateway pncpGateway,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<PublicarContratoNoPncpCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarContratoNoPncpCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contrato = await contratos.ObterPorIdAsync(new ContratoId(request.ContratoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contrato nao encontrado.");

        // Idempotencia: ja divulgado => no-op (o gateway tambem e idempotente, mas evitamos transmissao).
        if (contrato.PublicadoNoPncp)
        {
            return;
        }

        // Transmissao via ACL (Polly + idempotencia por chave). A chave de idempotencia amarra a
        // operacao ao contrato — replays do Outbox nao geram registro duplicado no PNCP.
        var requisicao = new PublicacaoContratoPncpRequest(
            contrato.Id.Value,
            request.CnpjOrgao,
            request.CodigoUnidade,
            request.NumeroContratoInterno,
            contrato.Objeto,
            contrato.ValorContratado.Valor,
            contrato.DataAssinatura,
            contrato.VigenciaInicio,
            contrato.VigenciaFim,
            request.DocumentoFornecedor,
            ChaveIdempotencia: $"contrato:{contrato.Id.Value:N}");

        var resultado = await pncpGateway.PublicarContratoAsync(requisicao, cancellationToken).ConfigureAwait(false);
        if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.NumeroControlePncp))
        {
            // Falha de transmissao: lanca para que o pipeline/Outbox reprocesse (resiliencia at-least-once).
            throw new InvalidOperationException(
                $"Falha ao divulgar contrato no PNCP ({resultado.CodigoErro}): {resultado.MensagemErro}");
        }

        var dataPublicacao = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        contrato.PublicarContratoPncp(resultado.NumeroControlePncp, dataPublicacao);

        var evento = new ContratoPublicadoPncpIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            contrato.Id.Value,
            resultado.NumeroControlePncp);

        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
