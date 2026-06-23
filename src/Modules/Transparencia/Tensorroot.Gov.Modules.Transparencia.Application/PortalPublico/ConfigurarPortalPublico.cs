using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

namespace Tensorroot.Gov.Modules.Transparencia.Application.PortalPublico;

/// <summary>
/// INTERNO: configura/atualiza o portal publico do tenant (slug + nome de exibicao). O slug e
/// normalizado para kebab-case e a unicidade GLOBAL e verificada antes de gravar (dois entes nao podem
/// compartilhar o mesmo slug na URL publica). Mutacao autenticada (<c>transparencia.gerenciar</c>).
/// </summary>
/// <param name="Slug">Slug publico desejado (sera normalizado).</param>
/// <param name="NomeEnte">Nome de exibicao do ente.</param>
public sealed record ConfigurarPortalPublicoCommand(string Slug, string NomeEnte) : ICommand<string>;

/// <summary>Validador da configuracao do portal.</summary>
public sealed class ConfigurarPortalPublicoValidator : AbstractValidator<ConfigurarPortalPublicoCommand>
{
    /// <summary>Regras.</summary>
    public ConfigurarPortalPublicoValidator()
    {
        RuleFor(comando => comando.Slug).NotEmpty().MaximumLength(120);
        RuleFor(comando => comando.NomeEnte).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler que cria/atualiza a configuracao do portal e retorna o slug efetivo.</summary>
public sealed class ConfigurarPortalPublicoHandler(
    IPortalPublicoConfigRepository configuracoes, ITenantContext tenant, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfigurarPortalPublicoCommand, string>
{
    /// <inheritdoc />
    public async Task<string> Handle(ConfigurarPortalPublicoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var slugNormalizado = PortalPublicoConfig.NormalizarSlug(request.Slug);
        if (slugNormalizado.Length == 0)
        {
            throw new ArgumentException("Slug publico invalido apos normalizacao.", nameof(request));
        }

        var existente = await configuracoes.ObterDoTenantAsync(cancellationToken).ConfigureAwait(false);

        // Unicidade global: o slug nao pode pertencer a OUTRO tenant.
        if (existente is null || !string.Equals(existente.Slug, slugNormalizado, StringComparison.Ordinal))
        {
            if (await configuracoes.SlugExisteAsync(slugNormalizado, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException($"O slug publico '{slugNormalizado}' ja esta em uso por outro ente.");
            }
        }

        if (existente is null)
        {
            var config = PortalPublicoConfig.Criar(tenant.TenantId, request.Slug, request.NomeEnte);
            configuracoes.Adicionar(config);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return config.Slug;
        }

        existente.AlterarSlug(request.Slug);
        existente.Ativar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existente.Slug;
    }
}
