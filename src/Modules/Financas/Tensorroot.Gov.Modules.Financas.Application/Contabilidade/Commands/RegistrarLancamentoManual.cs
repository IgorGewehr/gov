using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Commands;

/// <summary>Linha de um lançamento manual (código da conta + lado + valor).</summary>
/// <param name="CodigoConta">Código PCASP da conta analítica.</param>
/// <param name="Lado">Lado (Debito/Credito).</param>
/// <param name="Valor">Valor.</param>
public sealed record LinhaManual(string CodigoConta, LadoPartida Lado, decimal Valor);

/// <summary>Registra um lançamento contábil manual (contador).</summary>
/// <param name="Data">Data do lançamento.</param>
/// <param name="Historico">Histórico.</param>
/// <param name="Linhas">Partidas.</param>
public sealed record RegistrarLancamentoManualCommand(
    DateOnly Data,
    string Historico,
    IReadOnlyList<LinhaManual> Linhas) : ICommand<Guid>;

/// <summary>Validação do lançamento manual.</summary>
public sealed class RegistrarLancamentoManualValidator : AbstractValidator<RegistrarLancamentoManualCommand>
{
    /// <summary>Define as regras.</summary>
    public RegistrarLancamentoManualValidator()
    {
        RuleFor(c => c.Historico).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Linhas).NotEmpty();
        RuleForEach(c => c.Linhas).ChildRules(linha =>
        {
            linha.RuleFor(l => l.CodigoConta).NotEmpty();
            linha.RuleFor(l => l.Valor).GreaterThan(0m);
            linha.RuleFor(l => l.Lado).IsInEnum();
        });
    }
}

/// <summary>Handler do lançamento manual: resolve contas por código e aplica as invariantes de domínio.</summary>
public sealed class RegistrarLancamentoManualHandler(
    IContaContabilRepository contas,
    ILancamentoContabilRepository lancamentos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<RegistrarLancamentoManualCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(RegistrarLancamentoManualCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var linhas = new List<LinhaLancamento>(request.Linhas.Count);
        foreach (var item in request.Linhas)
        {
            var conta = await contas.ObterPorCodigoAsync(item.CodigoConta, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Conta {item.CodigoConta} nao encontrada.");

            linhas.Add(new LinhaLancamento(
                conta.Id,
                conta.Codigo,
                conta.NaturezaInformacao,
                conta.Tipo,
                item.Lado,
                ValorMonetario.De(item.Valor)));
        }

        // As invariantes (ΣD=ΣC, mesma natureza, conta folha) são validadas no factory de domínio.
        var lancamento = LancamentoContabil.Registrar(
            tenant.TenantId,
            request.Data,
            request.Data.Year,
            request.Historico,
            OrigemLancamento.Manual,
            origemReferenciaId: null,
            eventoContabilId: null,
            linhas,
            periodoAberto: true);

        lancamentos.Adicionar(lancamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return lancamento.Id.Value;
    }
}
