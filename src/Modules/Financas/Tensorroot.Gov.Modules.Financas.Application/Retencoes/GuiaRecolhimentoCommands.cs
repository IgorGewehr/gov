using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Recolhimentos;
using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>Retenção a incluir na guia (referência a uma consignação pendente).</summary>
/// <param name="LiquidacaoId">Liquidação que apurou a retenção.</param>
/// <param name="RetencaoId">Retenção a recolher.</param>
public sealed record ItemGuiaPayload(Guid LiquidacaoId, Guid RetencaoId);

/// <summary>Emite uma guia de recolhimento reunindo retenções pendentes de mesma natureza.</summary>
/// <param name="Natureza">Natureza recolhida.</param>
/// <param name="CodigoReceita">Código de receita (DARF/GPS/guia).</param>
/// <param name="FavorecidoDocumento">Documento do favorecido.</param>
/// <param name="DataVencimento">Vencimento.</param>
/// <param name="Competencia">Competência.</param>
/// <param name="Itens">Retenções a recolher.</param>
public sealed record EmitirGuiaRecolhimentoCommand(
    NaturezaRetencao Natureza,
    string? CodigoReceita,
    string? FavorecidoDocumento,
    DateOnly DataVencimento,
    DateOnly Competencia,
    IReadOnlyList<ItemGuiaPayload> Itens) : ICommand<Guid>;

/// <summary>Validação da emissão de guia.</summary>
public sealed class EmitirGuiaRecolhimentoValidator : AbstractValidator<EmitirGuiaRecolhimentoCommand>
{
    /// <summary>Define as regras.</summary>
    public EmitirGuiaRecolhimentoValidator()
    {
        RuleFor(c => c.Natureza).IsInEnum();
        RuleFor(c => c.Itens).NotEmpty();
    }
}

/// <summary>Handler da emissão de guia: valida e baixa cada retenção, vinculando-a à guia.</summary>
public sealed class EmitirGuiaRecolhimentoHandler(
    IGuiaRecolhimentoRepository guias,
    ILiquidacaoRepository liquidacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<EmitirGuiaRecolhimentoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EmitirGuiaRecolhimentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var guia = GuiaRecolhimento.Emitir(
            tenant.TenantId,
            request.Natureza,
            request.CodigoReceita,
            request.FavorecidoDocumento,
            request.DataVencimento,
            request.Competencia);

        foreach (var item in request.Itens)
        {
            var liquidacao = await liquidacoes.ObterPorIdAsync(new LiquidacaoId(item.LiquidacaoId), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Liquidacao {item.LiquidacaoId} nao encontrada.");

            var retencao = liquidacao.Retencoes.FirstOrDefault(r => r.Id == new RetencaoId(item.RetencaoId))
                ?? throw new InvalidOperationException($"Retencao {item.RetencaoId} nao encontrada na liquidacao {item.LiquidacaoId}.");

            if (retencao.Recolhida)
            {
                throw new InvalidOperationException($"Retencao {item.RetencaoId} ja recolhida.");
            }

            if (retencao.Natureza != request.Natureza)
            {
                throw new InvalidOperationException($"Retencao {item.RetencaoId} ({retencao.Natureza}) nao pertence a natureza da guia ({request.Natureza}).");
            }

            retencao.MarcarRecolhida(guia.Id.Value);
            guia.AdicionarItem(new ItemGuiaRecolhimento(liquidacao.Id, retencao.Id, retencao.Valor.Valor));
        }

        guias.Adicionar(guia);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return guia.Id.Value;
    }
}

/// <summary>Registra o recolhimento efetivo de uma guia (baixa do passivo extra-orçamentário).</summary>
/// <param name="GuiaRecolhimentoId">Guia a recolher.</param>
/// <param name="DataRecolhimento">Data do recolhimento.</param>
public sealed record RecolherGuiaCommand(Guid GuiaRecolhimentoId, DateOnly DataRecolhimento) : ICommand;

/// <summary>Handler do recolhimento.</summary>
public sealed class RecolherGuiaHandler(
    IGuiaRecolhimentoRepository guias,
    IUnitOfWork unitOfWork) : ICommandHandler<RecolherGuiaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RecolherGuiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var guia = await guias.ObterPorIdAsync(new GuiaRecolhimentoId(request.GuiaRecolhimentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Guia de recolhimento nao encontrada.");

        guia.Recolher(request.DataRecolhimento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Cancela uma guia ainda não recolhida (reabre as retenções não é automático no PoC).</summary>
/// <param name="GuiaRecolhimentoId">Guia a cancelar.</param>
public sealed record CancelarGuiaCommand(Guid GuiaRecolhimentoId) : ICommand;

/// <summary>Handler do cancelamento de guia.</summary>
public sealed class CancelarGuiaHandler(
    IGuiaRecolhimentoRepository guias,
    ILiquidacaoRepository liquidacoes,
    IUnitOfWork unitOfWork) : ICommandHandler<CancelarGuiaCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarGuiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var guia = await guias.ObterPorIdAsync(new GuiaRecolhimentoId(request.GuiaRecolhimentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Guia de recolhimento nao encontrada.");

        // Reabre as retenções vinculadas (volta a consignação a pendente) antes de cancelar.
        foreach (var item in guia.Itens)
        {
            var liquidacao = await liquidacoes.ObterPorIdAsync(item.LiquidacaoId, cancellationToken).ConfigureAwait(false);
            var retencao = liquidacao?.Retencoes.FirstOrDefault(r => r.Id == item.RetencaoId);
            retencao?.ReabrirRecolhimento(guia.Id.Value);
        }

        guia.Cancelar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
