using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Ppa;

/// <summary>Cria um Plano Plurianual (PPA) em elaboração.</summary>
/// <param name="AnoInicio">Primeiro ano do quadriênio.</param>
/// <param name="NumeroLei">Número/identificação da lei.</param>
/// <param name="AnoLei">Ano da lei.</param>
public sealed record CriarPpaCommand(int AnoInicio, string NumeroLei, int AnoLei) : ICommand<Guid>;

/// <summary>Validação da criação de PPA.</summary>
public sealed class CriarPpaValidator : AbstractValidator<CriarPpaCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarPpaValidator()
    {
        RuleFor(c => c.AnoInicio).GreaterThanOrEqualTo(DotacaoOrcamentaria.ExercicioMinimo);
        RuleFor(c => c.NumeroLei).NotEmpty().MaximumLength(40);
        RuleFor(c => c.AnoLei).GreaterThanOrEqualTo(DotacaoOrcamentaria.ExercicioMinimo);
    }
}

/// <summary>Handler da criação de PPA.</summary>
public sealed class CriarPpaHandler(IPpaRepository ppas, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<CriarPpaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarPpaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = PlanoPlurianual.Criar(tenant.TenantId, request.AnoInicio, request.NumeroLei, request.AnoLei);
        ppas.Adicionar(ppa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ppa.Id.Value;
    }
}

/// <summary>Adiciona um programa ao PPA.</summary>
public sealed record AdicionarProgramaCommand(
    Guid PpaId,
    string Codigo,
    string Nome,
    string Objetivo,
    string PublicoAlvo,
    string Indicador,
    decimal IndicadorLinhaBase,
    decimal IndicadorMeta) : ICommand<Guid>;

/// <summary>Validação da adição de programa.</summary>
public sealed class AdicionarProgramaValidator : AbstractValidator<AdicionarProgramaCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarProgramaValidator()
    {
        RuleFor(c => c.PpaId).NotEmpty();
        RuleFor(c => c.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Objetivo).NotEmpty().MaximumLength(1000);
        RuleFor(c => c.PublicoAlvo).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Indicador).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler da adição de programa.</summary>
public sealed class AdicionarProgramaHandler(IPpaRepository ppas, IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarProgramaCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarProgramaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new PpaId(request.PpaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA nao encontrado.");

        var programaId = ppa.AdicionarPrograma(
            request.Codigo, request.Nome, request.Objetivo, request.PublicoAlvo,
            request.Indicador, request.IndicadorLinhaBase, request.IndicadorMeta);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return programaId.Value;
    }
}

/// <summary>Adiciona uma ação a um programa do PPA.</summary>
public sealed record AdicionarAcaoCommand(
    Guid PpaId,
    Guid ProgramaId,
    string Codigo,
    string Nome,
    TipoAcao Tipo,
    string FuncionalProgramatica,
    string Produto,
    string UnidadeMedida) : ICommand<Guid>;

/// <summary>Validação da adição de ação.</summary>
public sealed class AdicionarAcaoValidator : AbstractValidator<AdicionarAcaoCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarAcaoValidator()
    {
        RuleFor(c => c.PpaId).NotEmpty();
        RuleFor(c => c.ProgramaId).NotEmpty();
        RuleFor(c => c.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Tipo).Must(t => Enum.IsDefined(t)).WithMessage("Tipo de acao invalido.");
        RuleFor(c => c.FuncionalProgramatica).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Produto).NotEmpty().MaximumLength(200);
        RuleFor(c => c.UnidadeMedida).NotEmpty().MaximumLength(30);
    }
}

/// <summary>Handler da adição de ação.</summary>
public sealed class AdicionarAcaoHandler(IPpaRepository ppas, IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarAcaoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarAcaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new PpaId(request.PpaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA nao encontrado.");

        var acaoId = ppa.AdicionarAcao(
            new ProgramaId(request.ProgramaId), request.Codigo, request.Nome, request.Tipo,
            request.FuncionalProgramatica, request.Produto, request.UnidadeMedida);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return acaoId.Value;
    }
}

/// <summary>Define (cria/atualiza) uma meta física/financeira de uma ação num ano do quadriênio.</summary>
public sealed record DefinirMetaCommand(
    Guid PpaId,
    Guid AcaoId,
    int Ano,
    decimal MetaFisica,
    string UnidadeMedida,
    decimal MetaFinanceira,
    string Regiao) : ICommand;

/// <summary>Validação da definição de meta.</summary>
public sealed class DefinirMetaValidator : AbstractValidator<DefinirMetaCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirMetaValidator()
    {
        RuleFor(c => c.PpaId).NotEmpty();
        RuleFor(c => c.AcaoId).NotEmpty();
        RuleFor(c => c.MetaFisica).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.UnidadeMedida).NotEmpty().MaximumLength(30);
        RuleFor(c => c.MetaFinanceira).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.Regiao).NotEmpty().MaximumLength(100);
    }
}

/// <summary>Handler da definição de meta.</summary>
public sealed class DefinirMetaHandler(IPpaRepository ppas, IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirMetaCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirMetaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new PpaId(request.PpaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA nao encontrado.");

        ppa.DefinirMeta(
            new AcaoPpaId(request.AcaoId), request.Ano, request.MetaFisica, request.UnidadeMedida,
            ValorMonetario.De(request.MetaFinanceira), request.Regiao);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Coloca o PPA em tramitação.</summary>
public sealed record TramitarPpaCommand(Guid PpaId) : ICommand;

/// <summary>Handler da tramitação do PPA.</summary>
public sealed class TramitarPpaHandler(IPpaRepository ppas, IUnitOfWork unitOfWork)
    : ICommandHandler<TramitarPpaCommand>
{
    /// <inheritdoc />
    public async Task Handle(TramitarPpaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new PpaId(request.PpaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA nao encontrado.");
        ppa.ColocarEmTramitacao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Coloca o PPA em vigor (lei sancionada).</summary>
public sealed record VigorarPpaCommand(Guid PpaId, string NumeroLei, int AnoLei) : ICommand;

/// <summary>Validação da vigência do PPA.</summary>
public sealed class VigorarPpaValidator : AbstractValidator<VigorarPpaCommand>
{
    /// <summary>Define as regras.</summary>
    public VigorarPpaValidator()
    {
        RuleFor(c => c.PpaId).NotEmpty();
        RuleFor(c => c.NumeroLei).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da vigência do PPA.</summary>
public sealed class VigorarPpaHandler(IPpaRepository ppas, IUnitOfWork unitOfWork)
    : ICommandHandler<VigorarPpaCommand>
{
    /// <inheritdoc />
    public async Task Handle(VigorarPpaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new PpaId(request.PpaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA nao encontrado.");
        ppa.Vigorar(request.NumeroLei, request.AnoLei);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
