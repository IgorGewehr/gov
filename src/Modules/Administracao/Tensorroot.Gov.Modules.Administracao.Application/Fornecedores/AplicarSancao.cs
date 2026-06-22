using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Fornecedores;

/// <summary>Aplica uma sancao administrativa ao fornecedor (Lei 14.133/2021, art. 156).</summary>
/// <param name="FornecedorId">Fornecedor sancionado.</param>
/// <param name="Tipo">Tipo da sancao.</param>
/// <param name="DataInicio">Inicio da vigencia.</param>
/// <param name="DataFim">Termo final da vigencia (opcional).</param>
/// <param name="ProcessoAdministrativo">Processo administrativo (devido processo legal).</param>
/// <param name="Fundamentacao">Fundamentacao/motivacao do ato.</param>
/// <param name="ValorMulta">Valor da multa, quando o tipo for <see cref="TipoSancao.Multa"/>.</param>
public sealed record AplicarSancaoCommand(
    Guid FornecedorId,
    TipoSancao Tipo,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string ProcessoAdministrativo,
    string Fundamentacao,
    decimal? ValorMulta) : ICommand<Guid>;

/// <summary>Regras de validacao da aplicacao de sancao.</summary>
public sealed class AplicarSancaoValidator : AbstractValidator<AplicarSancaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AplicarSancaoValidator()
    {
        RuleFor(comando => comando.FornecedorId).NotEmpty().WithMessage("Fornecedor e obrigatorio.");
        RuleFor(comando => comando.Tipo).IsInEnum().WithMessage("Tipo de sancao invalido.");
        RuleFor(comando => comando.ProcessoAdministrativo)
            .NotEmpty()
            .MaximumLength(60)
            .WithMessage("Processo administrativo e obrigatorio.");
        RuleFor(comando => comando.Fundamentacao).NotEmpty().WithMessage("Fundamentacao e obrigatoria.");
        RuleFor(comando => comando.DataFim)
            .GreaterThanOrEqualTo(comando => comando.DataInicio)
            .When(comando => comando.DataFim.HasValue)
            .WithMessage("Data fim nao pode ser anterior ao inicio.");
        RuleFor(comando => comando.ValorMulta)
            .NotNull()
            .Must(valor => valor > 0)
            .When(comando => comando.Tipo == TipoSancao.Multa)
            .WithMessage("Valor da multa deve ser positivo.");
    }
}

/// <summary>Handler da aplicacao de sancao.</summary>
public sealed class AplicarSancaoHandler(
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    TimeProvider timeProvider)
    : ICommandHandler<AplicarSancaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AplicarSancaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fornecedor nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var valorMulta = request.ValorMulta is { } valor ? ValorMonetario.De(valor) : null;

        var sancaoId = fornecedor.AplicarSancao(
            request.Tipo,
            request.DataInicio,
            request.DataFim,
            request.ProcessoAdministrativo,
            request.Fundamentacao,
            valorMulta,
            hoje);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new FornecedorSancionadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            fornecedor.TenantId,
            fornecedor.Id.Value,
            fornecedor.Cnpj.Digitos,
            request.Tipo.ToString(),
            request.DataInicio,
            request.DataFim);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return sancaoId.Value;
    }
}
