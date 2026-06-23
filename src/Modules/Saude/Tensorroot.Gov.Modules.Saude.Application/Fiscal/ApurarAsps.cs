using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Saude.Application.Fiscal;

/// <summary>Resultado da apuração do mínimo de Saúde (15% ASPS) de um exercício.</summary>
/// <param name="Exercicio">Ano de exercício apurado.</param>
/// <param name="ReceitaBase">Receita-base (impostos + transferências constitucionais).</param>
/// <param name="AplicadoAsps">Valor aplicado em ASPS (despesa computável, art. 3º).</param>
/// <param name="PercentualAplicado">Percentual aplicado (0..1).</param>
/// <param name="PercentualMinimo">Percentual mínimo (limite) vigente.</param>
/// <param name="Atingido">Se o mínimo foi atingido.</param>
public sealed record ApuracaoAspsResultado(
    int Exercicio,
    decimal ReceitaBase,
    decimal AplicadoAsps,
    decimal PercentualAplicado,
    decimal PercentualMinimo,
    bool Atingido);

/// <summary>
/// <b>S-1.</b> Apura o mínimo constitucional de Saúde (15% ASPS, LC 141/2012) de um exercício,
/// tenant-scoped. Read-only e reprodutível (sem relógio): usa o exercício como âncora temporal.
/// </summary>
/// <param name="Exercicio">Ano de exercício a apurar.</param>
public sealed record ApurarAspsQuery(int Exercicio) : IQuery<ApuracaoAspsResultado>;

/// <summary>
/// Handler da apuração ASPS: obtém a execução de Saúde (<see cref="IExecucaoSaudeReadModel"/>), as
/// regras de classificação vigentes (<see cref="IRegraClassificacaoAspsRepository"/>) e o percentual
/// mínimo vigente (<see cref="IParametroAspsProvider"/>), delega ao <see cref="ApuradorAsps"/> e projeta
/// o resultado. Toda a regra fiscal é do domínio; o handler só orquestra as portas.
/// </summary>
public sealed class ApurarAspsHandler(
    IExecucaoSaudeReadModel execucao,
    IRegraClassificacaoAspsRepository regras,
    IParametroAspsProvider parametros)
    : IQueryHandler<ApurarAspsQuery, ApuracaoAspsResultado>
{
    /// <inheritdoc />
    public async Task<ApuracaoAspsResultado> Handle(ApurarAspsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var execucaoSaude = await execucao.ObterExecucaoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        // Referência de vigência reprodutível: regras vigentes até o fim do exercício apurado.
        var referencia = new DateOnly(request.Exercicio, 12, 31);
        var regrasVigentes = await regras.ListarVigentesAsync(referencia, cancellationToken).ConfigureAwait(false);

        var percentualMinimo = await parametros.ObterPercentualMinimoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        var indicador = ApuradorAsps.Apurar(
            execucaoSaude.ReceitaBaseImpostosTransferencias,
            execucaoSaude.DespesasSaude,
            regrasVigentes,
            percentualMinimo);

        return new ApuracaoAspsResultado(
            request.Exercicio,
            indicador.ReceitaBase,
            indicador.AplicadoAsps,
            indicador.PercentualAplicado,
            indicador.PercentualMinimo,
            indicador.Atingido);
    }
}
