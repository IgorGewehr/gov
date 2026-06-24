using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
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
    IFornecedorRepository fornecedores,
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

        var agora = timeProvider.GetUtcNow();

        // BUG-A1: aferir a aptidao do fornecedor (sancao impeditiva vigente) no limite de agregado e passa-la
        // ao agregado Licitacao, que recusa fail-closed a habilitacao de impedido (art. 14/156 Lei 14.133/2021).
        var fornecedorImpedido = await EstaImpedidoAsync(request.FornecedorId, agora, cancellationToken).ConfigureAwait(false);

        // BUG-A4: o relogio externo (TimeProvider) fornece a DataVerificacao; o agregado nao le o relogio.
        licitacao.HabilitarLicitante(request.FornecedorId, request.Resultado, request.Motivo, agora, fornecedorImpedido);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> EstaImpedidoAsync(Guid fornecedorId, DateTimeOffset referencia, CancellationToken cancellationToken)
    {
        // Fail-closed: fornecedor inexistente no cadastro do tenant nao tem sancao a confrontar; a habilitacao
        // de fornecedor nao cadastrado segue barrada pelas demais regras da licitacao, nao por esta guarda.
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(fornecedorId), cancellationToken).ConfigureAwait(false);
        return fornecedor is not null && fornecedor.EstaImpedido(DateOnly.FromDateTime(referencia.UtcDateTime));
    }
}
