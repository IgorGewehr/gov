using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Farmacia;
using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;

namespace Tensorroot.Gov.Modules.Saude.Application.Imunizacao;

/// <summary>
/// Cadastra um imunobiologico no catalogo do PNI (esquema de doses + intervalo de aprazamento,
/// parametrizaveis). Opcionalmente vincula um item de estoque (vacina como medicamento) para baixa na
/// aplicacao.
/// </summary>
/// <param name="Nome">Nome do imunobiologico.</param>
/// <param name="Sigla">Sigla.</param>
/// <param name="TotalDoses">Total de doses do esquema (>= 1).</param>
/// <param name="IntervaloDiasProximaDose">Intervalo padrao (dias) ate a proxima dose.</param>
/// <param name="DoseUnica">Indica dose unica.</param>
/// <param name="MedicamentoEstoqueId">Item de estoque associado (opcional — para baixa na aplicacao).</param>
public sealed record CadastrarImunobiologicoCommand(
    string Nome,
    string Sigla,
    int TotalDoses,
    int IntervaloDiasProximaDose,
    bool DoseUnica,
    Guid? MedicamentoEstoqueId) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de imunobiologico.</summary>
public sealed class CadastrarImunobiologicoValidator : AbstractValidator<CadastrarImunobiologicoCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarImunobiologicoValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Sigla).NotEmpty().MaximumLength(20);
        RuleFor(c => c.TotalDoses).GreaterThanOrEqualTo(1);
        RuleFor(c => c.IntervaloDiasProximaDose).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler do cadastro de imunobiologico.</summary>
public sealed class CadastrarImunobiologicoHandler(
    IImunobiologicoRepository imunobiologicos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarImunobiologicoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarImunobiologicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medicamentoEstoqueId = request.MedicamentoEstoqueId is { } m ? new MedicamentoId(m) : (MedicamentoId?)null;

        var imunobiologico = Imunobiologico.Cadastrar(
            tenant.TenantId,
            request.Nome,
            request.Sigla,
            request.TotalDoses,
            request.IntervaloDiasProximaDose,
            request.DoseUnica,
            medicamentoEstoqueId);

        imunobiologicos.Adicionar(imunobiologico);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return imunobiologico.Id.Value;
    }
}
