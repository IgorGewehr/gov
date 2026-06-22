using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Abstractions;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>Edita os dados cadastrais de um usuario (nome e e-mail de login).</summary>
/// <param name="UsuarioId">Usuario a editar.</param>
/// <param name="Nome">Novo nome de exibicao.</param>
/// <param name="Email">Novo e-mail de login (unico por tenant).</param>
public sealed record EditarUsuarioCommand(Guid UsuarioId, string Nome, string Email) : ICommand;

/// <summary>Regras de validacao da edicao de usuario.</summary>
public sealed class EditarUsuarioValidator : AbstractValidator<EditarUsuarioCommand>
{
    /// <summary>Define as regras.</summary>
    public EditarUsuarioValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty();
        RuleFor(comando => comando.Nome).NotEmpty().MaximumLength(Usuario.ComprimentoMaximoNome);
        RuleFor(comando => comando.Email)
            .NotEmpty()
            .MaximumLength(Email.ComprimentoMaximo)
            .Must(email => Domain.ValueObjects.Email.TentarCriar(email, out _))
            .WithMessage("E-mail invalido.");
    }
}

/// <summary>Handler da edicao de usuario.</summary>
public sealed class EditarUsuarioHandler(
    IUsuarioRepository usuarios,
    IRegistroLoginCentral registroLoginCentral,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<EditarUsuarioCommand>
{
    /// <inheritdoc />
    public async Task Handle(EditarUsuarioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usuario = await usuarios.ObterPorIdAsync(new UsuarioId(request.UsuarioId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Usuario nao encontrado.");

        var email = Email.De(request.Email);
        var emailAntigo = usuario.Email;
        var emailMudou = email != emailAntigo;

        if (emailMudou && await usuarios.EmailEmUsoAsync(email, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ja existe um usuario com este e-mail no tenant.");
        }

        // Atualiza o indice central (unicidade global) antes de gravar no banco do tenant.
        if (emailMudou)
        {
            await registroLoginCentral
                .AtualizarAsync(emailAntigo.Valor, email.Valor, tenant.TenantId, cancellationToken)
                .ConfigureAwait(false);
        }

        usuario.Editar(request.Nome, email);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
