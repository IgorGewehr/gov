using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Saude.Application.Fiscal;
using Tensorroot.Gov.Modules.Saude.Domain.Fiscal;
using Tensorroot.Gov.Modules.Saude.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Saude.Infrastructure.Fiscal;

/// <summary>
/// <b>S-1 — implementação Via A2 da fonte de dados de execução de Saúde.</b> Deriva a
/// <see cref="ExecucaoSaude"/> a partir das linhas <see cref="LinhaExecucaoSaude"/> projetadas da
/// contabilidade: soma as linhas de receita-base e converte cada despesa de Saúde em
/// <see cref="DespesaSaude"/> (funcional/fonte/valor) para o <c>ApuradorAsps</c> classificar. NÃO
/// classifica aqui — a classificação ASPS é do domínio (apurador + regras vigentes), mantendo a fronteira
/// limpa. Determinístico (sem relógio).
/// <para>
/// A migração para a Via A1 (campos decompostos já carimbados no contrato de Finanças) troca APENAS o
/// alimentador das linhas — esta porta e o apurador não mudam.
/// </para>
/// </summary>
public sealed class ExecucaoSaudeReadModel(SaudeDbContext context) : IExecucaoSaudeReadModel
{
    /// <inheritdoc />
    public async Task<ExecucaoSaude> ObterExecucaoAsync(int exercicio, CancellationToken cancellationToken)
    {
        var linhas = await context.LinhasExecucaoSaude
            .Where(linha => linha.Exercicio == exercicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal receitaBase = 0m;
        var despesas = new List<DespesaSaude>();

        foreach (var linha in linhas)
        {
            if (linha.Tipo == TipoLinhaSaude.ReceitaBaseImpostosTransferencias)
            {
                receitaBase += linha.Valor;
                continue;
            }

            if (linha.Funcao is null)
            {
                continue;
            }

            var codigo = CodigoFuncionalSaude.De(linha.Funcao, linha.Subfuncao);
            despesas.Add(new DespesaSaude(codigo, linha.FonteRecurso, linha.Valor));
        }

        return new ExecucaoSaude(exercicio, receitaBase, despesas);
    }
}

/// <summary>
/// <b>S-1 — provedor do percentual mínimo ASPS vigente.</b> Lê o parâmetro versionado
/// <c>asps.percentual-minimo</c> com vigência ≤ 31/12 do exercício (reprodutível); se o tenant ainda não
/// parametrizou, devolve o <b>default legal 15%</b> (LC 141/2012) — nunca falha silenciosamente, mas
/// também não trava o tenant novo. O valor é parametrizável (Lei Orgânica pode fixar maior).
/// </summary>
public sealed class ParametroAspsProvider(SaudeDbContext context) : IParametroAspsProvider
{
    /// <summary>Default legal do mínimo ASPS (LC 141/2012, art. 7º): 15%.</summary>
    public const decimal PercentualMinimoLegalAsps = 0.15m;

    /// <inheritdoc />
    public async Task<decimal> ObterPercentualMinimoAsync(int exercicio, CancellationToken cancellationToken)
    {
        var referencia = new DateOnly(exercicio, 12, 31);
        var vigente = await context.ParametrosFiscaisSaude
            .Where(p => p.Chave == ParametroFiscalSaude.ChavePercentualMinimoAsps && p.VigenciaInicio <= referencia)
            .OrderByDescending(p => p.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return vigente?.Valor ?? PercentualMinimoLegalAsps;
    }
}
