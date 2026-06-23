using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Profissionais;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Application.Profissionais;

/// <summary>
/// Cadastra um profissional de saude no acervo local. Garante a unicidade por <c>(TenantId, Cpf)</c>.
/// </summary>
/// <param name="Cpf">CPF do profissional (com ou sem mascara).</param>
/// <param name="Nome">Nome do profissional.</param>
/// <param name="Cns">CNS do profissional (opcional, 15 digitos).</param>
/// <param name="Registro">Registro em conselho de classe (opcional).</param>
public sealed record CadastrarProfissionalCommand(
    string Cpf,
    string Nome,
    string? Cns,
    RegistroConselhoDto? Registro) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de profissional.</summary>
public sealed class CadastrarProfissionalValidator : AbstractValidator<CadastrarProfissionalCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarProfissionalValidator()
    {
        RuleFor(comando => comando.Cpf)
            .Must(cpf => Cpf.TryCreate(cpf, out _))
            .WithMessage("CPF e obrigatorio e deve ser valido.");

        RuleFor(comando => comando.Nome)
            .NotEmpty()
            .MaximumLength(Profissional.ComprimentoNome)
            .WithMessage("Nome do profissional e obrigatorio (max. 120).");

        When(comando => comando.Registro is not null, () =>
        {
            RuleFor(comando => comando.Registro!.Uf)
                .Length(RegistroConselho.ComprimentoUf)
                .WithMessage("UF do registro deve ter 2 caracteres.");
            RuleFor(comando => comando.Registro!.Numero)
                .NotEmpty()
                .MaximumLength(RegistroConselho.ComprimentoNumero)
                .WithMessage("Numero do registro e obrigatorio.");
            RuleFor(comando => comando.Registro!.Tipo)
                .Must(tipo => Enum.IsDefined(typeof(TipoConselho), tipo))
                .WithMessage("Tipo de conselho invalido.");
        });
    }
}

/// <summary>Handler do cadastro de profissional.</summary>
public sealed class CadastrarProfissionalHandler(
    IProfissionalCadastroRepository profissionais,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarProfissionalCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarProfissionalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cpf = Cpf.Create(request.Cpf);

        if (await profissionais.ExistePorCpfAsync(cpf, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Profissional ja cadastrado para este CPF.");
        }

        RegistroConselho? registro = request.Registro is { } dto
            ? new RegistroConselho((TipoConselho)dto.Tipo, dto.Uf, dto.Numero)
            : null;

        var profissional = Profissional.Cadastrar(tenant.TenantId, cpf, request.Nome, request.Cns, registro);

        profissionais.Adicionar(profissional);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return profissional.Id.Value;
    }
}
