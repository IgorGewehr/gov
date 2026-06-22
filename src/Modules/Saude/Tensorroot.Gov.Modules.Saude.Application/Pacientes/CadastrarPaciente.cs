using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>
/// Cadastra um paciente no PEP a partir de um CNS confirmado no CADSUS. Garante a unicidade
/// por <c>(TenantId, Cns)</c> (I-9) e aplica a confirmacao CADSUS quando positiva (I-2).
/// </summary>
/// <param name="Cns">Cartao Nacional de Saude (15 digitos).</param>
/// <param name="Identificacao">Dados civis do paciente.</param>
/// <param name="Endereco">Endereco residencial.</param>
public sealed record CadastrarPacienteCommand(
    string Cns,
    IdentificacaoDto Identificacao,
    EnderecoDto Endereco) : ICommand<Guid>;

/// <summary>Regras de validacao do cadastro de paciente.</summary>
public sealed class CadastrarPacienteValidator : AbstractValidator<CadastrarPacienteCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarPacienteValidator()
    {
        RuleFor(comando => comando.Cns)
            .NotEmpty()
            .Length(Domain.Pacientes.Cns.Comprimento)
            .Must(Domain.Pacientes.Cns.EhValido)
            .WithMessage("CNS e obrigatorio e deve ser valido (15 digitos).");

        RuleFor(comando => comando.Identificacao).NotNull();
        RuleFor(comando => comando.Identificacao.Nome)
            .NotEmpty()
            .MaximumLength(Identificacao.ComprimentoNome)
            .WithMessage("Nome do paciente e obrigatorio (max. 120).")
            .When(comando => comando.Identificacao is not null);

        RuleFor(comando => comando.Identificacao.Cpf)
            .Must(cpf => Cpf.TryCreate(cpf, out _))
            .WithMessage("CPF invalido.")
            .When(comando => comando.Identificacao is not null && !string.IsNullOrWhiteSpace(comando.Identificacao.Cpf));

        RuleFor(comando => comando.Endereco.Uf)
            .Length(Endereco.ComprimentoUf)
            .WithMessage("UF deve ter 2 caracteres.")
            .When(comando => comando.Endereco is not null);
    }
}

/// <summary>Handler do cadastro de paciente.</summary>
public sealed class CadastrarPacienteHandler(
    IPacienteRepository pacientes,
    ICadsusGateway cadsus,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<CadastrarPacienteCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarPacienteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Identificacao);

        var cns = new Cns(request.Cns);

        // I-9: unicidade por (TenantId, Cns) — valida antes de cadastrar.
        if (await pacientes.ExistePorCnsAsync(cns, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Paciente ja cadastrado para este CNS.");
        }

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var identificacao = request.Identificacao.ParaDominio(hoje);
        var endereco = request.Endereco.ParaDominio();

        var paciente = Paciente.Cadastrar(tenant.TenantId, cns, identificacao, endereco);

        // I-2/B-5: confirmacao CADSUS sincrona; indisponibilidade nasce sem confirmacao.
        if (await cadsus.ValidarCnsAsync(cns, cancellationToken).ConfigureAwait(false))
        {
            paciente.ConfirmarCadastro();
        }

        pacientes.Adicionar(paciente);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return paciente.Id.Value;
    }
}
