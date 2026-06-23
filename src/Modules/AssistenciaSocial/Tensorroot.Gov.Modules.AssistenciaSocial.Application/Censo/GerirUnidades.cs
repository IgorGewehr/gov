using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Prontuarios;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Censo;

/// <summary>3d.2: cadastra uma unidade socioassistencial (CRAS/CREAS/Centro POP) para a gestao do Censo SUAS.</summary>
/// <param name="Nome">Nome da unidade.</param>
/// <param name="Tipo">Tipo (CRAS/CREAS/Centro POP).</param>
/// <param name="TerritorioCobertura">Territorio coberto pela unidade.</param>
/// <param name="Endereco">Endereco da unidade.</param>
public sealed record CadastrarUnidadeSocioassistencialCommand(
    string Nome,
    TipoUnidadeAtendimento Tipo,
    string TerritorioCobertura,
    string Endereco) : ICommand<Guid>;

/// <summary>Validacao do cadastro de unidade.</summary>
public sealed class CadastrarUnidadeSocioassistencialValidator : AbstractValidator<CadastrarUnidadeSocioassistencialCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarUnidadeSocioassistencialValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Tipo).IsInEnum();
        RuleFor(c => c.TerritorioCobertura).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Endereco).NotEmpty().MaximumLength(300);
    }
}

/// <summary>Handler do cadastro de unidade socioassistencial.</summary>
public sealed class CadastrarUnidadeSocioassistencialHandler(
    IUnidadeSocioassistencialRepository unidades,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarUnidadeSocioassistencialCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarUnidadeSocioassistencialCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var unidade = UnidadeSocioassistencial.Cadastrar(tenant.TenantId, request.Nome, request.Tipo, request.TerritorioCobertura, request.Endereco);
        await unidades.AdicionarAsync(unidade, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return unidade.Id.Value;
    }
}

/// <summary>3d.2: oferta (ou atualiza a capacidade de) um servico tipificado e define a equipe de referencia da unidade.</summary>
/// <param name="UnidadeId">Unidade socioassistencial.</param>
/// <param name="Servico">Servico tipificado a ofertar (PAIF/PAEFI/SCFV).</param>
/// <param name="CapacidadeMensal">Capacidade mensal do servico.</param>
/// <param name="QuantidadeProfissionais">Tamanho da equipe de referencia (RH da unidade).</param>
public sealed record ConfigurarUnidadeCommand(
    Guid UnidadeId,
    TipoServico Servico,
    int CapacidadeMensal,
    int QuantidadeProfissionais) : ICommand;

/// <summary>Validacao da configuracao da unidade.</summary>
public sealed class ConfigurarUnidadeValidator : AbstractValidator<ConfigurarUnidadeCommand>
{
    /// <summary>Define as regras.</summary>
    public ConfigurarUnidadeValidator()
    {
        RuleFor(c => c.UnidadeId).NotEmpty();
        RuleFor(c => c.Servico).IsInEnum();
        RuleFor(c => c.CapacidadeMensal).GreaterThanOrEqualTo(0);
        RuleFor(c => c.QuantidadeProfissionais).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Handler da configuracao da unidade (servico + equipe).</summary>
public sealed class ConfigurarUnidadeHandler(
    IUnidadeSocioassistencialRepository unidades,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfigurarUnidadeCommand>
{
    /// <inheritdoc />
    public async Task Handle(ConfigurarUnidadeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var unidade = await unidades.ObterPorIdAsync(new UnidadeSocioassistencialId(request.UnidadeId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unidade socioassistencial inexistente no tenant.");

        unidade.OfertarServico(request.Servico, request.CapacidadeMensal);
        unidade.DefinirEquipe(request.QuantidadeProfissionais);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
