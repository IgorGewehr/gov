using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.Pca;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Application.Pca;

/// <summary>Abre o Plano de Contratacoes Anual do exercicio (art. 12, VII, Lei 14.133/2021), nasce EmElaboracao.</summary>
/// <param name="Exercicio">Ano do exercicio (ex.: 2027).</param>
public sealed record AbrirPcaCommand(int Exercicio) : ICommand<Guid>;

/// <summary>Inclui um item de contratacao pretendida no PCA em elaboracao.</summary>
/// <param name="PcaId">Identificador do plano.</param>
/// <param name="ItemCatalogoId">Item de catalogo a contratar.</param>
/// <param name="Quantidade">Quantidade pretendida.</param>
/// <param name="ValorEstimado">Valor estimado total.</param>
/// <param name="TrimestreDesejado">Trimestre desejado (1 a 4).</param>
/// <param name="Justificativa">Justificativa (opcional).</param>
public sealed record IncluirItemPcaCommand(
    Guid PcaId,
    Guid ItemCatalogoId,
    decimal Quantidade,
    decimal ValorEstimado,
    int TrimestreDesejado,
    string? Justificativa) : ICommand<Guid>;

/// <summary>Remove um item do PCA em elaboracao.</summary>
/// <param name="PcaId">Identificador do plano.</param>
/// <param name="ItemPcaId">Item a remover.</param>
public sealed record RemoverItemPcaCommand(Guid PcaId, Guid ItemPcaId) : ICommand;

/// <summary>Aprova o PCA pela autoridade competente (congela itens).</summary>
/// <param name="PcaId">Identificador do plano.</param>
public sealed record AprovarPcaCommand(Guid PcaId) : ICommand;

/// <summary>Marca o PCA como publicado no PNCP (art. 12, par. 1º).</summary>
/// <param name="PcaId">Identificador do plano.</param>
/// <param name="NumeroPncp">Numero de controle no PNCP.</param>
public sealed record PublicarPcaNoPncpCommand(Guid PcaId, string NumeroPncp) : ICommand;

/// <summary>Reabre o PCA vigente para revisao formal (alteracao de itens apos aprovacao/publicacao).</summary>
/// <param name="PcaId">Identificador do plano.</param>
/// <param name="Motivo">Motivacao do ato administrativo de revisao.</param>
public sealed record RevisarPcaCommand(Guid PcaId, string Motivo) : ICommand;

/// <summary>
/// Vincula um item do PCA a contratacao que o concretizou (licitacao/ata/dispensa/inexigibilidade),
/// rastreando o cumprimento do planejamento (art. 12, VII; Dec. 11.246/2022).
/// </summary>
/// <param name="PcaId">Identificador do plano.</param>
/// <param name="ItemPcaId">Item planejado a concretizar.</param>
/// <param name="Fonte">Natureza do instrumento (licitacao/ata/dispensa/inexigibilidade).</param>
/// <param name="ReferenciaId">Identificador do agregado gerado.</param>
/// <param name="Identificacao">Identificacao legivel do instrumento (opcional).</param>
public sealed record VincularContratacaoItemPcaCommand(
    Guid PcaId,
    Guid ItemPcaId,
    FonteContratacaoPca Fonte,
    Guid ReferenciaId,
    string? Identificacao) : ICommand;

/// <summary>Regras de validacao da abertura do PCA.</summary>
public sealed class AbrirPcaValidator : AbstractValidator<AbrirPcaCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirPcaValidator() => RuleFor(c => c.Exercicio).InclusiveBetween(2000, 2100);
}

/// <summary>Regras de validacao da inclusao de item no PCA.</summary>
public sealed class IncluirItemPcaValidator : AbstractValidator<IncluirItemPcaCommand>
{
    /// <summary>Define as regras.</summary>
    public IncluirItemPcaValidator()
    {
        RuleFor(c => c.PcaId).NotEmpty();
        RuleFor(c => c.ItemCatalogoId).NotEmpty();
        RuleFor(c => c.Quantidade).GreaterThan(0);
        RuleFor(c => c.ValorEstimado).GreaterThan(0);
        RuleFor(c => c.TrimestreDesejado).InclusiveBetween(1, 4);
        RuleFor(c => c.Justificativa).MaximumLength(2000);
    }
}

