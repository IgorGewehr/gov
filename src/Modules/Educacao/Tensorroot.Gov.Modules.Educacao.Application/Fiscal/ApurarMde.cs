using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

namespace Tensorroot.Gov.Modules.Educacao.Application.Fiscal;

/// <summary>Resultado da apuração do mínimo de Educação (25% MDE) de um exercício.</summary>
/// <param name="Exercicio">Ano de exercício apurado.</param>
/// <param name="Natureza">Natureza (aferição anual de conformidade / indicador bimestral informativo).</param>
/// <param name="ReceitaBase">Receita-base (impostos + transferências constitucionais — CF art. 212).</param>
/// <param name="AplicadoMde">Valor aplicado em MDE (despesa computável, LDB art. 70).</param>
/// <param name="PercentualAplicado">Percentual aplicado (0..1).</param>
/// <param name="PercentualMinimo">Percentual mínimo (limite) vigente.</param>
/// <param name="Atingido">Se o mínimo foi atingido.</param>
/// <param name="EhConformidade">Se este resultado é a aferição anual de conformidade (a que vale para o TCE).</param>
public sealed record ApuracaoMdeResultado(
    int Exercicio,
    NaturezaAferimentoMde Natureza,
    decimal ReceitaBase,
    decimal AplicadoMde,
    decimal PercentualAplicado,
    decimal PercentualMinimo,
    bool Atingido,
    bool EhConformidade);

/// <summary>
/// <b>E-1.</b> Apura o mínimo constitucional de Educação (25% MDE, CF art. 212) de um exercício,
/// tenant-scoped. Read-only e reprodutível (sem relógio): usa o exercício como âncora temporal. A
/// <see cref="Natureza"/> separa a <b>aferição anual de conformidade</b> (a que vale para o TCE) do
/// <b>indicador bimestral informativo</b> — evita alarme falso nos bimestres iniciais (RISCO #3).
/// </summary>
/// <param name="Exercicio">Ano de exercício a apurar.</param>
/// <param name="Natureza">Natureza da apuração (default: aferição anual de conformidade).</param>
public sealed record ApurarMdeQuery(
    int Exercicio,
    NaturezaAferimentoMde Natureza = NaturezaAferimentoMde.AferimentoAnual) : IQuery<ApuracaoMdeResultado>;

/// <summary>
/// Handler da apuração MDE: obtém a execução de Educação (<see cref="IExecucaoEducacaoReadModel"/>), as
/// regras de classificação vigentes (<see cref="IRegraClassificacaoMdeRepository"/>) e o percentual mínimo
/// vigente (<see cref="IParametroMdeProvider"/>), delega ao <see cref="ApuradorMde"/> e projeta o resultado.
/// Toda a regra fiscal é do domínio; o handler só orquestra as portas. Espelha o <c>ApurarAspsHandler</c>.
/// </summary>
public sealed class ApurarMdeHandler(
    IExecucaoEducacaoReadModel execucao,
    IRegraClassificacaoMdeRepository regras,
    IParametroMdeProvider parametros)
    : IQueryHandler<ApurarMdeQuery, ApuracaoMdeResultado>
{
    /// <inheritdoc />
    public async Task<ApuracaoMdeResultado> Handle(ApurarMdeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var execucaoEducacao = await execucao.ObterExecucaoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        // Referência de vigência reprodutível: regras vigentes até o fim do exercício apurado.
        var referencia = new DateOnly(request.Exercicio, 12, 31);
        var regrasVigentes = await regras.ListarVigentesAsync(referencia, cancellationToken).ConfigureAwait(false);

        var percentualMinimo = await parametros.ObterPercentualMinimoAsync(request.Exercicio, cancellationToken).ConfigureAwait(false);

        var indicador = ApuradorMde.Apurar(
            request.Natureza,
            execucaoEducacao.ReceitaBaseImpostosTransferencias,
            execucaoEducacao.DespesasEducacao,
            regrasVigentes,
            percentualMinimo);

        return new ApuracaoMdeResultado(
            request.Exercicio,
            indicador.Natureza,
            indicador.ReceitaBase,
            indicador.AplicadoMde,
            indicador.PercentualAplicado,
            indicador.PercentualMinimo,
            indicador.Atingido,
            indicador.EhConformidade);
    }
}
