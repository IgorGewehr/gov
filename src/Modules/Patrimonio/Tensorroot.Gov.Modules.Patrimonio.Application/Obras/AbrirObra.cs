using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Patrimonio.Application.Abstractions;
using Tensorroot.Gov.Modules.Patrimonio.Domain.Obras;
using Tensorroot.Gov.Modules.Patrimonio.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Patrimonio.Application.Obras;

/// <summary>Abre uma obra a partir de um contrato NLLC (Lei 14.133/2021), retornando seu identificador.</summary>
/// <param name="ContratoId">Contrato NLLC de origem (vínculo por ID).</param>
/// <param name="FornecedorId">Contratada/executora.</param>
/// <param name="Objeto">Descrição da obra/serviço de engenharia.</param>
/// <param name="Municipio">Município da obra.</param>
/// <param name="Uf">UF (2 letras).</param>
/// <param name="Logradouro">Logradouro/endereço (opcional).</param>
/// <param name="Latitude">Latitude (opcional).</param>
/// <param name="Longitude">Longitude (opcional).</param>
/// <param name="GeoCodigo">Código geográfico/SICOE (opcional).</param>
/// <param name="RegimeExecucao">Regime de execução (1..6 — art. 46).</param>
/// <param name="ValorContratado">Valor contratado (teto da medição — I-1).</param>
/// <param name="DataAssinaturaContrato">Data de assinatura (base do relógio art. 94 §3).</param>
public sealed record AbrirObraCommand(
    Guid ContratoId,
    Guid FornecedorId,
    string Objeto,
    string Municipio,
    string Uf,
    string? Logradouro,
    decimal? Latitude,
    decimal? Longitude,
    string? GeoCodigo,
    int RegimeExecucao,
    decimal ValorContratado,
    DateOnly DataAssinaturaContrato) : ICommand<Guid>;

/// <summary>Regras de validação da abertura de obra.</summary>
public sealed class AbrirObraValidator : AbstractValidator<AbrirObraCommand>
{
    /// <summary>Define as regras.</summary>
    public AbrirObraValidator()
    {
        RuleFor(comando => comando.ContratoId).NotEmpty().WithMessage("Contrato de origem é obrigatório.");
        RuleFor(comando => comando.FornecedorId).NotEmpty().WithMessage("Fornecedor é obrigatório.");
        RuleFor(comando => comando.Objeto).NotEmpty().MaximumLength(500);
        RuleFor(comando => comando.Municipio).NotEmpty().MaximumLength(120);
        RuleFor(comando => comando.Uf).NotEmpty().Length(2);
        RuleFor(comando => comando.RegimeExecucao)
            .Must(regime => Enum.IsDefined(typeof(RegimeExecucao), regime))
            .WithMessage("Regime de execução inválido (1..6 — art. 46).");
        RuleFor(comando => comando.ValorContratado).GreaterThan(0);
    }
}

/// <summary>Handler da abertura de obra.</summary>
public sealed class AbrirObraHandler(
    IObraRepository obras,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<AbrirObraCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AbrirObraCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await obras.ExisteParaContratoAsync(request.ContratoId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Já existe obra aberta para o contrato {request.ContratoId}.");
        }

        var localizacao = LocalizacaoObra.Criar(
            request.Municipio,
            request.Uf,
            request.Logradouro,
            request.Latitude,
            request.Longitude,
            request.GeoCodigo);

        var obra = Obra.AbrirObra(
            tenant.TenantId,
            request.ContratoId,
            request.FornecedorId,
            request.Objeto,
            localizacao,
            (RegimeExecucao)request.RegimeExecucao,
            ValorMonetario.De(request.ValorContratado),
            request.DataAssinaturaContrato);

        obras.Adicionar(obra);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return obra.Id.Value;
    }
}
