using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ProcessosTrabalhistas;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ProcessosTrabalhistas;

/// <summary>Reavalia o prognostico de perda de um processo em andamento (recalcula a provisao).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="NovoPrognostico">Novo prognostico de perda.</param>
public sealed record ReavaliarPrognosticoCommand(Guid ProcessoId, PrognosticoPerda NovoPrognostico) : ICommand;

/// <summary>Regras de validacao da reavaliacao.</summary>
public sealed class ReavaliarPrognosticoValidator : AbstractValidator<ReavaliarPrognosticoCommand>
{
    /// <summary>Define as regras.</summary>
    public ReavaliarPrognosticoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
        RuleFor(comando => comando.NovoPrognostico).IsInEnum();
    }
}

/// <summary>Handler da reavaliacao de prognostico.</summary>
public sealed class ReavaliarPrognosticoHandler(IProcessoTrabalhistaRepository processos, IUnitOfWork unitOfWork)
    : ICommandHandler<ReavaliarPrognosticoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ReavaliarPrognosticoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await CarregarProcesso.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.ReavaliarPrognostico(request.NovoPrognostico);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Homologa um acordo encerrando o processo (grava o valor e ajusta a provisao).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="ValorAcordo">Valor homologado.</param>
/// <param name="DataAcordo">Data da homologacao.</param>
public sealed record RegistrarAcordoCommand(Guid ProcessoId, decimal ValorAcordo, DateOnly DataAcordo) : ICommand;

/// <summary>Regras de validacao do acordo.</summary>
public sealed class RegistrarAcordoValidator : AbstractValidator<RegistrarAcordoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarAcordoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
        RuleFor(comando => comando.ValorAcordo).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler do acordo.</summary>
public sealed class RegistrarAcordoHandler(IProcessoTrabalhistaRepository processos, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarAcordoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarAcordoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await CarregarProcesso.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.RegistrarAcordo(request.ValorAcordo, request.DataAcordo);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Registra o transito em julgado com condenacao do ente (grava o valor e fixa a provisao).</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="ValorCondenacao">Valor da condenacao.</param>
/// <param name="DataTransito">Data do transito em julgado.</param>
public sealed record RegistrarCondenacaoCommand(Guid ProcessoId, decimal ValorCondenacao, DateOnly DataTransito) : ICommand;

/// <summary>Regras de validacao da condenacao.</summary>
public sealed class RegistrarCondenacaoValidator : AbstractValidator<RegistrarCondenacaoCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarCondenacaoValidator()
    {
        RuleFor(comando => comando.ProcessoId).NotEmpty();
        RuleFor(comando => comando.ValorCondenacao).GreaterThanOrEqualTo(0m);
    }
}

/// <summary>Handler da condenacao.</summary>
public sealed class RegistrarCondenacaoHandler(IProcessoTrabalhistaRepository processos, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarCondenacaoCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarCondenacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await CarregarProcesso.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.RegistrarCondenacao(request.ValorCondenacao, request.DataTransito);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Encerra o processo sem saida de recursos (improcedencia/extincao); zera a provisao.</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
/// <param name="DataTransito">Data do transito em julgado.</param>
public sealed record RegistrarImprocedenciaCommand(Guid ProcessoId, DateOnly DataTransito) : ICommand;

/// <summary>Handler da improcedencia.</summary>
public sealed class RegistrarImprocedenciaHandler(IProcessoTrabalhistaRepository processos, IUnitOfWork unitOfWork)
    : ICommandHandler<RegistrarImprocedenciaCommand>
{
    /// <inheritdoc />
    public async Task Handle(RegistrarImprocedenciaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await CarregarProcesso.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.RegistrarImprocedencia(request.DataTransito);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Arquiva definitivamente um processo ja encerrado.</summary>
/// <param name="ProcessoId">Identificador do processo.</param>
public sealed record ArquivarProcessoTrabalhistaCommand(Guid ProcessoId) : ICommand;

/// <summary>Handler do arquivamento.</summary>
public sealed class ArquivarProcessoTrabalhistaHandler(IProcessoTrabalhistaRepository processos, IUnitOfWork unitOfWork)
    : ICommandHandler<ArquivarProcessoTrabalhistaCommand>
{
    /// <inheritdoc />
    public async Task Handle(ArquivarProcessoTrabalhistaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var processo = await CarregarProcesso.ObterAsync(processos, request.ProcessoId, cancellationToken).ConfigureAwait(false);
        processo.Arquivar();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Carga compartilhada do processo por id, com mensagem de erro consistente.</summary>
internal static class CarregarProcesso
{
    internal static async Task<ProcessoTrabalhista> ObterAsync(
        IProcessoTrabalhistaRepository processos, Guid id, CancellationToken cancellationToken)
        => await processos.ObterPorIdAsync(new ProcessoTrabalhistaId(id), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Processo trabalhista nao encontrado.");
}
