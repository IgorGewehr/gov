using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.PlanoCarreira;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.PlanoCarreira;

/// <summary>
/// Enquadra um servidor numa posicao (classe + referencia) de um plano de carreira e APLICA o
/// vencimento da celula ao cargo provido pelo servidor (efeito remuneratorio), reusando
/// <c>Cargo.AlterarVencimento</c> — auditado pelo interceptor. Unico por servidor/tenant.
/// </summary>
/// <param name="ServidorId">Servidor a enquadrar.</param>
/// <param name="PlanoCarreiraId">Plano de carreira destino.</param>
/// <param name="Classe">Classe de ingresso (>= 1).</param>
/// <param name="Referencia">Referencia de ingresso (>= 1).</param>
/// <param name="Fundamento">Fundamento (lei/processo) do enquadramento.</param>
/// <param name="DataEfeito">Data de efeito; quando nula, usa o "hoje" do tenant.</param>
public sealed record EnquadrarServidorCommand(
    Guid ServidorId,
    Guid PlanoCarreiraId,
    int Classe,
    int Referencia,
    string Fundamento,
    DateOnly? DataEfeito) : ICommand<Guid>;

/// <summary>Regras de validacao do enquadramento.</summary>
public sealed class EnquadrarServidorValidator : AbstractValidator<EnquadrarServidorCommand>
{
    /// <summary>Define as regras.</summary>
    public EnquadrarServidorValidator()
    {
        RuleFor(comando => comando.ServidorId).NotEmpty();
        RuleFor(comando => comando.PlanoCarreiraId).NotEmpty();
        RuleFor(comando => comando.Classe).GreaterThanOrEqualTo(PosicaoCarreira.IndiceMinimo);
        RuleFor(comando => comando.Referencia).GreaterThanOrEqualTo(PosicaoCarreira.IndiceMinimo);
        RuleFor(comando => comando.Fundamento).NotEmpty();
    }
}

/// <summary>Handler do enquadramento de servidor na carreira.</summary>
public sealed class EnquadrarServidorHandler(
    IEnquadramentoServidorRepository enquadramentos,
    IPlanoCarreiraRepository planos,
    IServidorRepository servidores,
    ICargoRepository cargos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IDataHojeTenant dataHoje)
    : ICommandHandler<EnquadrarServidorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(EnquadrarServidorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var servidorId = new ServidorId(request.ServidorId);
        if (await enquadramentos.ExisteParaServidorAsync(servidorId, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Servidor ja possui enquadramento em plano de carreira; use progressao/promocao.");
        }

        var servidor = await servidores.ObterPorIdAsync(servidorId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");
        var plano = await planos.ObterPorIdAsync(new PlanoCarreiraId(request.PlanoCarreiraId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Plano de carreira nao encontrado.");

        var dataEfeito = request.DataEfeito ?? dataHoje.Hoje();
        var posicao = PosicaoCarreira.De(request.Classe, request.Referencia);

        var enquadramento = EnquadramentoServidor.Enquadrar(
            tenant.TenantId,
            servidorId,
            plano,
            posicao,
            dataEfeito,
            request.Fundamento);

        // Efeito remuneratorio: o vencimento da celula passa a vigorar no cargo do servidor.
        var vencimento = plano.VencimentoDa(posicao);
        var cargo = await cargos.ObterPorIdAsync(servidor.CargoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo do servidor nao encontrado para aplicar o vencimento do enquadramento.");
        cargo.AlterarVencimento(vencimento);

        enquadramentos.Adicionar(enquadramento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return enquadramento.Id.Value;
    }
}
