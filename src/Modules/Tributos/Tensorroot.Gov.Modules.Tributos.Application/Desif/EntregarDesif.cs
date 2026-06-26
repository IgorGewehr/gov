using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Desif;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Tributos.Application.Desif;

/// <summary>Subtítulo COSIF tributável declarado na DES-IF (entrada — Registro 0430).</summary>
/// <param name="ContaCosif">Conta/subtítulo do Plano Contábil COSIF (ex.: "7.1.7.99.00-8").</param>
/// <param name="CodigoTributacaoDesif">Código de tributação da tabela DES-IF (Anexo 6).</param>
/// <param name="ItemListaServico">Item da lista de serviços LC 116 correlato (ex.: "15.01").</param>
/// <param name="Descricao">Descrição do subtítulo/serviço.</param>
/// <param name="BaseCalculo">Receita tributável do subtítulo (base de cálculo, R$).</param>
/// <param name="AliquotaPercentual">Alíquota aplicável (%) conforme o item (lei municipal).</param>
public sealed record SubtituloDesifInput(
    string ContaCosif,
    string CodigoTributacaoDesif,
    string ItemListaServico,
    string Descricao,
    decimal BaseCalculo,
    decimal AliquotaPercentual);

/// <summary>Resultado da entrega da DES-IF mensal (Módulo 2).</summary>
/// <param name="DeclaracaoId">Declaração DES-IF gerada.</param>
/// <param name="LancamentoId">Lançamento do ISSQN a recolher gerado, ou <c>null</c> se não houver valor.</param>
/// <param name="QuantidadeSubtitulos">Quantidade de subtítulos escriturados.</param>
/// <param name="ReceitaTributavelTotal">Receita tributável total (R$).</param>
/// <param name="IssqnDevidoBruto">ISSQN devido bruto antes das deduções (R$).</param>
/// <param name="IssqnARecolher">ISSQN mensal a recolher após deduções (R$).</param>
public sealed record ResultadoDesif(
    Guid DeclaracaoId,
    Guid? LancamentoId,
    int QuantidadeSubtitulos,
    decimal ReceitaTributavelTotal,
    decimal IssqnDevidoBruto,
    decimal IssqnARecolher);

/// <summary>
/// Entrega a DES-IF mensal (Módulo 2 — Apuração Mensal do ISSQN, modelo conceitual ABRASF) de uma
/// instituição financeira: abre a declaração, escritura os subtítulos COSIF tributáveis (Registro 0430),
/// aplica as deduções legais (Registro 0440 — deduções da receita, incentivos em lei, depósitos
/// judiciais), apura o ISSQN A RECOLHER e CONSTITUI o crédito tributário (CTN art. 150 — lançamento por
/// homologação). Paridade com o incumbente SAPI. NÃO emitimos a declaração: o banco DECLARA.
/// </summary>
/// <param name="ContribuinteId">Contribuinte declarante (instituição financeira).</param>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="FundamentoLegal">Fundamento legal da obrigação acessória (CTM) — parametrizável.</param>
/// <param name="DeducoesReceita">Deduções da receita declarada (R$).</param>
/// <param name="IncentivosFiscais">Incentivos fiscais autorizados em lei (R$).</param>
/// <param name="DepositosJudiciais">Depósitos judiciais (R$).</param>
/// <param name="VencimentoIssqn">Vencimento do ISSQN a recolher lançado.</param>
/// <param name="Subtitulos">Subtítulos COSIF tributáveis declarados.</param>
public sealed record EntregarDesifCommand(
    Guid ContribuinteId,
    int Ano,
    int Mes,
    string FundamentoLegal,
    decimal DeducoesReceita,
    decimal IncentivosFiscais,
    decimal DepositosJudiciais,
    DateOnly VencimentoIssqn,
    IReadOnlyList<SubtituloDesifInput> Subtitulos) : ICommand<ResultadoDesif>;

