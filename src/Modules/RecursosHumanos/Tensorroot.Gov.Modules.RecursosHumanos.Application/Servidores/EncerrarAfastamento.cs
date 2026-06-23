using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>
/// Encerra um afastamento vigente (retorno do servidor): grava o fim efetivo, recoloca o servidor em
/// exercicio (espelho) e, se o tipo NAO conta tempo (licenca sem vencimento etc.), acumula os dias do
/// periodo como NAO-COMPUTAVEIS (atrasam a estabilidade — design RH §3.3).
/// </summary>
/// <param name="AfastamentoId">Afastamento a encerrar.</param>
/// <param name="FimEfetivo">Data efetiva de retorno.</param>
public sealed record EncerrarAfastamentoCommand(Guid AfastamentoId, DateOnly FimEfetivo) : ICommand;

/// <summary>Regras de validacao do encerramento de afastamento.</summary>
public sealed class EncerrarAfastamentoValidator : AbstractValidator<EncerrarAfastamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarAfastamentoValidator()
        => RuleFor(c => c.AfastamentoId).NotEmpty().WithMessage("Afastamento e obrigatorio.");
}

/// <summary>Handler do encerramento de afastamento.</summary>
public sealed class EncerrarAfastamentoHandler(
    IAfastamentoRepository afastamentos,
    IServidorRepository servidores,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EncerrarAfastamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(EncerrarAfastamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var afastamento = await afastamentos.ObterPorIdAsync(new AfastamentoId(request.AfastamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Afastamento nao encontrado.");

        var contaTempo = afastamento.ContaTempo;
        var inicio = afastamento.Inicio;

        afastamento.Encerrar(request.FimEfetivo);

        var servidor = await servidores.ObterPorIdAsync(afastamento.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");
        servidor.RetornarDeAfastamento();

        // Contagem de tempo: periodos com ContaTempo=false ADIAM a elegibilidade a estabilidade.
        if (!contaTempo)
        {
            var dias = request.FimEfetivo.DayNumber - inicio.DayNumber + 1;
            if (dias > 0)
            {
                servidor.AcumularDiasNaoComputaveis(dias);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Cancela um afastamento vigente lancado por engano/revogado (sem efeito na folha).</summary>
/// <param name="AfastamentoId">Afastamento a cancelar.</param>
/// <param name="Motivo">Motivo do cancelamento.</param>
public sealed record CancelarAfastamentoCommand(Guid AfastamentoId, string Motivo) : ICommand;

/// <summary>Regras de validacao do cancelamento de afastamento.</summary>
public sealed class CancelarAfastamentoValidator : AbstractValidator<CancelarAfastamentoCommand>
{
    /// <summary>Define as regras.</summary>
    public CancelarAfastamentoValidator()
    {
        RuleFor(c => c.AfastamentoId).NotEmpty().WithMessage("Afastamento e obrigatorio.");
        RuleFor(c => c.Motivo).NotEmpty().WithMessage("Motivo do cancelamento e obrigatorio.");
    }
}

/// <summary>Handler do cancelamento de afastamento.</summary>
public sealed class CancelarAfastamentoHandler(
    IAfastamentoRepository afastamentos,
    IServidorRepository servidores,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelarAfastamentoCommand>
{
    /// <inheritdoc />
    public async Task Handle(CancelarAfastamentoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var afastamento = await afastamentos.ObterPorIdAsync(new AfastamentoId(request.AfastamentoId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Afastamento nao encontrado.");

        afastamento.Cancelar(request.Motivo);

        // Desfaz o espelho de situacao no servidor (volta ao exercicio), pois o afastamento nao ocorreu.
        var servidor = await servidores.ObterPorIdAsync(afastamento.ServidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");
        servidor.RetornarDeAfastamento();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
