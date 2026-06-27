using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Sst;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Sst;

/// <summary>
/// Comunica um Acidente de Trabalho (CAT) de um servidor — base do S-2210. Reabertura/obito exigem a CAT
/// de origem (inicial); a comunicacao de obito exige a data do obito.
/// </summary>
/// <param name="ServidorId">Servidor acidentado.</param>
/// <param name="TipoCat">Tipo da CAT (inicial/reabertura/obito).</param>
/// <param name="TipoAcidente">Tipo do acidente (tipico/doenca/trajeto).</param>
/// <param name="DataHoraAcidente">Data/hora do acidente.</param>
/// <param name="DescricaoSituacao">Descricao da situacao geradora.</param>
/// <param name="HouveObito">Indica obito.</param>
/// <param name="DataObito">Data do obito (obrigatoria quando houve obito).</param>
/// <param name="Cid">CID-10 (opcional).</param>
/// <param name="ParteCorpoAtingida">Parte do corpo atingida (opcional).</param>
/// <param name="AgenteCausador">Agente causador (opcional).</param>
/// <param name="CatOrigemId">CAT de origem (obrigatoria em reabertura/obito).</param>
public sealed record ComunicarAcidenteCommand(
    Guid ServidorId,
    TipoCat TipoCat,
    TipoAcidente TipoAcidente,
    DateTimeOffset DataHoraAcidente,
    string DescricaoSituacao,
    bool HouveObito = false,
    DateOnly? DataObito = null,
    string? Cid = null,
    string? ParteCorpoAtingida = null,
    string? AgenteCausador = null,
    Guid? CatOrigemId = null) : ICommand<Guid>;

/// <summary>Regras de validacao da comunicacao de acidente.</summary>
public sealed class ComunicarAcidenteValidator : AbstractValidator<ComunicarAcidenteCommand>
{
    /// <summary>Define as regras.</summary>
    public ComunicarAcidenteValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.TipoCat).IsInEnum().WithMessage("Tipo de CAT invalido.");
        RuleFor(c => c.TipoAcidente).IsInEnum().WithMessage("Tipo de acidente invalido.");
        RuleFor(c => c.DataHoraAcidente).NotEmpty().WithMessage("Data/hora do acidente e obrigatoria.");
        RuleFor(c => c.DescricaoSituacao).NotEmpty().WithMessage("Descricao da situacao e obrigatoria.");
        RuleFor(c => c.CatOrigemId)
            .NotNull()
            .When(c => c.TipoCat is TipoCat.Reabertura or TipoCat.ComunicacaoObito)
            .WithMessage("CAT de reabertura/obito exige a CAT de origem.");
        RuleFor(c => c.DataObito)
            .NotNull()
            .When(c => c.HouveObito)
            .WithMessage("Comunicacao de obito exige a data do obito.");
    }
}

/// <summary>Handler da comunicacao de acidente.</summary>
public sealed class ComunicarAcidenteHandler(
    IServidorRepository servidores,
    IComunicacaoAcidenteRepository cats,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<ComunicarAcidenteCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(ComunicarAcidenteCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        // Reabertura/obito referenciam a CAT inicial: valida a existencia da origem no tenant.
        ComunicacaoAcidenteId? origem = null;
        if (request.CatOrigemId is { } origemId)
        {
            var catOrigem = await cats.ObterPorIdAsync(new ComunicacaoAcidenteId(origemId), cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("CAT de origem nao encontrada.");
            origem = catOrigem.Id;
        }

        var cat = ComunicacaoAcidente.Comunicar(
            tenant.TenantId,
            servidor.Id,
            request.TipoCat,
            request.TipoAcidente,
            request.DataHoraAcidente,
            request.DescricaoSituacao,
            request.HouveObito,
            request.DataObito,
            request.Cid,
            request.ParteCorpoAtingida,
            request.AgenteCausador,
            origem);

        cats.Adicionar(cat);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return cat.Id.Value;
    }
}
