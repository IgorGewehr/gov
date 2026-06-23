using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Planejamento.Lrf;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;
using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Ldo;

/// <summary>Cria uma LDO vinculada a um PPA vigente que cobre o exercício.</summary>
public sealed record CriarLdoCommand(int Exercicio, Guid PpaId, string NumeroLei, int AnoLei) : ICommand<Guid>;

/// <summary>Validação da criação de LDO.</summary>
public sealed class CriarLdoValidator : AbstractValidator<CriarLdoCommand>
{
    /// <summary>Define as regras.</summary>
    public CriarLdoValidator()
    {
        RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(DotacaoOrcamentaria.ExercicioMinimo);
        RuleFor(c => c.PpaId).NotEmpty();
        RuleFor(c => c.NumeroLei).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da criação de LDO.</summary>
public sealed class CriarLdoHandler(IPpaRepository ppas, ILdoRepository ldos, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<CriarLdoCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CriarLdoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ppa = await ppas.ObterPorIdAsync(new PpaId(request.PpaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA nao encontrado.");

        var ldo = LeiDiretrizes.Criar(tenant.TenantId, request.Exercicio, ppa, request.NumeroLei, request.AnoLei);
        ldos.Adicionar(ldo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ldo.Id.Value;
    }
}

/// <summary>Prioriza uma ação do PPA na LDO (CF 165 §2º).</summary>
public sealed record PriorizarAcaoCommand(Guid LdoId, Guid AcaoPpaId, int Ordem, string Justificativa) : ICommand;

/// <summary>Validação da priorização.</summary>
public sealed class PriorizarAcaoValidator : AbstractValidator<PriorizarAcaoCommand>
{
    /// <summary>Define as regras.</summary>
    public PriorizarAcaoValidator()
    {
        RuleFor(c => c.LdoId).NotEmpty();
        RuleFor(c => c.AcaoPpaId).NotEmpty();
        RuleFor(c => c.Ordem).GreaterThanOrEqualTo(1);
        RuleFor(c => c.Justificativa).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>Handler da priorização (carrega o PPA para a validação de domínio).</summary>
public sealed class PriorizarAcaoHandler(ILdoRepository ldos, IPpaRepository ppas, IUnitOfWork unitOfWork)
    : ICommandHandler<PriorizarAcaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(PriorizarAcaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new LdoId(request.LdoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LDO nao encontrada.");
        var ppa = await ppas.ObterPorIdAsync(ldo.PpaId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("PPA da LDO nao encontrado.");

        ldo.PriorizarAcao(ppa, new AcaoPpaId(request.AcaoPpaId), request.Ordem, request.Justificativa);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Define a meta fiscal de um ano do triênio (LRF art. 4º §1º).</summary>
public sealed record DefinirMetaFiscalCommand(
    Guid LdoId,
    int Ano,
    decimal ReceitaTotal,
    decimal DespesaTotal,
    decimal ResultadoPrimario,
    decimal ResultadoNominal,
    decimal DividaConsolidada) : ICommand;

/// <summary>Validação da meta fiscal.</summary>
public sealed class DefinirMetaFiscalValidator : AbstractValidator<DefinirMetaFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public DefinirMetaFiscalValidator()
    {
        RuleFor(c => c.LdoId).NotEmpty();
        RuleFor(c => c.ReceitaTotal).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.DespesaTotal).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.DividaConsolidada).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da meta fiscal.</summary>
public sealed class DefinirMetaFiscalHandler(ILdoRepository ldos, IUnitOfWork unitOfWork)
    : ICommandHandler<DefinirMetaFiscalCommand>
{
    /// <inheritdoc />
    public async Task Handle(DefinirMetaFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new LdoId(request.LdoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LDO nao encontrada.");

        ldo.DefinirMetaFiscal(
            request.Ano,
            ValorMonetario.De(request.ReceitaTotal),
            ValorMonetario.De(request.DespesaTotal),
            request.ResultadoPrimario,
            request.ResultadoNominal,
            ValorMonetario.De(request.DividaConsolidada));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Anexa um documento da LRF (AMF/ARF) à LDO.</summary>
public sealed record AnexarLdoCommand(Guid LdoId, TipoAnexoLdo Tipo, string ReferenciaDocumento) : ICommand;

/// <summary>Validação do anexo.</summary>
public sealed class AnexarLdoValidator : AbstractValidator<AnexarLdoCommand>
{
    /// <summary>Define as regras.</summary>
    public AnexarLdoValidator()
    {
        RuleFor(c => c.LdoId).NotEmpty();
        RuleFor(c => c.Tipo).Must(t => Enum.IsDefined(t)).WithMessage("Tipo de anexo invalido.");
        RuleFor(c => c.ReferenciaDocumento).NotEmpty().MaximumLength(200);
    }
}

/// <summary>Handler do anexo (a obrigatoriedade vem das opções LRF do tenant).</summary>
public sealed class AnexarLdoHandler(ILdoRepository ldos, IUnitOfWork unitOfWork, OpcoesPlanejamentoLrf opcoes)
    : ICommandHandler<AnexarLdoCommand>
{
    /// <inheritdoc />
    public async Task Handle(AnexarLdoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new LdoId(request.LdoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LDO nao encontrada.");

        var obrigatorio = request.Tipo == TipoAnexoLdo.AnexoMetasFiscais
            ? opcoes.ExigirAnexoMetasFiscais
            : opcoes.ExigirAnexoRiscosFiscais;

        ldo.Anexar(request.Tipo, request.ReferenciaDocumento, obrigatorio);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Coloca a LDO em tramitação.</summary>
public sealed record TramitarLdoCommand(Guid LdoId) : ICommand;

/// <summary>Handler da tramitação da LDO.</summary>
public sealed class TramitarLdoHandler(ILdoRepository ldos, IUnitOfWork unitOfWork)
    : ICommandHandler<TramitarLdoCommand>
{
    /// <inheritdoc />
    public async Task Handle(TramitarLdoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new LdoId(request.LdoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LDO nao encontrada.");
        ldo.ColocarEmTramitacao();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Coloca a LDO em vigor (valida anexos obrigatórios conforme opções do tenant).</summary>
public sealed record VigorarLdoCommand(Guid LdoId, string NumeroLei, int AnoLei) : ICommand;

/// <summary>Validação da vigência da LDO.</summary>
public sealed class VigorarLdoValidator : AbstractValidator<VigorarLdoCommand>
{
    /// <summary>Define as regras.</summary>
    public VigorarLdoValidator()
    {
        RuleFor(c => c.LdoId).NotEmpty();
        RuleFor(c => c.NumeroLei).NotEmpty().MaximumLength(40);
    }
}

/// <summary>Handler da vigência da LDO.</summary>
public sealed class VigorarLdoHandler(ILdoRepository ldos, IUnitOfWork unitOfWork, OpcoesPlanejamentoLrf opcoes)
    : ICommandHandler<VigorarLdoCommand>
{
    /// <inheritdoc />
    public async Task Handle(VigorarLdoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ldo = await ldos.ObterPorIdAsync(new LdoId(request.LdoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("LDO nao encontrada.");

        ldo.Vigorar(request.NumeroLei, request.AnoLei, opcoes.ExigirAnexoMetasFiscais, opcoes.ExigirAnexoRiscosFiscais);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
