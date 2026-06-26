using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Dispensas;

// Particao do agregado DispensaEletronica: logica interna de classificacao/julgamento dos lances
// (ranking por proposta global, melhor lance vigente por item, desempate por sequencia) e guarda de
// ciclo de vida. Sao detalhes privados de implementacao do julgamento, separados da maquina de estados
// publica em DispensaEletronica.cs (manutenibilidade — sem alterar comportamento nem a API publica).
public sealed partial class DispensaEletronica
{
    // Melhor lance vigente (apos os sucessivos) de um fornecedor para um item, pelo criterio.
    private CotacaoDispensa? MelhorLanceVigenteDe(Guid fornecedorId, ItemDispensaId itemId)
    {
        var lancesDoFornecedor = _cotacoes
            .Where(cotacao => cotacao.FornecedorId == fornecedorId && cotacao.ItemId == itemId && cotacao.Situacao != SituacaoCotacao.Desclassificada)
            .ToList();

        if (lancesDoFornecedor.Count == 0)
        {
            return null;
        }

        return CriterioJulgamento == CriterioJulgamentoDispensa.MenorPreco
            ? lancesDoFornecedor.OrderBy(cotacao => cotacao.Valor.Valor).ThenBy(cotacao => cotacao.Sequencia).First()
            : lancesDoFornecedor.OrderByDescending(cotacao => cotacao.Valor.Valor).ThenBy(cotacao => cotacao.Sequencia).First();
    }

    // Lance novo melhora a oferta vigente conforme o criterio (estritamente).
    private bool LanceMelhora(ValorMonetario novo, ValorMonetario vigente)
        => CriterioJulgamento == CriterioJulgamentoDispensa.MenorPreco
            ? novo.MenorQue(vigente)
            : novo.MaiorQue(vigente);

    // Cotacao vencedora = melhor lance vigente do fornecedor de MENOR total agregado (proposta global).
    private CotacaoDispensa? MelhorCotacaoGlobal()
    {
        var fornecedores = _cotacoes
            .Where(cotacao => cotacao.Situacao != SituacaoCotacao.Desclassificada)
            .Select(cotacao => cotacao.FornecedorId)
            .Distinct()
            .ToList();

        if (fornecedores.Count == 0)
        {
            return null;
        }

        // Para cada fornecedor, total agregado dos melhores lances vigentes por item; ordena pelo criterio.
        var ranking = fornecedores
            .Select(fornecedorId => new
            {
                FornecedorId = fornecedorId,
                Total = TotalAgregadoDoFornecedor(fornecedorId).Valor,
                MelhorSequencia = MelhoresLancesDoFornecedor(fornecedorId).Min(cotacao => cotacao.Sequencia),
            })
            .ToList();

        var vencedor = CriterioJulgamento == CriterioJulgamentoDispensa.MenorPreco
            ? ranking.OrderBy(r => r.Total).ThenBy(r => r.MelhorSequencia).First()
            : ranking.OrderByDescending(r => r.Total).ThenBy(r => r.MelhorSequencia).First();

        // Cotacao representativa do vencedor (o melhor lance de menor sequencia) — ancora a marcacao de vencedora.
        return MelhoresLancesDoFornecedor(vencedor.FornecedorId)
            .OrderBy(cotacao => cotacao.Sequencia)
            .First();
    }

    private List<CotacaoDispensa> MelhoresLancesDoFornecedor(Guid fornecedorId)
        => _itens
            .Select(item => MelhorLanceVigenteDe(fornecedorId, item.Id))
            .Where(cotacao => cotacao is not null)
            .Select(cotacao => cotacao!)
            .ToList();

    private ValorMonetario TotalAgregadoDoFornecedor(Guid fornecedorId)
        => MelhoresLancesDoFornecedor(fornecedorId)
            .Aggregate(ValorMonetario.Zero, (acumulado, cotacao) => acumulado.Somar(cotacao.Valor));

    private void GarantirNaoEncerrada()
    {
        if (Situacao is SituacaoDispensa.Homologada
            or SituacaoDispensa.Fracassada
            or SituacaoDispensa.Deserta
            or SituacaoDispensa.Revogada
            or SituacaoDispensa.Anulada)
        {
            throw new InvalidOperationException($"Dispensa encerrada nao admite novas transicoes. Situacao atual: {Situacao}.");
        }
    }
}
