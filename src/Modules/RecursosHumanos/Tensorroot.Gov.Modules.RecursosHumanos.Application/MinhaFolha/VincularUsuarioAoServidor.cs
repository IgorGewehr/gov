using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.MinhaFolha;

/// <summary>
/// Cria o vinculo usuario&#8596;servidor (ancora do autosservico). E uma operacao de GESTAO do RH
/// (gated por <c>recursoshumanos.gerenciar</c>): o gestor declara que o usuario X (do Identidade)
/// E o servidor Y. A partir dai, os endpoints proprios resolvem o servidor do usuario por este
/// vinculo. Idempotencia/unicidade: um usuario mapeia no maximo um servidor e vice-versa (I).
/// </summary>
/// <param name="UsuarioId">Usuario (subject do JWT) a vincular.</param>
/// <param name="ServidorId">Servidor correspondente no tenant.</param>
public sealed record VincularUsuarioAoServidorCommand(Guid UsuarioId, Guid ServidorId) : ICommand<Guid>;

/// <summary>Regras de validacao do vinculo.</summary>
public sealed class VincularUsuarioAoServidorValidator : AbstractValidator<VincularUsuarioAoServidorCommand>
{
    /// <summary>Define as regras.</summary>
    public VincularUsuarioAoServidorValidator()
    {
        RuleFor(comando => comando.UsuarioId).NotEmpty().WithMessage("Usuario e obrigatorio.");
        RuleFor(comando => comando.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
    }
}

/// <summary>Handler do vinculo usuario&#8596;servidor.</summary>
public sealed class VincularUsuarioAoServidorHandler(
    IVinculoServidorUsuarioRepository vinculos,
    IServidorRepository servidores,
    IUnitOfWork unitOfWork)
    : ICommandHandler<VincularUsuarioAoServidorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(VincularUsuarioAoServidorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = new ServidorId(request.ServidorId);

        // O servidor tem de existir NO TENANT (Global Query Filter): vincular a um servidor de outro
        // tenant e impossivel — a consulta nao o enxerga, e o vinculo nasce com o tenant carimbado.
        var servidor = await servidores.ObterPorIdAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado no tenant.");

        if (await vinculos.UsuarioJaVinculadoAsync(request.UsuarioId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Usuario ja esta vinculado a um servidor neste tenant.");
        }

        if (await vinculos.ServidorJaVinculadoAsync(servidorId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Servidor ja esta vinculado a um usuario neste tenant.");
        }

        // TenantId carimbado pelo interceptor no insert; usamos o do proprio servidor (mesmo tenant).
        var vinculo = VinculoServidorUsuario.Criar(servidor.TenantId, request.UsuarioId, servidor.Id);
        vinculos.Adicionar(vinculo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return vinculo.Id.Value;
    }
}
