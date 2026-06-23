using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ldo;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Ppa;

namespace Tensorroot.Gov.Modules.Financas.Application.Planejamento.Compatibilidade;

/// <summary>
/// Serviço de domínio (implementado em Application — acessa repositórios do próprio módulo) que
/// prova a cadeia de compatibilidade da LOA:
/// <list type="number">
/// <item>LOA ⊆ LDO ⊆ PPA — toda ação fixada existe/vigente no PPA e é priorizada na LDO (CF 167, I/§1º; CF 165 §2º);</item>
/// <item>Equilíbrio — Σ receita prevista ≥ Σ despesa fixada (Lei 4.320/64 art. 2º);</item>
/// <item>Exercício coerente — LOA == LDO, dentro do quadriênio do PPA.</item>
/// </list>
/// </summary>
public sealed class CompatibilidadeOrcamentariaService(IPpaRepository ppas, ILdoRepository ldos)
    : ICompatibilidadeOrcamentariaService
{
    /// <inheritdoc />
    public async Task<ResultadoCompatibilidade> VerificarAsync(LeiOrcamentariaAnual loa, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loa);

        var motivos = new List<string>();

        var ppa = await ppas.ObterPorIdAsync(loa.PpaId, cancellationToken).ConfigureAwait(false);
        var ldo = await ldos.ObterPorIdAsync(loa.LdoId, cancellationToken).ConfigureAwait(false);

        if (ppa is null)
        {
            motivos.Add("PPA vinculado a LOA nao encontrado.");
        }

        if (ldo is null)
        {
            motivos.Add("LDO vinculada a LOA nao encontrada.");
        }

        if (ppa is not null && !ppa.CobreExercicio(loa.Exercicio))
        {
            motivos.Add($"Exercicio {loa.Exercicio} da LOA fora do quadrienio do PPA [{ppa.AnoInicio}-{ppa.AnoFim}].");
        }

        if (ldo is not null && ldo.Exercicio != loa.Exercicio)
        {
            motivos.Add($"Exercicio da LOA ({loa.Exercicio}) difere do da LDO ({ldo.Exercicio}).");
        }

        if (ppa is not null && ldo is not null)
        {
            VerificarItens(loa, ppa, ldo, motivos);
        }

        VerificarEquilibrio(loa, motivos);

        return motivos.Count == 0 ? ResultadoCompatibilidade.Ok() : ResultadoCompatibilidade.Incompativel(motivos);
    }

    private static void VerificarItens(LeiOrcamentariaAnual loa, PlanoPlurianual ppa, LeiDiretrizes ldo, List<string> motivos)
    {
        foreach (var item in loa.Itens)
        {
            // Crédito especial cria ação sem priorização prévia da LDO original (fluxo do art. 42);
            // a checagem LOA⊆LDO⊆PPA se aplica à LOA original (itens não-especiais).
            if (item.OrigemCreditoEspecial)
            {
                continue;
            }

            if (!ppa.ContemAcaoVigente(item.AcaoPpaId))
            {
                motivos.Add($"Item {item.Id}: acao {item.AcaoPpaId} nao existe/vigente no PPA (CF 167, I).");
                continue;
            }

            if (!ldo.ContemPrioridade(item.AcaoPpaId))
            {
                motivos.Add($"Item {item.Id}: acao {item.AcaoPpaId} nao priorizada na LDO do exercicio (CF 165 §2º).");
            }
        }
    }

    private static void VerificarEquilibrio(LeiOrcamentariaAnual loa, List<string> motivos)
    {
        // Equilibrio orcamentario (Lei 4.320/64 art. 2º): receita prevista >= despesa fixada.
        if (loa.TotalDespesaFixada.EhMaiorQue(loa.TotalReceitaPrevista))
        {
            motivos.Add(
                $"Desequilibrio: despesa fixada ({loa.TotalDespesaFixada}) maior que receita prevista ({loa.TotalReceitaPrevista}).");
        }
    }
}
