using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Administracao.Application.Abstractions;
using Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

namespace Tensorroot.Gov.Modules.Administracao.Application.RegistroPrecos;

/// <summary>Registra uma nova Ata de Registro de Precos (ARP), nasce Vigente (art. 82-86, Lei 14.133/2021).</summary>
/// <param name="Numero">Numero/identificacao da ata (unico por tenant).</param>
/// <param name="LicitacaoId">Licitacao SRP de origem (opcional).</param>
/// <param name="CnpjOrgaoGerenciador">CNPJ do orgao gerenciador da ata (art. 86, caput).</param>
/// <param name="NomeOrgaoGerenciador">Nome do orgao gerenciador da ata.</param>
/// <param name="VigenciaInicio">Inicio da vigencia.</param>
/// <param name="VigenciaFim">Termo final da vigencia.</param>
public sealed record RegistrarAtaCommand(
    string Numero,
    Guid? LicitacaoId,
    string CnpjOrgaoGerenciador,
    string NomeOrgaoGerenciador,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de ata.</summary>
public sealed class RegistrarAtaValidator : AbstractValidator<RegistrarAtaCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAtaValidator()
    {
        RuleFor(c => c.Numero).NotEmpty().MaximumLength(60);
        RuleFor(c => c.CnpjOrgaoGerenciador).NotEmpty().MaximumLength(20);
        RuleFor(c => c.NomeOrgaoGerenciador).NotEmpty().MaximumLength(200);
        RuleFor(c => c.VigenciaFim).GreaterThan(c => c.VigenciaInicio)
            .WithMessage("Vigencia final deve ser posterior ao inicio.");
    }
}

/// <summary>Handler do registro de ata.</summary>
public sealed class RegistrarAtaHandler(IAtaRepository atas, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<RegistrarAtaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarAtaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await atas.ExistePorNumeroAsync(request.Numero.Trim(), cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe ata com este numero no tenant.");
        }

        var ata = Ata.Registrar(
            tenant.TenantId,
            request.Numero,
            request.LicitacaoId,
            request.CnpjOrgaoGerenciador,
            request.NomeOrgaoGerenciador,
            request.VigenciaInicio,
            request.VigenciaFim);
        atas.Adicionar(ata);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ata.Id.Value;
    }
}
