using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Calculo;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Iss;

/// <summary>Resultado da apuração mensal do ISS de um contribuinte.</summary>
/// <param name="ApuracaoId">Apuração (livro) gerada.</param>
/// <param name="LancamentoId">Lançamento do ISS próprio gerado, ou <c>null</c> se não houver ISS próprio.</param>
/// <param name="QuantidadeNotas">Quantidade de NFS-e escrituradas.</param>
/// <param name="IssProprio">ISS próprio a recolher (R$).</param>
/// <param name="IssRetido">ISS retido na fonte (R$).</param>
/// <param name="IssSubstituicao">ISS por substituição tributária (R$).</param>
public sealed record ResultadoApuracaoIss(
    Guid ApuracaoId,
    Guid? LancamentoId,
    int QuantidadeNotas,
    decimal IssProprio,
    decimal IssRetido,
    decimal IssSubstituicao);

/// <summary>
/// Apura o ISS mensal de um contribuinte (prestador) a partir das NFS-e já INGERIDAS do ADN
/// (NotaFiscalServico): classifica próprio/retido/substituição pela tabela de alíquotas municipal
/// vigente, monta o livro eletrônico (<see cref="ApuracaoIss"/>) e LANÇA o ISS próprio (CTN art. 142).
/// NÃO emite NFS-e (integração passiva — ADR-0003). Ver M6-DESIGN §2.2.
/// </summary>
/// <param name="ContribuinteId">Contribuinte prestador a apurar.</param>
/// <param name="Ano">Ano da competência.</param>
/// <param name="Mes">Mês da competência.</param>
/// <param name="VencimentoIssProprio">Vencimento do ISS próprio lançado.</param>
public sealed record ApurarIssMensalCommand(
    Guid ContribuinteId,
    int Ano,
    int Mes,
    DateOnly VencimentoIssProprio) : ICommand<ResultadoApuracaoIss>;

/// <summary>Regras de validação da apuração mensal do ISS.</summary>
public sealed class ApurarIssMensalValidator : AbstractValidator<ApurarIssMensalCommand>
{
    /// <summary>Define as regras.</summary>
    public ApurarIssMensalValidator()
    {
        RuleFor(c => c.ContribuinteId).NotEmpty();
        RuleFor(c => c.Ano).GreaterThanOrEqualTo(1900);
        RuleFor(c => c.Mes).InclusiveBetween(1, 12);
    }
}

/// <summary>Handler da apuração mensal do ISS.</summary>
public sealed class ApurarIssMensalHandler(
    IContribuinteRepository contribuintes,
    INotaFiscalServicoConsulta notas,
    ITabelaAliquotaIssRepository tabelas,
    IApuracaoIssRepository apuracoes,
    ILancamentoRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ApurarIssMensalCommand, ResultadoApuracaoIss>
{
    /// <inheritdoc />
    public async Task<ResultadoApuracaoIss> Handle(ApurarIssMensalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contribuinteId = new ContribuinteId(request.ContribuinteId);
        var contribuinte = await contribuintes.ObterPorIdAsync(contribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte não encontrado.");

        var competencia = Competencia.De(request.Ano, request.Mes);

        var existente = await apuracoes.ObterPorContribuinteCompetenciaAsync(contribuinteId, competencia, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            throw new InvalidOperationException($"Já existe apuração de ISS para o contribuinte na competência {competencia}.");
        }

        var tabela = await tabelas.ObterVigentePorCompetenciaAsync(competencia, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Não há tabela de alíquotas do ISS vigente para a competência {competencia}.");

        var notasVigentes = await notas.ListarVigentesPorPrestadorCompetenciaAsync(contribuinte.Documento, competencia, cancellationToken).ConfigureAwait(false);

        var apuracao = ApuracaoIss.Abrir(tenant.TenantId, contribuinteId, competencia);
        foreach (var nota in notasVigentes)
        {
            var memoria = CalculadoraIss.Apurar(nota, tabela);
            apuracao.Escriturar(memoria);
        }

        apuracao.Encerrar();
        apuracoes.Adicionar(apuracao);

        // Lança o crédito tributário do ISS PRÓPRIO (devido pelo prestador). O retido/substituição é
        // responsabilidade de terceiro e não gera lançamento contra este prestador.
        Guid? lancamentoId = null;
        if (apuracao.IssProprio.Valor > 0m)
        {
            var lancamento = Lancamento.Lancar(
                tenant.TenantId,
                contribuinteId,
                TipoTributo.Iss,
                competencia,
                apuracao.IssProprio,
                request.VencimentoIssProprio);
            lancamentos.Adicionar(lancamento);
            lancamentoId = lancamento.Id.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoApuracaoIss(
            apuracao.Id.Value,
            lancamentoId,
            apuracao.QuantidadeNotas,
            apuracao.IssProprio.Valor,
            apuracao.IssRetido.Valor,
            apuracao.IssSubstituicao.Valor);
    }
}
