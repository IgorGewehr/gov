using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Contracts;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
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
/// empenho (<see cref="Empenho.RegistrarLiquidacao"/>) na mesma transação. Enfileira no Outbox
/// (consistência transacional — padrão FecharFolha/GerarMsc) o <see cref="DespesaLiquidadaIntegrationEvent"/>
/// (2º estágio da despesa), que fecha o trio empenhado/liquidado/pago da execução orçamentária no
/// Painel do Gestor. A classificação por função/fonte vem da dotação onerada pelo empenho.
/// </summary>
public sealed class LiquidarDespesaHandler(
    ILiquidacaoRepository liquidacoes,
    IEmpenhoRepository empenhos,
    IDotacaoOrcamentariaRepository dotacoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IIntegrationEventWriter integrationEvents,
    TimeProvider timeProvider) : ICommandHandler<LiquidarDespesaCommand, Guid>
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

        // Classificação por função/fonte da dotação onerada (insumo do read model setorial do Painel).
        var dotacao = await dotacoes.ObterPorIdAsync(empenho.DotacaoId, cancellationToken).ConfigureAwait(false);
        var evento = new DespesaLiquidadaIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            liquidacao.Id.Value,
            empenho.Id.Value,
            valor.Valor,
            request.DataLiquidacao,
            dotacao is null ? null : ExtrairFuncaoSubfuncao(dotacao.Classificacao.FuncionalProgramatica),
            dotacao?.Classificacao.FonteDeRecurso,
            new DateOnly(request.DataLiquidacao.Year, request.DataLiquidacao.Month, 1));

        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return liquidacao.Id.Value;
    }

    // FS (função+subfunção) = 2 primeiros segmentos da funcional-programática; tolerante a formato.
    private static string? ExtrairFuncaoSubfuncao(string funcionalProgramatica)
    {
        var partes = funcionalProgramatica.Split('.');
        if (partes.Length < 2)
        {
            return null;
        }

        var fs = partes[0] + partes[1];
        return fs.All(char.IsDigit) ? fs : null;
    }

    private static DocumentoComprobatorio MontarDocumento(LiquidarDespesaCommand request) => request.TipoDocumento switch
    {
        TipoDocumentoComprobatorio.NfseChaveAcesso => DocumentoComprobatorio.NfsE(request.ChaveNfse!, request.DataEmissaoDocumento),
        TipoDocumentoComprobatorio.NotaFiscal => DocumentoComprobatorio.NotaFiscal(request.NumeroDocumento!, request.DataEmissaoDocumento ?? request.DataLiquidacao),
        _ => DocumentoComprobatorio.Generico(request.TipoDocumento, request.NumeroDocumento, request.DataEmissaoDocumento),
    };
}
