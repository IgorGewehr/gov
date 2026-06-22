using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TabelasLegais;

/// <summary>Faixa de uma tabela progressiva (entrada).</summary>
/// <param name="LimiteInferior">Limite inferior (exclusivo).</param>
/// <param name="LimiteSuperior">Limite superior (inclusivo).</param>
/// <param name="Aliquota">Aliquota em fracao decimal (ex.: 0,14 para 14%).</param>
public sealed record FaixaProgressivaDto(decimal LimiteInferior, decimal LimiteSuperior, decimal Aliquota);

/// <summary>
/// Cria a tabela RPPS municipal do tenant a partir da lei previdenciaria do ente (NUNCA default
/// federal — fail-closed sem ela). Sem esta tabela, o motor recusa o calculo de servidor efetivo.
/// </summary>
/// <param name="AnoVigencia">Ano de inicio de vigencia.</param>
/// <param name="MesVigencia">Mes de inicio de vigencia (1 a 12).</param>
/// <param name="Faixas">Faixas progressivas (a primeira inicia em zero; contiguas).</param>
/// <param name="Teto">Teto da base (opcional).</param>
/// <param name="BaseLegal">Lei previdenciaria municipal.</param>
public sealed record CriarTabelaRppsCommand(
    int AnoVigencia,
    int MesVigencia,
    IReadOnlyList<FaixaProgressivaDto> Faixas,
    decimal? Teto,
    string BaseLegal) : ICommand<Guid>;

/// <summary>Regras de validacao da criacao da tabela RPPS.</summary>
public sealed class CriarTabelaRppsValidator : AbstractValidator<CriarTabelaRppsCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarTabelaRppsValidator()
    {
        RuleFor(c => c.AnoVigencia).InclusiveBetween(2000, 2100);
        RuleFor(c => c.MesVigencia).InclusiveBetween(1, 12);
        RuleFor(c => c.Faixas).NotEmpty().WithMessage("Informe ao menos uma faixa.");
        RuleFor(c => c.BaseLegal).NotEmpty().MaximumLength(200)
            .WithMessage("Base legal (lei municipal) e obrigatoria.");
    }
}

/// <summary>Handler da criacao da tabela RPPS municipal.</summary>
public sealed class CriarTabelaRppsHandler(
    ITabelasLegaisRepository tabelas,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CriarTabelaRppsCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarTabelaRppsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var faixas = request.Faixas
            .Select(f => FaixaProgressiva.De(f.LimiteInferior, f.LimiteSuperior, f.Aliquota))
            .ToList();

        var tabela = TabelaRpps.Criar(
            tenantContext.TenantId,
            Competencia.De(request.AnoVigencia, request.MesVigencia),
            faixas,
            request.Teto,
            request.BaseLegal);

        tabelas.Adicionar(tabela);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tabela.Id.Value;
    }
}
