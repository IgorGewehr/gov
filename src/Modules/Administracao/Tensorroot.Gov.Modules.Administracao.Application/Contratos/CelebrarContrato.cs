using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Contracts;
using Tensorroot.Gov.Modules.Administracao.Domain.Contratos;
using Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Application.Contratos;

/// <summary>Celebra (assina) um novo contrato administrativo (Lei 14.133/2021).</summary>
/// <param name="LicitacaoId">Licitacao de origem (obrigatoria se origem = Licitacao; nula nas diretas).</param>
/// <param name="FornecedorId">Fornecedor contratado.</param>
/// <param name="Origem">Fundamento da contratacao (1 = Licitacao, 2 = Dispensa, 3 = Inexigibilidade).</param>
/// <param name="Objeto">Descricao do objeto contratado.</param>
/// <param name="Valor">Valor global original do contrato.</param>
/// <param name="DataAssinatura">Data de assinatura (marco inicial do prazo PNCP do art. 94); nula = data atual.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Fim da vigencia.</param>
/// <param name="EmpenhoId">Identificador do empenho (quando informado na celebracao).</param>
/// <param name="NumeroEmpenho">Numero do empenho (quando informado).</param>
/// <param name="JustificativaContratacaoDireta">Justificativa do enquadramento (obrigatoria nas contratacoes diretas).</param>
public sealed record CelebrarContratoCommand(
    Guid? LicitacaoId,
    Guid FornecedorId,
    OrigemContratacao Origem,
    string Objeto,
    decimal Valor,
    DateOnly? DataAssinatura,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    Guid? EmpenhoId,
    string? NumeroEmpenho,
    string? JustificativaContratacaoDireta) : ICommand<Guid>;

/// <summary>Regras de validacao da celebracao de contrato.</summary>
public sealed class CelebrarContratoValidator : AbstractValidator<CelebrarContratoCommand>
{
    /// <summary>Define as regras.</summary>
    public CelebrarContratoValidator()
    {
        RuleFor(comando => comando.FornecedorId).NotEmpty().WithMessage("Fornecedor e obrigatorio.");
        RuleFor(comando => comando.Origem).IsInEnum().WithMessage("Origem da contratacao invalida.");
        RuleFor(comando => comando.Objeto).NotEmpty().MaximumLength(500).WithMessage("Objeto e obrigatorio (max. 500).");
        RuleFor(comando => comando.Valor).GreaterThan(0).WithMessage("Valor contratado deve ser positivo.");
        RuleFor(comando => comando.VigenciaFim)
            .GreaterThanOrEqualTo(comando => comando.VigenciaInicio)
            .WithMessage("Fim da vigencia nao pode ser anterior ao inicio.");
        RuleFor(comando => comando)
            .Must(CoerenteComOrigem)
            .WithMessage("Licitacao obrigatoria quando origem = Licitacao; vedada nas contratacoes diretas.");
        RuleFor(comando => comando.JustificativaContratacaoDireta)
            .NotEmpty()
            .When(comando => comando.Origem != OrigemContratacao.Licitacao)
            .WithMessage("Justificativa e obrigatoria na contratacao direta.");
    }

    private static bool CoerenteComOrigem(CelebrarContratoCommand comando)
    {
        var temLicitacao = comando.LicitacaoId is { } id && id != Guid.Empty;
        return comando.Origem == OrigemContratacao.Licitacao ? temLicitacao : !temLicitacao;
    }
}

/// <summary>Handler da celebracao de contrato (publica <see cref="ContratoAssinadoIntegrationEvent"/> para Financas).</summary>
public sealed class CelebrarContratoHandler(
    IContratoRepository contratos,
    IFornecedorRepository fornecedores,
    IUnitOfWork unitOfWork,
    IIntegrationEventWriter integrationEvents,
    ITenantContext tenant,
    IPncpParametros pncpParametros,
    ICalendarioDiasUteis calendario,
    IDataHojeTenant dataHoje,
    TimeProvider timeProvider)
    : ICommandHandler<CelebrarContratoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CelebrarContratoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        EmpenhoRef? empenhoRef = null;
        if (request.EmpenhoId is { } empenhoId && empenhoId != Guid.Empty && !string.IsNullOrWhiteSpace(request.NumeroEmpenho))
        {
            empenhoRef = EmpenhoRef.De(empenhoId, request.NumeroEmpenho);
        }

        // BUG-A1: aferir a aptidao do fornecedor (sancao impeditiva vigente) no limite de agregado e passa-la
        // ao agregado Contrato, que recusa fail-closed a celebracao com impedido — em qualquer origem, inclusive
        // contratacoes diretas (art. 14 e art. 156, III/IV da Lei 14.133/2021).
        // "Hoje" no FUSO do tenant (UTC-3): serve de data de assinatura default (→ prazo PNCP art. 94) e
        // de marco do impedimento. Perto da meia-noite o UTC ja virou o dia seguinte e erraria o prazo.
        var hoje = dataHoje.Hoje();
        var fornecedor = await fornecedores.ObterPorIdAsync(new FornecedorId(request.FornecedorId), cancellationToken).ConfigureAwait(false);
        var fornecedorImpedido = fornecedor is not null && fornecedor.EstaImpedido(hoje);

        // Data de assinatura: informada, ou a data corrente (relogio externo — nunca dentro do dominio).
        var dataAssinatura = request.DataAssinatura ?? hoje;

        // Parametro de prazo de divulgacao no PNCP do tenant (art. 94) — sem numero magico (§16). Mapeia a
        // triade da Application para o tipo domestico do Domain (preserva a regra de dependencia §2).
        var divulgacao = pncpParametros.Divulgacao();
        var prazoDivulgacao = new PrazoPncpParametro(divulgacao.Quantidade, divulgacao.Unidade, divulgacao.NormaFonte);

        var contrato = Contrato.Celebrar(
            tenant.TenantId,
            request.LicitacaoId,
            request.FornecedorId,
            request.Origem,
            request.Objeto,
            ValorMonetario.De(request.Valor),
            dataAssinatura,
            request.VigenciaInicio,
            request.VigenciaFim,
            fornecedorImpedido,
            prazoDivulgacao,
            calendario,
            empenhoRef);

        contratos.Adicionar(contrato);

        // Integration event via OUTBOX (CLAUDE.md §2/§10), NUNCA via IPublisher in-scope: (a) garante a
        // consistencia transacional (a mensagem grava na MESMA UoW que o contrato) e (b) evita resolver o
        // DbContext de outro modulo (ex.: TransparenciaDbContext, que consome este evento) no MESMO escopo
        // da requisicao — o que dispararia a guarda H5 de multiplos ModuleDbContext por escopo. A entrega
        // efetiva aos consumidores cross-module ocorre na drenagem do Outbox, em escopo dedicado por modulo.
        var evento = new ContratoAssinadoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            contrato.Id.Value,
            request.FornecedorId,
            request.Valor,
            contrato.LicitacaoId);

        integrationEvents.Enfileirar(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return contrato.Id.Value;
    }
}
