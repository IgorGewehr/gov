using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Licitacoes;

namespace Tensorroot.Gov.Modules.Administracao.Application.Licitacoes;

/// <summary>Verifica a habilitacao de um licitante (juridica, fiscal, tecnica e economica).</summary>
/// <param name="LicitacaoId">Identificador da licitacao.</param>
/// <param name="FornecedorId">Licitante verificado.</param>
/// <param name="Resultado">Resultado da habilitacao.</param>
/// <param name="Motivo">Motivo (opcional).</param>
public sealed record HabilitarLicitanteCommand(
    Guid LicitacaoId,
    Guid FornecedorId,
    ResultadoHabilitacao Resultado,
    string? Motivo) : ICommand;

/// <summary>Regras de validacao da habilitacao de licitante.</summary>
public sealed class HabilitarLicitanteValidator : AbstractValidator<HabilitarLicitanteCommand>
{
    /// <summary>Define as regras.</summary>
    public HabilitarLicitanteValidator()
    {
        RuleFor(comando => comando.LicitacaoId).NotEmpty().WithMessage("Licitacao e obrigatoria.");
        RuleFor(comando => comando.FornecedorId).NotEmpty().WithMessage("Fornecedor e obrigatorio.");
        RuleFor(comando => comando.Resultado).IsInEnum().WithMessage("Resultado de habilitacao invalido.");
    }
}

/// <summary>Handler da habilitacao de licitante.</summary>
public sealed class HabilitarLicitanteHandler(
    ILicitacaoRepository licitacoes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<HabilitarLicitanteCommand>
{
    /// <inheritdoc />
    public async Task Handle(HabilitarLicitanteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var licitacao = await licitacoes.ObterPorIdAsync(new LicitacaoId(request.LicitacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Licitacao nao encontrada.");

        // BUG-A4: o relogio externo (TimeProvider) fornece a DataVerificacao; o agregado nao le o relogio.
        licitacao.HabilitarLicitante(request.FornecedorId, request.Resultado, request.Motivo, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
