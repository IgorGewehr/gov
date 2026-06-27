using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Credenciamentos;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

namespace Tensorroot.Gov.Modules.Administracao.Application.Credenciamentos;

/// <summary>Inscreve um interessado no credenciamento (protocolo de adesao — ingresso a qualquer tempo).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="FornecedorId">Fornecedor interessado (ja cadastrado).</param>
public sealed record InscreverInteressadoCommand(Guid CredenciamentoId, Guid FornecedorId) : ICommand<Guid>;

/// <summary>Defere a inscricao (habilitacao documental): o interessado torna-se credenciado apto.</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
public sealed record DeferirInscricaoCommand(Guid CredenciamentoId, Guid CredenciadoId) : ICommand;

/// <summary>Indefere a inscricao por nao atendimento das condicoes do edital.</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="Motivo">Motivacao do indeferimento.</param>
public sealed record IndeferirInscricaoCommand(Guid CredenciamentoId, Guid CredenciadoId, string Motivo) : ICommand;

/// <summary>Suspende/reabilita um credenciado (descumprimento sanavel).</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="Suspender"><c>true</c> para suspender; <c>false</c> para reabilitar.</param>
/// <param name="Motivo">Motivacao da suspensao (exigida ao suspender).</param>
public sealed record AlterarCredenciadoCommand(Guid CredenciamentoId, Guid CredenciadoId, bool Suspender, string? Motivo) : ICommand;

/// <summary>Descredencia um interessado (terminal): a pedido, descumprimento ou sancao impeditiva.</summary>
/// <param name="CredenciamentoId">Identificador do credenciamento.</param>
/// <param name="CredenciadoId">Identificador da inscricao.</param>
/// <param name="Motivo">Motivacao do ato.</param>
public sealed record DescredenciarCommand(Guid CredenciamentoId, Guid CredenciadoId, string Motivo) : ICommand;

/// <summary>Validacao da inscricao.</summary>
public sealed class InscreverInteressadoValidator : AbstractValidator<InscreverInteressadoCommand>
{
    /// <summary>Define as regras.</summary>
    public InscreverInteressadoValidator()
    {
        RuleFor(c => c.CredenciamentoId).NotEmpty();
        RuleFor(c => c.FornecedorId).NotEmpty();
    }
}

/// <summary>Validacao do indeferimento.</summary>
public sealed class IndeferirInscricaoValidator : AbstractValidator<IndeferirInscricaoCommand>
{
    /// <summary>Define as regras.</summary>
    public IndeferirInscricaoValidator()
    {
        RuleFor(c => c.CredenciamentoId).NotEmpty();
        RuleFor(c => c.CredenciadoId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>Validacao do descredenciamento.</summary>
public sealed class DescredenciarValidator : AbstractValidator<DescredenciarCommand>
{
    /// <summary>Define as regras.</summary>
    public DescredenciarValidator()
    {
        RuleFor(c => c.CredenciamentoId).NotEmpty();
        RuleFor(c => c.CredenciadoId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>Handler da inscricao de interessado. Exige que o fornecedor exista no tenant.</summary>
public sealed class InscreverInteressadoHandler(
    ICredenciamentoRepository credenciamentos,
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<InscreverInteressadoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(InscreverInteressadoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");

        _ = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Fornecedor interessado nao encontrado no tenant; cadastre-o antes de inscrever.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var credenciadoId = credenciamento.Inscrever(request.FornecedorId, hoje);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return credenciadoId.Value;
    }
}

/// <summary>
/// Handler do deferimento. Fail-closed: afere a aptidao do fornecedor (sancao impeditiva vigente) no
/// limite de agregado e passa-a ao Credenciamento, que recusa credenciar impedido (art. 14/156).
/// </summary>
public sealed class DeferirInscricaoHandler(
    ICredenciamentoRepository credenciamentos,
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<DeferirInscricaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(DeferirInscricaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");

        var inscricao = credenciamento.Credenciados.FirstOrDefault(c => c.Id == new CredenciadoId(request.CredenciadoId))
            ?? throw new InvalidOperationException("Inscricao nao encontrada neste credenciamento.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(inscricao.FornecedorId), cancellationToken).ConfigureAwait(false);
        var fornecedorImpedido = fornecedor is not null && fornecedor.EstaImpedido(hoje);

        credenciamento.DeferirInscricao(new CredenciadoId(request.CredenciadoId), hoje, fornecedorImpedido);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do indeferimento.</summary>
public sealed class IndeferirInscricaoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<IndeferirInscricaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(IndeferirInscricaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        credenciamento.IndeferirInscricao(new CredenciadoId(request.CredenciadoId), request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da suspensao/reabilitacao de credenciado.</summary>
public sealed class AlterarCredenciadoHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork)
    : ICommandHandler<AlterarCredenciadoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AlterarCredenciadoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        var credenciadoId = new CredenciadoId(request.CredenciadoId);
        if (request.Suspender)
        {
            credenciamento.SuspenderCredenciado(credenciadoId, request.Motivo ?? throw new InvalidOperationException("Motivo da suspensao e obrigatorio."));
        }
        else
        {
            credenciamento.ReabilitarCredenciado(credenciadoId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do descredenciamento.</summary>
public sealed class DescredenciarHandler(ICredenciamentoRepository credenciamentos, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<DescredenciarCommand>
{
    /// <inheritdoc />
    public async Task Handle(DescredenciarCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var credenciamento = await credenciamentos.ObterPorIdAsync(new CredenciamentoId(request.CredenciamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Credenciamento nao encontrado.");
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        credenciamento.Descredenciar(new CredenciadoId(request.CredenciadoId), hoje, request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