/// <summary>Regras de validação da entrega da DES-IF.</summary>
public sealed class EntregarDesifValidator : AbstractValidator<EntregarDesifCommand>
{
    /// <summary>Define as regras.</summary>
    public EntregarDesifValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.Ano).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
        RuleFor(c => c.FundamentoLegal).NotEmpty().MaximumLength(300);
        RuleFor(c => c.DeducoesReceita).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.IncentivosFiscais).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.DepositosJudiciais).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.Subtitulos).NotEmpty();
        RuleForEach(c => c.Subtitulos).ChildRules(subtitulo =>
        {
            subtitulo.RuleFor(s => s.ContaCosif).NotEmpty().MaximumLength(30);
            subtitulo.RuleFor(s => s.CodigoTributacaoDesif).NotEmpty().MaximumLength(20);
            subtitulo.RuleFor(s => s.ItemListaServico).NotEmpty().MaximumLength(10);
            subtitulo.RuleFor(s => s.Descricao).NotEmpty().MaximumLength(300);
            subtitulo.RuleFor(s => s.BaseCalculo).GreaterThanOrEqualTo(0m);
            subtitulo.RuleFor(s => s.AliquotaPercentual).InclusiveBetween(0m, 100m);
        });
    }
}

/// <summary>Handler da entrega da DES-IF mensal de ISSQN.</summary>
public sealed class EntregarDesifHandler(
    IContribuinteRepository contribuintes,
    IDeclaracaoDesifRepository declaracoes,
    ILancamentoRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<EntregarDesifCommand, ResultadoDesif>
{
    /// <inheritdoc />
    public async Task<ResultadoDesif> Handle(EntregarDesifCommand request, CancellationToken cancellationToken)
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
                $"Já existe DES-IF vigente para o contribuinte na competência {competencia}. Retifique a existente.");
        }

        var declaracao = DeclaracaoDesif.Abrir(tenant.TenantId, contribuinteId, competencia, request.FundamentoLegal);
        foreach (var subtitulo in request.Subtitulos)
        {
            declaracao.EscriturarSubtitulo(
                subtitulo.ContaCosif,
                subtitulo.CodigoTributacaoDesif,
                subtitulo.ItemListaServico,
                subtitulo.Descricao,
                ValorMonetario.De(subtitulo.BaseCalculo),
                subtitulo.AliquotaPercentual);
        }

        var hoje = dataHoje.Hoje();
        declaracao.Entregar(
            ValorMonetario.De(request.DeducoesReceita),
            ValorMonetario.De(request.IncentivosFiscais),
            ValorMonetario.De(request.DepositosJudiciais),
            hoje);
        declaracoes.Adicionar(declaracao);

        // Constitui o crédito tributário do ISSQN a recolher (devido pela instituição financeira). Fato
        // gerador do ISS: prestação na competência (LC 116) — último dia do mês. A decadência (CTN art.
        // 173, I) é aferida no agregado Lancamento.
        Guid? lancamentoId = null;
        if (declaracao.IssqnARecolher.Valor > 0m)
        {
            var dataFatoGerador = new DateOnly(competencia.Ano, competencia.Mes, DateTime.DaysInMonth(competencia.Ano, competencia.Mes));

            var lancamento = Lancamento.Lancar(
                tenant.TenantId,
                contribuinteId,
                TipoTributo.Iss,
                competencia,
                declaracao.IssqnARecolher,
                request.VencimentoIssqn,
                dataFatoGerador,
                hoje);
            lancamentos.Adicionar(lancamento);
            lancamentoId = lancamento.Id.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoDesif(
            declaracao.Id.Value,
            lancamentoId,
            declaracao.QuantidadeSubtitulos,
            declaracao.ReceitaTributavelTotal.Valor,
            declaracao.IssqnDevidoBruto.Valor,
            declaracao.IssqnARecolher.Valor);
    }
}
