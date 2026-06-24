namespace Tensorroot.Gov.Modules.Legislativo.Domain.LimiteCamara;

/// <summary>
/// Semaforo de aderencia de um limite/subteto do art. 29-A da CF/88. Padroniza o sinal visual do
/// demonstrativo (verde/amarelo/vermelho) a partir do percentual de utilizacao do teto.
/// </summary>
public enum SemaforoLimite
{
    /// <summary>Dentro do limite com folga (abaixo da faixa de atencao). Verde.</summary>
    Adequado = 1,

    /// <summary>Proximo do limite (faixa de atencao parametrizavel). Amarelo.</summary>
    Atencao = 2,

    /// <summary>Limite excedido (estouro do teto — crime de responsabilidade, art. 29-A §2). Vermelho.</summary>
    Excedido = 3,
}

/// <summary>
/// Natureza da despesa total da Camara apurada no art. 29-A. Discrimina o que entra no teto para que a
/// regra temporal da EC 109/2021 (inativos/pensionistas no teto a partir da legislatura de 2025) seja
/// aplicada de forma auditavel, sem numero magico.
/// </summary>
public enum NaturezaDespesaCamara
{
    /// <summary>Folha de pessoal ativo (vereadores + servidores da Camara) e respectivos encargos.</summary>
    PessoalAtivo = 1,

    /// <summary>Inativos e pensionistas custeados pela Camara (entram no teto so a partir de 2025 — EC 109/2021).</summary>
    InativosPensionistas = 2,

    /// <summary>Demais despesas de custeio e capital da Camara (material, servicos, investimentos).</summary>
    OutrasDespesas = 3,
}
