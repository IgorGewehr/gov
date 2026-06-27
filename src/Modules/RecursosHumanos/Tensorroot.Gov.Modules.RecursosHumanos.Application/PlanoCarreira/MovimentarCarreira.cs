using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.Tempo;
using DominioCargo = Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos.Cargo;
using DominioPlano = Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira.PlanoCarreira;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.PlanoCarreira;

/// <summary>
/// Concede uma PROGRESSAO HORIZONTAL (avanco de referencia na mesma classe, por tempo de servico e/ou
/// avaliacao de desempenho) ao servidor enquadrado, aplicando o novo vencimento ao cargo.
/// </summary>
/// <param name="ServidorId">Servidor enquadrado.</param>
/// <param name="Criterio">Criterio que fundamenta a progressao (tempo/avaliacao/ambos).</param>
/// <param name="Fundamento">Fundamento (processo/justificativa) da progressao.</param>
/// <param name="PortariaId">Portaria que formaliza a progressao (opcional).</param>
/// <param name="DataEfeito">Data de efeito; quando nula, usa o "hoje" do tenant.</param>
public sealed record ConcederProgressaoCommand(
    Guid ServidorId,
    CriterioProgressao Criterio,
    string Fundamento,
    Guid? PortariaId,
    DateOnly? DataEfeito) : ICommand<decimal>;

/// <summary>Regras de validacao da progressao.</summary>
public sealed class ConcederProgressaoValidator : AbstractValidator<ConcederProgressaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ConcederProgressaoValidator()
    {
        RuleFor(comando => comando.ServidorId).NotEmpty();
        RuleFor(comando => comando.Criterio).IsInEnum();
        RuleFor(comando => comando.Fundamento).NotEmpty();
    }
}

/// <summary>Handler da progressao horizontal.</summary>
public sealed class ConcederProgressaoHandler(
    IEnquadramentoServidorRepository enquadramentos,
    IPlanoCarreiraRepository planos,
    IServidorRepository servidores,
    ICargoRepository cargos,
    IUnitOfWork unitOfWork,
    IDataHojeTenant dataHoje)
    : ICommandHandler<ConcederProgressaoCommand, decimal>
{
    /// <inheritdoc />
    public async Task<decimal> Handle(ConcederProgressaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contexto = await MovimentacaoCarreiraServico
            .CarregarAsync(new ServidorId(request.ServidorId), enquadramentos, planos, servidores, cargos, cancellationToken)
            .ConfigureAwait(false);

        var dataEfeito = request.DataEfeito ?? dataHoje.Hoje();
        var novoVencimento = contexto.Enquadramento.ConcederProgressao(
            contexto.Plano,
            request.Criterio,
            dataEfeito,
            request.Fundamento,
            request.PortariaId);

        contexto.Cargo.AlterarVencimento(novoVencimento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return novoVencimento.Valor;
    }
}

/// <summary>
/// Concede uma PROMOCAO VERTICAL (avanco de classe, voltando a referencia inicial — por titulacao/
/// antiguidade) ao servidor enquadrado, aplicando o novo vencimento ao cargo.
/// </summary>
/// <param name="ServidorId">Servidor enquadrado.</param>
/// <param name="Fundamento">Fundamento (lei/titulacao/processo) da promocao.</param>
/// <param name="PortariaId">Portaria que formaliza a promocao (opcional).</param>
/// <param name="DataEfeito">Data de efeito; quando nula, usa o "hoje" do tenant.</param>
public sealed record ConcederPromocaoCommand(
    Guid ServidorId,
    string Fundamento,
    Guid? PortariaId,
    DateOnly? DataEfeito) : ICommand<decimal>;

/// <summary>Regras de validacao da promocao.</summary>
public sealed class ConcederPromocaoValidator : AbstractValidator<ConcederPromocaoCommand>
{
    /// <summary>Define as regras.</summary>
    public ConcederPromocaoValidator()
    {
        RuleFor(comando => comando.ServidorId).NotEmpty();
        RuleFor(comando => comando.Fundamento).NotEmpty();
    }
}

/// <summary>Handler da promocao vertical.</summary>
public sealed class ConcederPromocaoHandler(
    IEnquadramentoServidorRepository enquadramentos,
    IPlanoCarreiraRepository planos,
    IServidorRepository servidores,
    ICargoRepository cargos,
    IUnitOfWork unitOfWork,
    IDataHojeTenant dataHoje)
    : ICommandHandler<ConcederPromocaoCommand, decimal>
{
    /// <inheritdoc />
    public async Task<decimal> Handle(ConcederPromocaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contexto = await MovimentacaoCarreiraServico
            .CarregarAsync(new ServidorId(request.ServidorId), enquadramentos, planos, servidores, cargos, cancellationToken)
            .ConfigureAwait(false);

        var dataEfeito = request.DataEfeito ?? dataHoje.Hoje();
        var novoVencimento = contexto.Enquadramento.ConcederPromocao(
            contexto.Plano,
            dataEfeito,
            request.Fundamento,
            request.PortariaId);

        contexto.Cargo.AlterarVencimento(novoVencimento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return novoVencimento.Valor;
    }
}

/// <summary>
/// Servico de carga compartilhado pelas movimentacoes da carreira: resolve o enquadramento vigente, o
/// plano correspondente e o cargo provido do servidor (alvo do efeito remuneratorio), com mensagens de
/// erro claras. Mantem os handlers de progressao/promocao enxutos e sem duplicacao.
/// </summary>
internal static class MovimentacaoCarreiraServico
{
    internal sealed record Contexto(
        EnquadramentoServidor Enquadramento,
        DominioPlano Plano,
        DominioCargo Cargo);

    internal static async Task<Contexto> CarregarAsync(
        ServidorId servidorId,
        IEnquadramentoServidorRepository enquadramentos,
        IPlanoCarreiraRepository planos,
        IServidorRepository servidores,
        ICargoRepository cargos,
        CancellationToken cancellationToken)
    {
        var enquadramento = await enquadramentos.ObterPorServidorAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao esta enquadrado em plano de carreira; enquadre antes de movimentar.");
        var plano = await planos.ObterPorIdAsync(enquadramento.PlanoCarreiraId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Plano de carreira do enquadramento nao encontrado.");
        var servidor = await servidores.ObterPorIdAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");
        var cargo = await cargos.ObterPorIdAsync(servidor.CargoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo do servidor nao encontrado para aplicar o vencimento.");
        return new Contexto(enquadramento, plano, cargo);
    }
}
