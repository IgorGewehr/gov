using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;

/// <summary>
/// Cadastra um Registrador Eletronico de Ponto (REP) no parque do ente (multi-tenant). A credencial
/// real do equipamento NUNCA entra aqui — apenas a REFERENCIA (nome do segredo) no Key Vault.
/// </summary>
/// <param name="IdentificacaoEquipamento">Nº de serie/fabricante (nao vazio).</param>
/// <param name="Marca">Marca/fabricante (1=ArquivoAfd, 2=ControlId, ..., 99=Simulado).</param>
/// <param name="Modos">Modos de coleta suportados (1=Arquivo, 2=TcpSdk, 4=RestCloud; combinaveis).</param>
/// <param name="Tipo">Tipo regulatorio (1=REP-C, 2=REP-A, 3=REP-P).</param>
/// <param name="EnderecoOuReferencia">Host/porta ou referencia logica; opcional.</param>
/// <param name="ReferenciaCredencialCofre">Nome do segredo no Key Vault; opcional.</param>
public sealed record RegistrarRepCommand(
    string IdentificacaoEquipamento,
    int Marca,
    int Modos,
    int Tipo,
    string? EnderecoOuReferencia,
    string? ReferenciaCredencialCofre) : ICommand<Guid>;

/// <summary>Validacao do cadastro de REP.</summary>
public sealed class RegistrarRepValidator : AbstractValidator<RegistrarRepCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarRepValidator()
    {
        RuleFor(c => c.IdentificacaoEquipamento).NotEmpty().WithMessage("Identificacao do equipamento e obrigatoria.");
        // LICAO: validar enum por Enum.IsDefined sobre o tipo, nunca IsInEnum sobre o int do contrato.
        RuleFor(c => c.Marca).Must(m => Enum.IsDefined(typeof(MarcaRep), m))
            .WithMessage("Marca de REP invalida.");
        RuleFor(c => c.Tipo).Must(t => Enum.IsDefined(typeof(TipoRep), t))
            .WithMessage("Tipo de REP invalido (1=REP-C, 2=REP-A, 3=REP-P).");
        RuleFor(c => c.Modos).GreaterThan(0).WithMessage("Informe ao menos um modo de coleta.");
    }
}

/// <summary>Handler do cadastro de REP.</summary>
public sealed class RegistrarRepHandler(
    IRepRepository reps,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarRepCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarRepCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rep = RepConfigurado.Registrar(
            tenantContext.TenantId,
            request.IdentificacaoEquipamento,
            (MarcaRep)request.Marca,
            (ModoColeta)request.Modos,
            (TipoRep)request.Tipo,
            request.EnderecoOuReferencia,
            request.ReferenciaCredencialCofre);

        reps.Adicionar(rep);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return rep.Id.Value;
    }
}
