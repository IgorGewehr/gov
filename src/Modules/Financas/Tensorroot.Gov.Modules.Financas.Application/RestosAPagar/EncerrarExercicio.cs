using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.RestosAPagar;

namespace Tensorroot.Gov.Modules.Financas.Application.RestosAPagar;

/// <summary>
/// Encerra o exercício inscrevendo em Restos a Pagar os empenhos com saldo aberto
/// (Lei 4.320/64, art. 36). Processados = liquidados e não pagos; Não Processados = não liquidados.
/// </summary>
/// <param name="Exercicio">Exercício a encerrar.</param>
public sealed record EncerrarExercicioCommand(int Exercicio) : ICommand<int>;

/// <summary>Regras de validação do encerramento de exercício.</summary>
public sealed class EncerrarExercicioValidator : AbstractValidator<EncerrarExercicioCommand>
{
    /// <summary>Define as regras.</summary>
    public EncerrarExercicioValidator() => RuleFor(c => c.Exercicio).GreaterThanOrEqualTo(2000);
}

/// <summary>
/// Handler do encerramento de exercício. Para cada empenho com saldo aberto, inscreve um
/// Resto a Pagar classificado conforme tenha (Processado) ou não (Não Processado) liquidação.
/// </summary>
// TODO(revisao-contabil): operacao contabil sensivel (apuracao de superavit financeiro, RAP por
// fonte, lancamentos PCASP) — aqui apenas a inscricao de RAP por empenho; orquestracao detalhada
// fica para fase posterior.
public sealed class EncerrarExercicioHandler(
    IEmpenhoRepository empenhos,
    IRestoAPagarRepository restos,
    IUnitOfWork unitOfWork,
    ITenantContext tenant) : ICommandHandler<EncerrarExercicioCommand, int>
{
    /// <inheritdoc />
    public async Task<int> Handle(EncerrarExercicioCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var abertos = await empenhos.ListarComSaldoAbertoPorExercicioAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);
        var exercicioInscricao = request.Exercicio + 1;
        var inscritos = 0;

        foreach (var empenho in abertos)
        {
            if (!empenho.PossuiSaldoParaRestosAPagar())
            {
                continue;
            }

            // Lei 4.320/64 art. 36 c/c MCASP: o saldo aberto de UM empenho pode gerar DUAS inscricoes
            // distintas, cada uma na sua classificacao — nao um booleano unico que perde a parcela
            // empenhada-nao-liquidada:
            //   Processado     = Liquidado - Pago                 (RP Processado — liquidado nao pago)
            //   Nao Processado = (Empenhado - Anulado) - Liquidado (RP Nao Processado — empenhado nao liquidado)
            var valorProcessado = empenho.SaldoAPagar;                              // Liquidado - Pago
            var valorNaoProcessado = empenho.SaldoEmpenhado.Subtrair(empenho.ValorLiquidado); // SaldoEmpenhado - Liquidado

            if (valorProcessado.EhPositivo())
            {
                restos.Adicionar(RestoAPagar.Inscrever(
                    tenant.TenantId,
                    empenho.Id,
                    ClassificacaoRestoAPagar.Processado,
                    valorProcessado,
                    request.Exercicio,
                    exercicioInscricao));
                inscritos++;
            }

            if (valorNaoProcessado.EhPositivo())
            {
                restos.Adicionar(RestoAPagar.Inscrever(
                    tenant.TenantId,
                    empenho.Id,
                    ClassificacaoRestoAPagar.NaoProcessado,
                    valorNaoProcessado,
                    request.Exercicio,
                    exercicioInscricao));
                inscritos++;
            }

            empenho.InscreverEmRestosAPagar(exercicioInscricao);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return inscritos;
    }
}
