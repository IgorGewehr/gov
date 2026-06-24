using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Iss;

/// <summary>Linha de serviço declarada na GIA (entrada).</summary>
/// <param name="ItemListaServico">Item da lista de serviços LC 116 (ex.: "7.02").</param>
/// <param name="Descricao">Descrição do serviço prestado.</param>
/// <param name="BaseCalculo">Base de cálculo (valor do serviço, R$).</param>
/// <param name="AliquotaPercentual">Alíquota aplicável (%) conforme o item (lei municipal).</param>
/// <param name="RetidoNaFonte">Se o ISS desta linha foi retido na fonte pelo tomador.</param>
public sealed record ServicoGiaInput(
    string ItemListaServico,
    string Descricao,
    decimal BaseCalculo,
    decimal AliquotaPercentual,
    bool RetidoNaFonte);

/// <summary>Resultado da entrega da GIA mensal de ISS.</summary>
/// <param name="DeclaracaoId">Declaração (GIA) gerada.</param>
/// <param name="LancamentoId">Lançamento do ISS próprio gerado, ou <c>null</c> se não houver ISS devido.</param>
/// <param name="QuantidadeServicos">Quantidade de serviços declarados.</param>
/// <param name="TotalServicos">Total dos serviços prestados (R$).</param>
/// <param name="IssDevido">ISS próprio devido (R$).</param>
public sealed record ResultadoGiaIss(
    Guid DeclaracaoId,
    Guid? LancamentoId,
    int QuantidadeServicos,
    decimal TotalServicos,
    decimal IssDevido);

/// <summary>
/// Entrega a GIA mensal de ISS de um contribuinte (declaração do próprio prestador — CTN art. 150,
/// lançamento por homologação): abre a declaração, escritura os serviços prestados, apura o ISS próprio
/// DEVIDO e CONSTITUI o crédito tributário do ISS devido. Cobre serviços SEM NFS-e nacional, que a
/// apuração derivada do ADN não alcança (PARIDADE-PoC SW-A10). NÃO emitimos NFS-e (ADR-0003).
/// </summary>
/// <param name="ContribuinteId">Contribuinte declarante (prestador).</param>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="FundamentoLegal">Fundamento legal da obrigação acessória (CTM) — parametrizável.</param>
/// <param name="VencimentoIssDevido">Vencimento do ISS próprio devido lançado.</param>
/// <param name="Servicos">Serviços prestados declarados.</param>
public sealed record EntregarGiaIssCommand(
    Guid ContribuinteId,
    int Ano,
    int Mes,
    string FundamentoLegal,
    DateOnly VencimentoIssDevido,
    IReadOnlyList<ServicoGiaInput> Servicos) : ICommand<ResultadoGiaIss>;

/// <summary>Regras de validação da entrega da GIA.</summary>
public sealed class EntregarGiaIssValidator : AbstractValidator<EntregarGiaIssCommand>
{
    /// <summary>Define as regras.</summary>
    public EntregarGiaIssValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.Ano).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Servicos).NotEmpty();
        RuleForEach(c => c.Servicos).ChildRules(servico =>
        {
            servico.RuleFor(s => s.ItemListaServico).NotEmpty().MaximumLength(10);
            servico.RuleFor(s => s.Descricao).NotEmpty().MaximumLength(300);
            servico.RuleFor(s => s.BaseCalculo).GreaterThanOrEqualTo(0m);
            servico.RuleFor(s => s.AliquotaPercentual).InclusiveBetween(0m, 100m);
        });
    }
}

/// <summary>Handler da entrega da GIA mensal de ISS.</summary>
public sealed class EntregarGiaIssHandler(
    IContribuinteRepository contribuintes,
    IDeclaracaoGiaIssRepository declaracoes,
    ILancamentoRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<EntregarGiaIssCommand, ResultadoGiaIss>
{
    /// <inheritdoc />
    public async Task<ResultadoGiaIss> Handle(EntregarGiaIssCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        _ = await contribuintes.ObterPorIdAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte não encontrado.");

        var competencia = Competencia.De(request.Ano, request.Mes);

        var existente = await declaracoes
            .ObterVigentePorContribuinteCompetenciaAsync(contribuinteId, competencia, cancellationToken)
            .ConfigureAwait(false);
        if (existente is not null)
        {
            throw new InvalidOperationException(
                $"Já existe GIA de ISS vigente para o contribuinte na competência {competencia}. Retifique a existente.");
        }

        var declaracao = DeclaracaoGiaIss.Abrir(tenant.TenantId, contribuinteId, competencia, request.FundamentoLegal);
        foreach (var servico in request.Servicos)
        {
            declaracao.DeclararServico(
                servico.ItemListaServico,
                servico.Descricao,
                ValorMonetario.De(servico.BaseCalculo),
                servico.AliquotaPercentual,
                servico.RetidoNaFonte);
        }

        var hoje = dataHoje.Hoje();
        declaracao.Entregar(hoje);
        declaracoes.Adicionar(declaracao);

        // Constitui o crédito tributário do ISS PRÓPRIO DEVIDO (devido pelo prestador). O retido na
        // fonte é responsabilidade do tomador e não compõe o devido próprio (já excluído no agregado).
        Guid? lancamentoId = null;
        if (declaracao.IssDevido.Valor > 0m)
        {
            // Fato gerador do ISS: prestação na competência (LC 116). Adota-se o último dia do mês.
            // A decadência (CTN art. 173, I) é aferida no agregado Lancamento.
            var dataFatoGerador = new DateOnly(competencia.Ano, competencia.Mes, DateTime.DaysInMonth(competencia.Ano, competencia.Mes));

            var lancamento = Lancamento.Lancar(
                tenant.TenantId,
                contribuinteId,
                TipoTributo.Iss,
                competencia,
                declaracao.IssDevido,
                request.VencimentoIssDevido,
                dataFatoGerador,
                hoje);
            lancamentos.Adicionar(lancamento);
            lancamentoId = lancamento.Id.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoGiaIss(
            declaracao.Id.Value,
            lancamentoId,
            declaracao.QuantidadeServicos,
            declaracao.TotalServicos.Valor,
            declaracao.IssDevido.Valor);
    }
}
