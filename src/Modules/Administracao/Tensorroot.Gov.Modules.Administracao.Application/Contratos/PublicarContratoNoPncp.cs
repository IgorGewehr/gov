using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>
/// Divulga o contrato no PNCP — condicao de eficacia (Lei 14.133/2021, art. 94). NAO recebe mais o numero
/// de controle de fora: a transmissao e feita pela ACL (<see cref="IPncpGateway"/>), que devolve o numero
/// de controle PNCP oficial. (CORRECAO LEGAL: art. 174 institui o PNCP; a eficacia e do art. 94.)
/// <para>
/// L2: os campos do schema "Inserir Contrato/Empenho" do Manual 2.3.5 que NAO vivem no agregado
/// (codigos de tabela de dominio do PNCP, vinculo a compra publicada, fornecedor) sao informados no
/// comando pela borda. // TODO(M10-validate): conferir os codigos de dominio (tipoContratoId,
/// categoriaProcessoId) contra <c>treina.pncp.gov.br</c> no credenciamento.
/// </para>
/// </summary>
/// <param name="ContratoId">Contrato a divulgar.</param>
/// <param name="CnpjOrgao">CNPJ do orgao/entidade comprador (pre-cadastro PNCP).</param>
/// <param name="CodigoUnidade">Codigo da unidade administrativa compradora.</param>
/// <param name="NumeroContratoInterno">Numero do contrato no ente (ex.: "0012/2026").</param>
/// <param name="AnoContrato">Ano do contrato (schema PNCP).</param>
/// <param name="Processo">Numero do processo administrativo (schema PNCP).</param>
/// <param name="NiFornecedor">CPF/CNPJ/identificador estrangeiro do fornecedor contratado.</param>
/// <param name="TipoPessoaFornecedor">PJ/PF/PE do fornecedor (schema PNCP).</param>
/// <param name="NomeRazaoSocialFornecedor">Nome/razao social do fornecedor (schema PNCP).</param>
/// <param name="TipoContratoId">Codigo do tipo de contrato (tabela de dominio PNCP).</param>
/// <param name="CategoriaProcessoId">Codigo da categoria do processo (tabela de dominio PNCP).</param>
/// <param name="NumeroParcelas">Numero de parcelas (schema PNCP; default 1).</param>
/// <param name="CnpjCompra">CNPJ originario da compra (default = CnpjOrgao).</param>
/// <param name="AnoCompra">Ano da compra a que o contrato se vincula.</param>
/// <param name="SequencialCompra">Sequencial da compra gerado pelo PNCP.</param>
/// <param name="NumeroControlePncpCompra">Numero de controle da compra ja publicada (edital).</param>
/// <param name="FrutoAdesao">Contrato fruto de adesao a ata de SRP (carona).</param>
public sealed record PublicarContratoNoPncpCommand(
    Guid ContratoId,
    string CnpjOrgao,
    string CodigoUnidade,
    string NumeroContratoInterno,
    int AnoContrato,
    string Processo,
    string NiFornecedor,
    TipoPessoaFornecedorPncp TipoPessoaFornecedor,
    string NomeRazaoSocialFornecedor,
    int TipoContratoId,
    int CategoriaProcessoId,
    int NumeroParcelas = 1,
    string? CnpjCompra = null,
    int AnoCompra = 0,
    int SequencialCompra = 0,
    string? NumeroControlePncpCompra = null,
    bool FrutoAdesao = false) : ICommand;

/// <summary>Regras de validacao da publicacao no PNCP (campos obrigatorios do schema 2.3.5).</summary>
public sealed class PublicarContratoNoPncpValidator : AbstractValidator<PublicarContratoNoPncpCommand>
{
    /// <summary>Define as regras.</summary>
    public PublicarContratoNoPncpValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty();
        RuleFor(comando => comando.CnpjOrgao).NotEmpty().MaximumLength(14);
        RuleFor(comando => comando.CodigoUnidade).NotEmpty().MaximumLength(20);
        RuleFor(comando => comando.NumeroContratoInterno).NotEmpty().MaximumLength(50);
        RuleFor(comando => comando.Processo).NotEmpty().MaximumLength(50);
        RuleFor(comando => comando.NiFornecedor).NotEmpty().MaximumLength(30);
        RuleFor(comando => comando.TipoPessoaFornecedor).IsInEnum();
        RuleFor(comando => comando.NomeRazaoSocialFornecedor).NotEmpty().MaximumLength(100);
        RuleFor(comando => comando.TipoContratoId).GreaterThan(0).WithMessage("tipoContratoId (tabela de dominio PNCP) e obrigatorio.");
        RuleFor(comando => comando.CategoriaProcessoId).GreaterThan(0).WithMessage("categoriaProcessoId (tabela de dominio PNCP) e obrigatorio.");
        RuleFor(comando => comando.NumeroParcelas).GreaterThan(0);
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
    IDataHojeTenant dataHoje,
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
        // operacao ao contrato — replays do Outbox nao geram registro duplicado no PNCP. Os campos
        // obrigatorios do schema "Inserir Contrato/Empenho" 2.3.5 sao montados do agregado + comando.
        var requisicao = new PublicacaoContratoPncpRequest(
            ContratoId: contrato.Id.Value,
            CnpjOrgao: request.CnpjOrgao,
            CnpjCompra: request.CnpjCompra ?? request.CnpjOrgao,
            AnoCompra: request.AnoCompra == 0 ? contrato.DataAssinatura.Year : request.AnoCompra,
            SequencialCompra: request.SequencialCompra,
            TipoContratoId: request.TipoContratoId,
            NumeroContratoEmpenho: request.NumeroContratoInterno,
            AnoContrato: request.AnoContrato,
            Processo: request.Processo,
            CategoriaProcessoId: request.CategoriaProcessoId,
            Receita: false, // contrato administrativo de despesa
            CodigoUnidade: request.CodigoUnidade,
            NiFornecedor: request.NiFornecedor,
            TipoPessoaFornecedor: request.TipoPessoaFornecedor,
            NomeRazaoSocialFornecedor: request.NomeRazaoSocialFornecedor,
            ObjetoContrato: contrato.Objeto,
            ValorInicial: contrato.ValorContratado.Valor,
            NumeroParcelas: request.NumeroParcelas,
            ValorGlobal: contrato.ValorAtual.Valor,
            DataAssinatura: contrato.DataAssinatura,
            DataVigenciaInicio: contrato.VigenciaInicio,
            DataVigenciaFim: contrato.VigenciaFim,
            NumeroControlePncpCompra: request.NumeroControlePncpCompra,
            FrutoAdesao: request.FrutoAdesao,
            ChaveIdempotencia: $"contrato:{contrato.Id.Value:N}");

        var resultado = await pncpGateway.PublicarContratoAsync(requisicao, cancellationToken).ConfigureAwait(false);
        if (!resultado.Sucesso || string.IsNullOrWhiteSpace(resultado.NumeroControlePncp))
        {
            // Falha de transmissao: lanca para que o pipeline/Outbox reprocesse (resiliencia at-least-once).
            throw new InvalidOperationException(
                $"Falha ao divulgar contrato no PNCP ({resultado.CodigoErro}): {resultado.MensagemErro}");
        }

        // Data de publicacao no PNCP (cumprimento do prazo art. 94) → dia civil no FUSO do tenant (UTC-3),
        // nao o UTC cru. O timestamp do evento abaixo permanece UTC (instante absoluto da trilha/Outbox).
        var dataPublicacao = dataHoje.Hoje();
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
