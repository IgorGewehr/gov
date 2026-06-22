using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Empenhos;

/// <summary>Emite um novo empenho onerando uma dotação.</summary>
/// <param name="Numero">Número do empenho.</param>
/// <param name="DotacaoId">Dotação onerada.</param>
/// <param name="TipoEmpenho">Tipo de empenho.</param>
/// <param name="Exercicio">Exercício orçamentário.</param>
/// <param name="DataEmpenho">Data do empenho.</param>
/// <param name="CredorNome">Nome/razão social do credor.</param>
/// <param name="CredorTipo">Tipo de pessoa do credor.</param>
/// <param name="CredorDocumento">Documento (CPF/CNPJ) do credor.</param>
/// <param name="Valor">Valor a empenhar.</param>
public sealed record EmpenharCommand(
    string Numero,
    Guid DotacaoId,
    TipoEmpenho TipoEmpenho,
    int Exercicio,
    DateOnly DataEmpenho,
    string CredorNome,
    TipoPessoa CredorTipo,
    string CredorDocumento,
    decimal Valor) : ICommand<Guid>;

/// <summary>Regras de validação da emissão de empenho.</summary>
public sealed class EmpenharValidator : AbstractValidator<EmpenharCommand>
{
    /// <summary>Define as regras.</summary>
    public EmpenharValidator()
    {
        RuleFor(c => c.Numero).NotEmpty().MaximumLength(30);
        RuleFor(c => c.DotacaoId).NotEmpty();
        RuleFor(c => c.TipoEmpenho).IsInEnum();
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
        RuleFor(c => c.CredorNome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.CredorTipo).IsInEnum();
        RuleFor(c => c.CredorDocumento).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0m);
    }
}

/// <summary>
/// Handler da emissão de empenho. Orquestra o invariante cross-aggregate de saldo:
/// debita a dotação (<see cref="DotacaoOrcamentaria.ReservarEmpenho"/>) e cria o empenho
/// na mesma transação.
/// </summary>
public sealed class EmpenharHandler(
    IEmpenhoRepository empenhos,
    IDotacaoOrcamentariaRepository dotacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<EmpenharCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EmpenharCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var dotacao = await dotacoes.ObterPorIdAsync(new DotacaoOrcamentariaId(request.DotacaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dotacao nao encontrada.");

        var valor = ValorMonetario.De(request.Valor);
        var credor = Credor.De(request.CredorNome, request.CredorTipo, request.CredorDocumento);

        // Invariante central (Lei 4.320/64, art. 60): nao empenhar acima do saldo da dotacao.
        dotacao.ReservarEmpenho(valor);

        var empenho = Empenho.Emitir(
            tenant.TenantId,
            request.Numero,
            dotacao.Id,
            credor,
            valor,
            request.TipoEmpenho,
            request.Exercicio,
            request.DataEmpenho);

        empenhos.Adicionar(empenho);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return empenho.Id.Value;
    }
}
