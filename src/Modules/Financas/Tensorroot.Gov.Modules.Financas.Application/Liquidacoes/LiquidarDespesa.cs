using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Empenhos;
using Tensorroot.Gov.Modules.Financas.Domain.Liquidacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Liquidacoes;

/// <summary>Liquida uma despesa empenhada (2º estágio — Lei 4.320/64, art. 63).</summary>
/// <param name="EmpenhoId">Empenho vinculado.</param>
/// <param name="Valor">Valor a liquidar.</param>
/// <param name="DataLiquidacao">Data da liquidação.</param>
/// <param name="TipoDocumento">Tipo do documento comprobatório.</param>
/// <param name="NumeroDocumento">Número do documento (quando aplicável).</param>
/// <param name="ChaveNfse">Chave de acesso da NFS-e (quando NFS-e).</param>
/// <param name="DataEmissaoDocumento">Data de emissão do documento.</param>
public sealed record LiquidarDespesaCommand(
    Guid EmpenhoId,
    decimal Valor,
    DateOnly DataLiquidacao,
    TipoDocumentoComprobatorio TipoDocumento,
    string? NumeroDocumento,
    string? ChaveNfse,
    DateOnly? DataEmissaoDocumento) : ICommand<Guid>;

/// <summary>Regras de validação da liquidação.</summary>
public sealed class LiquidarDespesaValidator : AbstractValidator<LiquidarDespesaCommand>
{
    /// <summary>Define as regras.</summary>
    public LiquidarDespesaValidator()
    {
        RuleFor(c => c.EmpenhoId).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0m);
        RuleFor(c => c.TipoDocumento).IsInEnum();
        RuleFor(c => c.ChaveNfse)
            .NotEmpty()
            .Length(DocumentoComprobatorio.TamanhoChaveNfse)
            .When(c => c.TipoDocumento == TipoDocumentoComprobatorio.NfseChaveAcesso);
        RuleFor(c => c.NumeroDocumento)
            .NotEmpty()
            .When(c => c.TipoDocumento == TipoDocumentoComprobatorio.NotaFiscal);
    }
}

/// <summary>
/// Handler da liquidação. Cria a <see cref="Liquidacao"/> e aplica o invariante de saldo no
/// empenho (<see cref="Empenho.RegistrarLiquidacao"/>) na mesma transação.
/// </summary>
public sealed class LiquidarDespesaHandler(
    ILiquidacaoRepository liquidacoes,
    IEmpenhoRepository empenhos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<LiquidarDespesaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(LiquidarDespesaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var empenho = await empenhos.ObterPorIdAsync(new EmpenhoId(request.EmpenhoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empenho nao encontrado.");

        var valor = ValorMonetario.De(request.Valor);
        var documento = MontarDocumento(request);

        // Invariante (Lei 4.320/64, art. 63): valor liquidado <= saldo a liquidar do empenho.
        empenho.RegistrarLiquidacao(valor);

        var liquidacao = Liquidacao.Registrar(tenant.TenantId, empenho.Id, valor, request.DataLiquidacao, documento);

        liquidacoes.Adicionar(liquidacao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return liquidacao.Id.Value;
    }

    private static DocumentoComprobatorio MontarDocumento(LiquidarDespesaCommand request) => request.TipoDocumento switch
    {
        TipoDocumentoComprobatorio.NfseChaveAcesso => DocumentoComprobatorio.NfsE(request.ChaveNfse!, request.DataEmissaoDocumento),
        TipoDocumentoComprobatorio.NotaFiscal => DocumentoComprobatorio.NotaFiscal(request.NumeroDocumento!, request.DataEmissaoDocumento ?? request.DataLiquidacao),
        _ => DocumentoComprobatorio.Generico(request.TipoDocumento, request.NumeroDocumento, request.DataEmissaoDocumento),
    };
}
