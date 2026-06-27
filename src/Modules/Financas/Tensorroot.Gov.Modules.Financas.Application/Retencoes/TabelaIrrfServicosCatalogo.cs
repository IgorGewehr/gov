using Tensorroot.Gov.Modules.Financas.Domain.Retencoes;

namespace Tensorroot.Gov.Modules.Financas.Application.Retencoes;

/// <summary>
/// Catálogo do seed da tabela de IRRF sobre pagamentos a PJ — IN RFB 1.234/2012, Anexo I (Tabela 1),
/// alterada pela IN RFB 2.145/2023 (estende a retenção a Estados e Municípios). Alíquotas e códigos de
/// receita (DARF) conforme a fonte oficial. Dispensa de retenção abaixo de R$ 10,00 por DARF
/// (art. 3º, §6º). Parametrizável por tenant — este é o default semeado na ativação do módulo.
/// </summary>
public static class TabelaIrrfServicosCatalogo
{
    /// <summary>Valor mínimo a reter por DARF (dispensa abaixo) — IN RFB 1.234/2012, art. 3º, §6º.</summary>
    public const decimal ValorMinimoRetencaoPadrao = 10.00m;

    /// <summary>Início de vigência do default (IN 2.145/2023 — extensão a Estados/Municípios).</summary>
    public static readonly DateOnly VigenciaInicioPadrao = new(2024, 1, 1);

    /// <summary>Faixas (enquadramentos) do Anexo I da IN RFB 1.234/2012.</summary>
    /// <returns>Faixas com alíquota e código de receita DARF.</returns>
    public static IReadOnlyList<FaixaIrrfServicos> Faixas() =>
    [
        // 1,2% — cod. 6147: mercadorias/bens em geral, alimentacao, energia, transporte de cargas,
        // construcao civil e servicos com emprego de materiais, servicos hospitalares (art. 30/31).
        FaixaIrrfServicos.De("MERCADORIAS_BENS", "Mercadorias e bens em geral", 0.012m, "6147"),
        FaixaIrrfServicos.De("ALIMENTACAO", "Alimentacao", 0.012m, "6147"),
        FaixaIrrfServicos.De("ENERGIA", "Energia eletrica", 0.012m, "6147"),
        FaixaIrrfServicos.De("SERVICOS_COM_MATERIAIS", "Servicos prestados com emprego de materiais", 0.012m, "6147"),
        FaixaIrrfServicos.De("CONSTRUCAO_COM_MATERIAIS", "Construcao civil por empreitada com emprego de materiais", 0.012m, "6147"),
        FaixaIrrfServicos.De("SERVICOS_HOSPITALARES", "Servicos hospitalares e de auxilio diagnostico/terapia (art. 30/31)", 0.012m, "6147"),
        FaixaIrrfServicos.De("TRANSPORTE_CARGAS", "Transporte de cargas", 0.012m, "6147"),
        FaixaIrrfServicos.De("PRODUTOS_FARMACEUTICOS", "Produtos farmaceuticos, de perfumaria, toucador ou higiene", 0.012m, "6147"),
        FaixaIrrfServicos.De("COMBUSTIVEIS", "Gasolina, oleo diesel, GLP e demais derivados de petroleo", 0.012m, "6147"),

        // 0,24% — cod. 9060: alcool etilico/biodiesel adquiridos de produtor.
        FaixaIrrfServicos.De("ALCOOL_BIODIESEL", "Alcool etilico hidratado (de produtor) e biodiesel", 0.0024m, "9060"),

        // 2,4% — cod. 6175: passagens/transporte de passageiros.
        FaixaIrrfServicos.De("TRANSPORTE_PASSAGEIROS", "Passagens e transporte de passageiros (inclui tarifa de embarque)", 0.024m, "6175"),

        // 2,4% — cod. 6188: servicos financeiros (bancos, seguros, capitalizacao, previdencia complementar) e seguro saude.
        FaixaIrrfServicos.De("SERVICOS_FINANCEIROS", "Servicos financeiros, seguros, capitalizacao e seguro saude", 0.024m, "6188"),

        // 4,8% — cod. 6190: demais servicos (vigilancia, limpeza, locacao de mao de obra, intermediacao,
        // administracao/locacao de bens, factoring, planos de saude, abastecimento de agua, telefone, correio).
        FaixaIrrfServicos.De("DEMAIS_SERVICOS", "Demais servicos (vigilancia, limpeza, mao de obra, intermediacao, agua, telefone)", 0.048m, "6190"),
    ];
}
