using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

/// <summary>
/// Inicia um periodo de exposicao a agentes nocivos de um servidor — base do S-2240 e fonte do PPP. Pelo
/// menos um agente deve ser informado (use o codigo de "ausencia de agente nocivo" para declarar a inexistencia).
/// </summary>
/// <param name="ServidorId">Servidor exposto.</param>
/// <param name="InicioExposicao">Inicio do periodo.</param>
/// <param name="SetorAtividade">Setor/atividade (descricao das atribuicoes — PPP).</param>
/// <param name="Agentes">Agentes nocivos do periodo (Tabela 23).</param>
public sealed record RegistrarExposicaoAgenteNocivoCommand(
    Guid ServidorId,
    DateOnly InicioExposicao,
    string SetorAtividade,
    IReadOnlyList<AgenteNocivoDto> Agentes) : ICommand<Guid>;

/// <summary>Regras de validacao do registro de exposicao a agentes nocivos.</summary>
public sealed class RegistrarExposicaoAgenteNocivoValidator : AbstractValidator<RegistrarExposicaoAgenteNocivoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarExposicaoAgenteNocivoValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.InicioExposicao).NotEmpty().WithMessage("Inicio da exposicao e obrigatorio.");
        RuleFor(c => c.SetorAtividade).NotEmpty().WithMessage("Setor/atividade e obrigatorio.");
        RuleFor(c => c.Agentes).NotEmpty().WithMessage("Informe ao menos um agente (use o codigo de ausencia de agente nocivo se for o caso).");
        RuleForEach(c => c.Agentes).ChildRules(agente =>
        {
            agente.RuleFor(a => a.Codigo).NotEmpty().WithMessage("Codigo do agente e obrigatorio.");
            agente.RuleFor(a => a.Descricao).NotEmpty().WithMessage("Descricao do agente e obrigatoria.");
        });
    }
}

/// <summary>Handler do registro de exposicao a agentes nocivos.</summary>
public sealed class RegistrarExposicaoAgenteNocivoHandler(
    IServidorRepository servidores,
    IExposicaoAgenteNocivoRepository exposicoes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<RegistrarExposicaoAgenteNocivoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarExposicaoAgenteNocivoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var exposicao = ExposicaoAgenteNocivo.Iniciar(
            tenant.TenantId,
            servidor.Id,
            request.InicioExposicao,
            request.SetorAtividade);

        foreach (var dto in request.Agentes)
        {
            exposicao.AdicionarAgente(AgenteNocivo.Criar(
                dto.Codigo,
                dto.Descricao,
                dto.Intensidade,
                dto.UnidadeMedida,
                dto.UtilizaEpc,
                dto.UtilizaEpi));
        }

        exposicoes.Adicionar(exposicao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return exposicao.Id.Value;
    }
}

/// <summary>Encerra um periodo de exposicao a agentes nocivos (gera o fim do periodo no PPP/S-2240).</summary>
/// <param name="ExposicaoId">Identificador da exposicao.</param>
/// <param name="FimExposicao">Data de fim do periodo.</param>
public sealed record EncerrarExposicaoAgenteNocivoCommand(Guid ExposicaoId, DateOnly FimExposicao) : ICommand;

/// <summary>Handler do encerramento de exposicao.</summary>
public sealed class EncerrarExposicaoAgenteNocivoHandler(
    IExposicaoAgenteNocivoRepository exposicoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarExposicaoAgenteNocivoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarExposicaoAgenteNocivoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var exposicao = await exposicoes.ObterPorIdAsync(new ExposicaoAgenteNocivoId(request.ExposicaoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Exposicao nao encontrada.");

        exposicao.Encerrar(request.FimExposicao);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
