using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;
using Tensorroot.Gov.Modules.Transparencia.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Transparencia.Application.DeclaracoesFiscais;

/// <summary>Linha contabil (saldo recebido) que compoe a Matriz de Saldos Contabeis.</summary>
/// <param name="ContaPcasp">Conta do PCASP (nao vazia).</param>
/// <param name="NaturezaSaldo">Natureza do saldo (1 = Devedor, 2 = Credor).</param>
/// <param name="Valor">Valor do saldo.</param>
/// <param name="InformacaoComplementar">Informacao complementar da MSC (opcional).</param>
public sealed record LinhaContabilDto(
    string ContaPcasp,
    int NaturezaSaldo,
    decimal Valor,
    string? InformacaoComplementar);

/// <summary>
/// Monta a MSC/declaracao a partir dos saldos recebidos de Financas (consumo da MSC). A declaracao
/// nasce em Consolidada (secao 5.1 das regras).
/// </summary>
/// <param name="TipoDeclaracao">Especie do demonstrativo (1 = Msc, 2 = Rreo, 3 = Rgf, 4 = Dca).</param>
/// <param name="Exercicio">Ano de exercicio (&gt;= 1900).</param>
/// <param name="Mes">Mes da competencia (MSC).</param>
/// <param name="NumeroBimestre">Numero do bimestre (RREO).</param>
/// <param name="NumeroQuadrimestre">Numero do quadrimestre (RGF).</param>
/// <param name="Linhas">Saldos contabeis recebidos.</param>
public sealed record ConsolidarDeclaracaoFiscalCommand(
    TipoDeclaracaoFiscal TipoDeclaracao,
    int Exercicio,
    int? Mes,
    int? NumeroBimestre,
    int? NumeroQuadrimestre,
    IReadOnlyList<LinhaContabilDto> Linhas) : ICommand<Guid>;

/// <summary>Regras de validacao da consolidacao da declaracao fiscal (secao 8 das regras).</summary>
public sealed class ConsolidarDeclaracaoFiscalValidator : AbstractValidator<ConsolidarDeclaracaoFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public ConsolidarDeclaracaoFiscalValidator()
    {
        RuleFor(comando => comando.TipoDeclaracao).IsInEnum();
        RuleFor(comando => comando.Exercicio).GreaterThanOrEqualTo(1900);

        RuleFor(comando => comando.Mes)
            .InclusiveBetween(1, 12)
            .When(comando => comando.TipoDeclaracao == TipoDeclaracaoFiscal.Msc);

        RuleFor(comando => comando.NumeroBimestre)
            .InclusiveBetween(1, 6)
            .When(comando => comando.TipoDeclaracao == TipoDeclaracaoFiscal.Rreo);

        RuleFor(comando => comando.NumeroQuadrimestre)
            .InclusiveBetween(1, 3)
            .When(comando => comando.TipoDeclaracao == TipoDeclaracaoFiscal.Rgf);

        RuleFor(comando => comando.Linhas).NotEmpty();
        RuleForEach(comando => comando.Linhas).ChildRules(linha =>
        {
            linha.RuleFor(item => item.ContaPcasp).NotEmpty().MaximumLength(30);
            // NaturezaSaldo é int no contrato (1=Devedor, 2=Credor). IsInEnum() aplicado a int reprova
            // tudo; validamos contra o enum NaturezaSaldo (membros definidos). [bug da ponte MSC corrigido]
            linha.RuleFor(item => item.NaturezaSaldo)
                .Must(valor => System.Enum.IsDefined(typeof(NaturezaSaldo), valor))
                .WithMessage("Natureza de saldo inválida (1 = Devedor, 2 = Credor).");
            linha.RuleFor(item => item.Valor).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Handler da consolidacao da declaracao fiscal.</summary>
public sealed class ConsolidarDeclaracaoFiscalHandler(
    IDeclaracaoFiscalRepository declaracoes,
    ICalendarioFiscal calendarioFiscal,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<ConsolidarDeclaracaoFiscalCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ConsolidarDeclaracaoFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // I-13: idempotencia — nao cria declaracao duplicada vigente para o mesmo (Tipo, periodo, tenant).
        var jaExiste = await declaracoes.ExisteVigenteAsync(
            request.TipoDeclaracao,
            request.Exercicio,
            request.Mes,
            request.NumeroBimestre,
            request.NumeroQuadrimestre,
            cancellationToken).ConfigureAwait(false);

        if (jaExiste)
        {
            throw new InvalidOperationException(
                "Ja existe declaracao vigente para o tipo/periodo informados (idempotencia I-13).");
        }

        var competencia = request.Mes is { } mes ? Competencia.De(request.Exercicio, mes) : null;
        var bimestre = request.NumeroBimestre is { } nb ? Bimestre.De(request.Exercicio, nb) : null;
        var quadrimestre = request.NumeroQuadrimestre is { } nq ? Quadrimestre.De(request.Exercicio, nq) : null;

        var linhas = request.Linhas
            .Select(item => LinhaContabil.Criar(
                item.ContaPcasp,
                (NaturezaSaldo)item.NaturezaSaldo,
                ValorMonetario.De(item.Valor),
                item.InformacaoComplementar))
            .ToList();
        var matriz = MatrizSaldos.Montar(linhas);

        var dataLimite = await calendarioFiscal.DerivarDataLimiteAsync(
            request.TipoDeclaracao,
            request.Exercicio,
            request.Mes,
            request.NumeroBimestre,
            request.NumeroQuadrimestre,
            cancellationToken).ConfigureAwait(false);

        var declaracao = DeclaracaoFiscal.ConsolidarMatriz(
            tenant.TenantId,
            request.TipoDeclaracao,
            request.Exercicio,
            competencia,
            bimestre,
            quadrimestre,
            dataLimite,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            matriz);

        declaracoes.Adicionar(declaracao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return declaracao.Id.Value;
    }
}
