using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Educacao.Application.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;
using Tensorroot.Gov.Modules.Educacao.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Educacao.Infrastructure.Fiscal;

/// <summary>
/// <b>E-1 — implementação Via A2 da fonte de dados de execução de Educação.</b> Deriva a
/// <see cref="ExecucaoEducacao"/> a partir das linhas <see cref="LinhaExecucaoEducacao"/> projetadas da
/// contabilidade: soma as linhas de receita-base e converte cada despesa de Educação em
/// <see cref="DespesaEducacao"/> (funcional/fonte/valor) para o <c>ApuradorMde</c> classificar. NÃO
/// classifica aqui — a classificação MDE é do domínio (apurador + regras vigentes), mantendo a fronteira
/// limpa. Determinístico (sem relógio). Espelha o <c>ExecucaoSaudeReadModel</c> da Saúde.
/// <para>
/// A migração para a Via A1 (campos decompostos já carimbados no contrato de Finanças) troca APENAS o
/// alimentador das linhas — esta porta e o apurador não mudam.
/// </para>
/// </summary>
public sealed class ExecucaoEducacaoReadModel(EducacaoDbContext context) : IExecucaoEducacaoReadModel
{
    /// <inheritdoc />
    public async Task<ExecucaoEducacao> ObterExecucaoAsync(int exercicio, CancellationToken cancellationToken)
    {
        var linhas = await context.LinhasExecucaoEducacao
            .Where(linha => linha.Exercicio == exercicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal receitaBase = 0m;
        var despesas = new List<DespesaEducacao>();

        foreach (var linha in linhas)
        {
            if (linha.Tipo == TipoLinhaEducacao.ReceitaBaseImpostosTransferencias)
            {
                receitaBase += linha.Valor;
                continue;
            }

            if (linha.Funcao is null)
            {
                continue;
            }

            var codigo = CodigoFuncionalEducacao.De(linha.Funcao, linha.Subfuncao);
            despesas.Add(new DespesaEducacao(codigo, linha.FonteRecurso, linha.Valor));
        }

        return new ExecucaoEducacao(exercicio, receitaBase, despesas);
    }
}

/// <summary>
/// <b>E-1 — provedor do percentual mínimo MDE vigente.</b> Lê o parâmetro versionado
/// <c>mde.percentual-minimo</c> com vigência ≤ 31/12 do exercício (reprodutível); se o tenant ainda não
/// parametrizou, devolve o <b>default legal 25%</b> (CF art. 212) — nunca falha silenciosamente, mas
/// também não trava o tenant novo. O valor é parametrizável (Lei Orgânica pode fixar maior).
/// </summary>
public sealed class ParametroMdeProvider(EducacaoDbContext context) : IParametroMdeProvider
{
    /// <summary>Default legal do mínimo MDE (CF art. 212): 25%.</summary>
    public const decimal PercentualMinimoLegalMde = 0.25m;

    /// <inheritdoc />
    public async Task<decimal> ObterPercentualMinimoAsync(int exercicio, CancellationToken cancellationToken)
    {
        var referencia = new DateOnly(exercicio, 12, 31);
        var vigente = await context.ParametrosFiscaisEducacao
            .Where(p => p.Chave == ParametroFiscalEducacao.ChavePercentualMinimoMde && p.VigenciaInicio <= referencia)
            .OrderByDescending(p => p.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return vigente?.Valor ?? PercentualMinimoLegalMde;
    }
}

/// <summary>
/// <b>E-2 — provedor do piso vigente de aplicação do FUNDEB na remuneração dos profissionais.</b> Lê o
/// parâmetro versionado <c>fundeb.piso-remuneracao</c> com vigência ≤ 31/12 do exercício (reprodutível);
/// se o tenant ainda não parametrizou, devolve o <b>default legal 70%</b> (EC 108/2020). Parametrizável.
/// </summary>
public sealed class ParametroFundebProvider(EducacaoDbContext context) : IParametroFundebProvider
{
    /// <summary>Default legal do piso de remuneração do FUNDEB (EC 108/2020): 70%.</summary>
    public const decimal PisoRemuneracaoLegalFundeb = 0.70m;

    /// <inheritdoc />
    public async Task<decimal> ObterPisoRemuneracaoAsync(int exercicio, CancellationToken cancellationToken)
    {
        var referencia = new DateOnly(exercicio, 12, 31);
        var vigente = await context.ParametrosFiscaisEducacao
            .Where(p => p.Chave == ParametroFiscalEducacao.ChavePisoRemuneracaoFundeb && p.VigenciaInicio <= referencia)
            .OrderByDescending(p => p.VigenciaInicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return vigente?.Valor ?? PisoRemuneracaoLegalFundeb;
    }
}

/// <summary>
/// <b>E-2 — read model da remuneração dos profissionais da educação (numerador dos 70%).</b> Lê/grava o
/// total por <c>(Tenant, Exercicio)</c> em <see cref="RemuneracaoMagisterioExercicio"/>, alimentado pela
/// folha do RH (Contracts) ou pelo total informado pelo município. Idempotente por exercício.
/// </summary>
public sealed class RemuneracaoMagisterioReadModel(EducacaoDbContext context, ITenantContext tenant) : IRemuneracaoMagisterioReadModel
{
    /// <inheritdoc />
    public async Task<decimal> ObterRemuneracaoProfissionaisAsync(int exercicio, CancellationToken cancellationToken)
    {
        var registro = await context.RemuneracoesMagisterio
            .Where(r => r.Exercicio == exercicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return registro?.RemuneracaoProfissionais ?? 0m;
    }

    /// <inheritdoc />
    public async Task DefinirRemuneracaoProfissionaisAsync(int exercicio, decimal remuneracaoProfissionais, CancellationToken cancellationToken)
    {
        var registro = await context.RemuneracoesMagisterio
            .Where(r => r.Exercicio == exercicio)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (registro is null)
        {
            await context.RemuneracoesMagisterio
                .AddAsync(RemuneracaoMagisterioExercicio.Criar(tenant.TenantId, exercicio, remuneracaoProfissionais), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            registro.Atualizar(remuneracaoProfissionais);
        }
    }
}
