using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>
/// P0-1: vincula uma pensao alimenticia judicial ATIVA a um servidor, para que o motor de folha a
/// deduza da base do IRRF (Lei 7.713/88 art. 4 II) e gere o desconto/repasse ao beneficiario. Modalidade
/// PERCENTUAL sobre base (proventos brutos ou liquido apos descontos legais) ou VALOR FIXO mensal — espelha
/// a decisao judicial. Sem este vinculo a pensao caia como rubrica de "outros descontos" sem reduzir o IRRF.
/// </summary>
/// <param name="ServidorId">Servidor alvo do vinculo.</param>
/// <param name="Beneficiario">Nome do alimentando (repasse).</param>
/// <param name="Modalidade">1 = percentual sobre base; 2 = valor fixo.</param>
/// <param name="Percentual">Fracao (0..1) quando percentual; ignorado no valor fixo.</param>
/// <param name="BaseIncidencia">Base do percentual (1 = proventos brutos; 2 = liquido apos descontos legais).</param>
/// <param name="ValorFixo">Valor fixo mensal quando aplicavel; ignorado no percentual.</param>
/// <param name="ProcessoJudicial">Identificacao do processo judicial (nao vazio).</param>
public sealed record AdicionarPensaoAlimenticiaCommand(
    Guid ServidorId,
    string Beneficiario,
    ModalidadePensao Modalidade,
    decimal Percentual,
    BasePensao BaseIncidencia,
    decimal ValorFixo,
    string ProcessoJudicial) : ICommand;

/// <summary>Validacao do vinculo de pensao alimenticia.</summary>
public sealed class AdicionarPensaoAlimenticiaValidator : AbstractValidator<AdicionarPensaoAlimenticiaCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarPensaoAlimenticiaValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.Beneficiario).NotEmpty().WithMessage("Beneficiario e obrigatorio.");
        RuleFor(c => c.ProcessoJudicial).NotEmpty().WithMessage("Processo judicial e obrigatorio.");
        RuleFor(c => c.Modalidade).IsInEnum().WithMessage("Modalidade de pensao invalida.");
        RuleFor(c => c.Percentual)
            .InclusiveBetween(0.0001m, 1m)
            .When(c => c.Modalidade == ModalidadePensao.PercentualSobreBase)
            .WithMessage("Percentual deve estar em (0,1].");
        RuleFor(c => c.BaseIncidencia)
            .IsInEnum()
            .When(c => c.Modalidade == ModalidadePensao.PercentualSobreBase)
            .WithMessage("Base de incidencia invalida.");
        RuleFor(c => c.ValorFixo)
            .GreaterThan(0m)
            .When(c => c.Modalidade == ModalidadePensao.ValorFixo)
            .WithMessage("Valor fixo deve ser positivo.");
    }
}

/// <summary>Handler do vinculo de pensao alimenticia (P0-1).</summary>
public sealed class AdicionarPensaoAlimenticiaHandler(
    IServidorRepository servidores,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarPensaoAlimenticiaCommand>
{
    /// <inheritdoc />
    public async Task Handle(AdicionarPensaoAlimenticiaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var pensao = request.Modalidade == ModalidadePensao.ValorFixo
            ? PensaoAlimenticia.PorValorFixo(request.Beneficiario, request.ValorFixo, request.ProcessoJudicial)
            : PensaoAlimenticia.PorPercentual(request.Beneficiario, request.Percentual, request.BaseIncidencia, request.ProcessoJudicial);

        servidor.AdicionarPensaoAlimenticia(pensao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
