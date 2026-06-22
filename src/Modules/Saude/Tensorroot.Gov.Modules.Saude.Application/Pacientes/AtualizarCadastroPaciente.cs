using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Atualiza os dados cadastrais (identificacao/endereco) de um paciente ativo (I-6).</summary>
/// <param name="PacienteId">Paciente a atualizar.</param>
/// <param name="Identificacao">Novos dados civis.</param>
/// <param name="Endereco">Novo endereco residencial.</param>
public sealed record AtualizarCadastroPacienteCommand(
    Guid PacienteId,
    IdentificacaoDto Identificacao,
    EnderecoDto Endereco) : ICommand;

/// <summary>Regras de validacao da atualizacao cadastral.</summary>
public sealed class AtualizarCadastroPacienteValidator : AbstractValidator<AtualizarCadastroPacienteCommand>
{
    /// <summary>Define as regras.</summary>
    public AtualizarCadastroPacienteValidator()
    {
        RuleFor(comando => comando.PacienteId).NotEmpty().WithMessage("Paciente e obrigatorio.");
        RuleFor(comando => comando.Identificacao).NotNull();
        RuleFor(comando => comando.Identificacao.Nome)
            .NotEmpty()
            .MaximumLength(Identificacao.ComprimentoNome)
            .WithMessage("Nome do paciente e obrigatorio (max. 120).")
            .When(comando => comando.Identificacao is not null);
    }
}

/// <summary>Handler da atualizacao cadastral do paciente.</summary>
public sealed class AtualizarCadastroPacienteHandler(
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AtualizarCadastroPacienteCommand>
{
    /// <inheritdoc />
    public async Task Handle(AtualizarCadastroPacienteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Identificacao);

        var paciente = await pacientes.ObterPorIdAsync(new PacienteId(request.PacienteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Paciente nao encontrado.");

        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var identificacao = request.Identificacao.ParaDominio(hoje);
        var endereco = request.Endereco.ParaDominio();

        paciente.AtualizarCadastro(identificacao, endereco);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
