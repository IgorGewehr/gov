using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Fiscal;

/// <summary>
/// <b>M7.0.0.</b> Registra uma regra de classificação setorial (<see cref="FonteRecursoVinculado"/>) do
/// tenant, versionada por <c>VigenciaInicio</c>: mapeia <c>(função[, fonte]) → setor</c> e marca se a
/// despesa assim classificada <b>computa</b> no mínimo (LC 141 art. 3º computa / art. 4º não, p/ Saúde;
/// análogo MDE). Nada hardcoded — a regra nasce parametrizável por tenant+vigência (CLAUDE.md §7/§16).
/// </summary>
/// <param name="Funcao">Código da função de governo (2 dígitos, ex.: "10" Saúde, "12" Educação).</param>
/// <param name="Setor">Setor de mínimo ao qual a despesa pertence.</param>
/// <param name="VigenciaInicio">Início de vigência (inclusive) da regra.</param>
/// <param name="FonteRecurso">Fonte de recurso (opcional; refina a regra dentro da função).</param>
/// <param name="ComputaNoMinimo">Se a despesa assim classificada computa no mínimo (default <c>true</c>).</param>
public sealed record RegistrarRegraClassificacaoCommand(
    string Funcao,
    SetorMinimo Setor,
    DateOnly VigenciaInicio,
    string? FonteRecurso = null,
    bool ComputaNoMinimo = true) : ICommand<Guid>;

/// <summary>Handler que persiste a regra de classificação setorial do tenant.</summary>
public sealed class RegistrarRegraClassificacaoHandler(
    IFonteRecursoVinculadoRepository regras,
    ITenantContext tenant,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarRegraClassificacaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarRegraClassificacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var regra = FonteRecursoVinculado.Criar(
            tenant.TenantId,
            request.Funcao,
            request.Setor,
            request.VigenciaInicio,
            request.FonteRecurso,
            request.ComputaNoMinimo);

        await regras.AdicionarAsync(regra, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return regra.Id.Value;
    }
}
