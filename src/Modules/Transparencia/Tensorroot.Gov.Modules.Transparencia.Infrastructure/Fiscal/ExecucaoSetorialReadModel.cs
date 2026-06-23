using Microsoft.EntityFrameworkCore;
using Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;
using Tensorroot.Gov.Modules.Transparencia.Domain.Fiscal;
using Tensorroot.Gov.Modules.Transparencia.Infrastructure.Persistence;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Fiscal;

/// <summary>
/// <b>M7.0.0 — implementação Via A2 da fonte de dados de execução.</b> Deriva a
/// <see cref="ExecucaoSetorial"/> classificada a partir das linhas <c>LinhaExecucaoFiscal</c>
/// (projetadas da contabilidade) cruzadas com as regras <see cref="FonteRecursoVinculado"/> vigentes do
/// tenant. A receita-base soma as linhas de imposto/transferência; cada despesa é classificada por
/// função/fonte e só entra no setor se a regra a marca como computável. Determinístico (sem relógio):
/// usa 31/12 do exercício como referência de vigência das regras.
/// <para>
/// A migração para a Via A1 (campos decompostos já carimbados no contrato de Finanças) troca APENAS
/// esta classe — o domínio e os apuradores não mudam (a porta <see cref="IExecucaoSetorialReadModel"/>).
/// </para>
/// </summary>
public sealed class ExecucaoSetorialReadModel(
    TransparenciaDbContext context,
    IFonteRecursoVinculadoRepository regras)
    : IExecucaoSetorialReadModel
{
    /// <inheritdoc />
    public async Task<ExecucaoSetorial> ObterExecucaoAsync(int exercicio, CancellationToken cancellationToken)
    {
        var linhas = await context.LinhasExecucaoFiscal
            .Where(linha => linha.Exercicio == exercicio)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Referência de vigência reprodutível: regras vigentes até o fim do exercício apurado.
        var referencia = new DateOnly(exercicio, 12, 31);
        var regrasVigentes = await regras.ListarVigentesAsync(referencia, cancellationToken).ConfigureAwait(false);

        decimal receitaBase = 0m;
        var aplicadoPorSetor = new Dictionary<SetorMinimo, decimal>();

        foreach (var linha in linhas)
        {
            if (linha.Tipo == TipoLinhaExecucao.ReceitaBaseImpostosTransferencias)
            {
                receitaBase += linha.Valor;
                continue;
            }

            // Despesa setorial: classifica por função/fonte e só soma se computável no mínimo.
            if (linha.Funcao is null)
            {
                continue;
            }

            var codigo = CodigoFuncional.De(linha.Funcao);
            var classificacao = ClassificadorFonteRecurso.Classificar(codigo, linha.FonteRecurso, regrasVigentes);
            if (classificacao.Setor == SetorMinimo.Nenhum || !classificacao.ComputaNoMinimo)
            {
                continue;
            }

            aplicadoPorSetor[classificacao.Setor] =
                aplicadoPorSetor.GetValueOrDefault(classificacao.Setor) + linha.Valor;
        }

        var despesas = aplicadoPorSetor
            .Select(par => new DespesaSetorialApurada(par.Key, par.Value))
            .ToList();

        return new ExecucaoSetorial(exercicio, receitaBase, despesas);
    }
}
