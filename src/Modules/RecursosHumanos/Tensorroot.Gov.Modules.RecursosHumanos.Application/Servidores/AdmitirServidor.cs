using FluentValidation;
using MediatR;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Contracts;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Admite (provimento) um servidor a partir de cargo provido (situacao inicial <c>Nomeado</c>).</summary>
/// <param name="Cpf">CPF do servidor.</param>
/// <param name="Matricula">Matricula unica do vinculo no tenant.</param>
/// <param name="DadosPessoais">Dados cadastrais sensiveis.</param>
/// <param name="CargoId">Cargo provido.</param>
/// <param name="Regime">Regime previdenciario (1 = RPPS, 2 = RGPS).</param>
/// <param name="DataNomeacao">Data do provimento/nomeacao.</param>
public sealed record AdmitirServidorCommand(
    string Cpf,
    string Matricula,
    DadosPessoaisDto DadosPessoais,
    Guid CargoId,
    RegimePrevidenciario Regime,
    DateOnly DataNomeacao) : ICommand<Guid>;

/// <summary>Regras de validacao da admissao de servidor.</summary>
public sealed class AdmitirServidorValidator : AbstractValidator<AdmitirServidorCommand>
{
    /// <summary>Define as regras.</summary>
    public AdmitirServidorValidator()
    {
        RuleFor(comando => comando.Cpf)
            .NotEmpty()
            .Must(cpf => Cpf.TryCreate(cpf, out _))
            .WithMessage("CPF invalido.");
        RuleFor(comando => comando.Matricula)
            .NotEmpty()
            .MaximumLength(Matricula.ComprimentoMaximo)
            .WithMessage("Matricula e obrigatoria (max. 20 caracteres).");
        RuleFor(comando => comando.DadosPessoais)
            .NotNull()
            .WithMessage("Dados pessoais sao obrigatorios.");
        RuleFor(comando => comando.DadosPessoais.Nome)
            .NotEmpty()
            .When(comando => comando.DadosPessoais is not null)
            .WithMessage("Dados pessoais sao obrigatorios.");
        RuleFor(comando => comando.CargoId).NotEmpty().WithMessage("Cargo e obrigatorio.");
        RuleFor(comando => comando.Regime).IsInEnum().WithMessage("Regime previdenciario invalido.");
        RuleFor(comando => comando.DataNomeacao).NotEmpty().WithMessage("Data de nomeacao e obrigatoria.");
    }
}

/// <summary>Handler da admissao de servidor.</summary>
public sealed class AdmitirServidorHandler(
    ICargoRepository cargos,
    IServidorRepository servidores,
    IUnitOfWork unitOfWork,
    IPublisher publisher,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<AdmitirServidorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdmitirServidorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cargo = await cargos.ObterPorIdAsync(new CargoId(request.CargoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo nao encontrado.");

        // I-6: regime previdenciario coerente com o tipo do cargo (efetivo -> RPPS; demais -> RGPS).
        if (cargo.Regime != request.Regime)
        {
            throw new InvalidOperationException("Regime previdenciario incoerente com o tipo do cargo.");
        }

        var matricula = Matricula.De(request.Matricula);

        // I-11: matricula unica por tenant.
        if (await servidores.MatriculaExisteAsync(matricula, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Matricula ja existe.");
        }

        var servidor = Servidor.Admitir(
            tenant.TenantId,
            Cpf.Create(request.Cpf),
            matricula,
            DadosPessoais.Criar(request.DadosPessoais.Nome, request.DadosPessoais.DataNascimento),
            cargo.Id,
            request.Regime,
            request.DataNomeacao);

        servidores.Adicionar(servidor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var evento = new ServidorAdmitidoIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            tenant.TenantId,
            servidor.Id.Value,
            matricula.Valor,
            cargo.Id.Value);

        await publisher.Publish(evento, cancellationToken).ConfigureAwait(false);

        return servidor.Id.Value;
    }
}