/// <summary>Handler da abertura do PCA. Garante unicidade por exercicio.</summary>
public sealed class AbrirPcaHandler(IPcaRepository planos, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<AbrirPcaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirPcaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await planos.ExistePorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe um PCA para este exercicio no tenant.");
        }

        var plano = PlanoContratacoes.Abrir(tenant.TenantId, request.Exercicio);
        planos.Adicionar(plano);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return plano.Id.Value;
    }
}

/// <summary>Handler da inclusao de item no PCA. Valida item de catalogo ativo.</summary>
public sealed class IncluirItemPcaHandler(
    IPcaRepository planos,
    ICatalogoRepository catalogo,
    IUnitOfWork unitOfWork)
    : ICommandHandler<IncluirItemPcaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(IncluirItemPcaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoContratacoesId(request.PcaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PCA nao encontrado.");

        var item = await catalogo.ObterPorIdAsync(new ItemCatalogoId(request.ItemCatalogoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Item de catalogo nao encontrado.");
        if (item.Situacao != SituacaoItemCatalogo.Ativo)
        {
            throw new InvalidOperationException("Item de catalogo inativo nao pode entrar no PCA.");
        }

        var itemPcaId = plano.IncluirItem(
            item.Id,
            request.Quantidade,
            ValorMonetario.De(request.ValorEstimado),
            request.TrimestreDesejado,
            request.Justificativa);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return itemPcaId.Value;
    }
}

/// <summary>Handler da remocao de item do PCA.</summary>
public sealed class RemoverItemPcaHandler(IPcaRepository planos, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoverItemPcaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RemoverItemPcaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoContratacoesId(request.PcaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PCA nao encontrado.");
        plano.RemoverItem(new ItemPcaId(request.ItemPcaId));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da aprovacao do PCA.</summary>
public sealed class AprovarPcaHandler(IPcaRepository planos, IUnitOfWork unitOfWork)
    : ICommandHandler<AprovarPcaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AprovarPcaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoContratacoesId(request.PcaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PCA nao encontrado.");
        plano.Aprovar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler da publicacao do PCA no PNCP. TODO(M10): integrar transmissao real via IPncpGateway.</summary>
public sealed class PublicarPcaNoPncpHandler(IPcaRepository planos, IUnitOfWork unitOfWork)
    : ICommandHandler<PublicarPcaNoPncpCommand>
{
    /// <inheritdoc />
    public async Task Handle(PublicarPcaNoPncpCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoContratacoesId(request.PcaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PCA nao encontrado.");
        plano.PublicarNoPncp(request.NumeroPncp);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Validacao da revisao do PCA.</summary>
public sealed class RevisarPcaValidator : AbstractValidator<RevisarPcaCommand>
{
    /// <summary>Define as regras.</summary>
    public RevisarPcaValidator()
    {
        RuleFor(c => c.PcaId).NotEmpty();
        RuleFor(c => c.Motivo).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>Validacao do vinculo de contratacao a item do PCA.</summary>
public sealed class VincularContratacaoItemPcaValidator : AbstractValidator<VincularContratacaoItemPcaCommand>
{
    /// <summary>Define as regras.</summary>
    public VincularContratacaoItemPcaValidator()
    {
        RuleFor(c => c.PcaId).NotEmpty();
        RuleFor(c => c.ItemPcaId).NotEmpty();
        RuleFor(c => c.Fonte).IsInEnum();
        RuleFor(c => c.ReferenciaId).NotEmpty();
        RuleFor(c => c.Identificacao).MaximumLength(200);
    }
}

/// <summary>Handler da revisao do PCA (reabre o plano vigente para alteracao formal de itens).</summary>
public sealed class RevisarPcaHandler(IPcaRepository planos, IUnitOfWork unitOfWork)
    : ICommandHandler<RevisarPcaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RevisarPcaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoContratacoesId(request.PcaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PCA nao encontrado.");
        plano.Revisar(request.Motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Handler do vinculo de contratacao a item do PCA (rastreabilidade planejamento -> execucao).</summary>
public sealed class VincularContratacaoItemPcaHandler(IPcaRepository planos, IUnitOfWork unitOfWork)
    : ICommandHandler<VincularContratacaoItemPcaCommand>
{
    /// <inheritdoc />
    public async Task Handle(VincularContratacaoItemPcaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var plano = await planos.ObterPorIdAsync(new PlanoContratacoesId(request.PcaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PCA nao encontrado.");
        plano.VincularContratacaoItem(
            new ItemPcaId(request.ItemPcaId),
            ContratacaoVinculada.Criar(request.Fonte, request.ReferenciaId, request.Identificacao));
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
