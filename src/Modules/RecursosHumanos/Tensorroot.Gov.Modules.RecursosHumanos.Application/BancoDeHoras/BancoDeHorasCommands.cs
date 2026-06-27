using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using DomainBancoDeHoras = Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto.BancoDeHoras;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.BancoDeHoras;

/// <summary>
/// Lanca manualmente um credito/debito no banco de horas de um servidor (ajuste administrativo:
/// compensacao formal, acordo, correcao). O banco e aberto sob demanda se ainda nao existir.
/// </summary>
/// <param name="ServidorId">Servidor titular.</param>
/// <param name="Minutos">Minutos do lancamento (&gt; 0).</param>
/// <param name="Credito">Verdadeiro para credito; falso para debito.</param>
/// <param name="Data">Data-base do lancamento.</param>
/// <param name="Descricao">Descricao do lancamento.</param>
public sealed record LancarBancoDeHorasCommand(
    Guid ServidorId,
    int Minutos,
    bool Credito,
    DateOnly Data,
    string Descricao) : ICommand<int>;

/// <summary>Regras de validacao do lancamento manual no banco de horas.</summary>
public sealed class LancarBancoDeHorasValidator : AbstractValidator<LancarBancoDeHorasCommand>
{
    /// <summary>Define as regras.</summary>
    public LancarBancoDeHorasValidator()
    {
        RuleFor(c => c.ServidorId).NotEmpty().WithMessage("Servidor e obrigatorio.");
        RuleFor(c => c.Minutos).GreaterThan(0).WithMessage("Minutos deve ser positivo.");
        RuleFor(c => c.Descricao).NotEmpty().WithMessage("Descricao e obrigatoria.");
    }
}

/// <summary>Handler do lancamento manual; devolve o saldo corrente apos o lancamento.</summary>
public sealed class LancarBancoDeHorasHandler(
    IServidorRepository servidores,
    IBancoDeHorasRepository bancos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    TimeProvider timeProvider)
    : ICommandHandler<LancarBancoDeHorasCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(LancarBancoDeHorasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Garante a existencia do servidor (isolamento e integridade referencial logica por tenant).
        _ = await servidores.ObterPorIdAsync(new Domain.Servidores.ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");

        var banco = await bancos.ObterPorServidorAsync(request.ServidorId, cancellationToken).ConfigureAwait(false);
        if (banco is null)
        {
            banco = DomainBancoDeHoras.Abrir(tenant.TenantId, request.ServidorId);
            bancos.Adicionar(banco);
        }

        // Referencia de negocio unica do ajuste manual (idempotencia por instante de registro + tipo).
        var carimbo = timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var referencia = $"manual:{(request.Credito ? "C" : "D")}:{carimbo}";

        if (request.Credito)
        {
            banco.Creditar(request.Minutos, request.Data, referencia, request.Descricao);
        }
        else
        {
            banco.Debitar(request.Minutos, request.Data, referencia, request.Descricao);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return banco.SaldoMinutos;
    }
}

/// <summary>
/// Executa a rotina de PRESCRICAO do banco de horas: para cada servidor, prescreve os creditos cuja
/// data-base e anterior a janela parametrizada (6/12 meses — <c>ParametrosPonto.BancoHorasJanelaMeses</c>),
/// debitando o montante prescrito. Idempotente por mes-limite.
/// </summary>
public sealed record ExecutarPrescricaoBancoDeHorasCommand : ICommand<int>;

/// <summary>Handler da rotina de prescricao em lote; devolve o total de minutos prescritos.</summary>
public sealed class ExecutarPrescricaoBancoDeHorasHandler(
    IBancoDeHorasRepository bancos,
    IParametrosPontoProvider parametros,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ExecutarPrescricaoBancoDeHorasCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(ExecutarPrescricaoBancoDeHorasCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var p = await parametros.ObterAsync(cancellationToken).ConfigureAwait(false);
        var hoje = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var limite = hoje.AddMonths(-p.BancoHorasJanelaMeses);

        var todos = await bancos.ListarTodosAsync(cancellationToken).ConfigureAwait(false);
        var totalPrescrito = 0;
        foreach (var banco in todos)
        {
            totalPrescrito += banco.PrescreverCreditosAnterioresA(limite, hoje);
        }

        if (totalPrescrito > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return totalPrescrito;
    }
}
