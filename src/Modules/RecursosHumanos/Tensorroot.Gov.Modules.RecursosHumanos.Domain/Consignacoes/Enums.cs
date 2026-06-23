namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Consignacoes;

/// <summary>
/// Natureza da entidade consignataria (banco/entidade habilitada a receber consignacao em folha).
/// Apenas classificacao cadastral; nao altera o efeito na folha (esse vem do <see cref="GrupoMargem"/>
/// da rubrica). Lei 14.131/2021 + Lei 8.112/90 art. 45 (consignacoes facultativas).
/// </summary>
public enum TipoConsignataria
{
    /// <summary>Instituicao financeira (banco/financeira) — emprestimos consignados, cartao consignado.</summary>
    InstituicaoFinanceira = 1,

    /// <summary>Entidade de classe/sindicato/associacao — mensalidades, contribuicoes facultativas.</summary>
    EntidadeClassista = 2,

    /// <summary>Seguradora — seguros e previdencia privada facultativa.</summary>
    Seguradora = 3,

    /// <summary>Outra entidade habilitada por norma do ente.</summary>
    Outra = 4,
}

/// <summary>Situacao da <see cref="Consignataria"/> no cadastro mestre do tenant.</summary>
public enum SituacaoConsignataria
{
    /// <summary>Ativa: pode receber novas averbacoes.</summary>
    Ativa = 1,

    /// <summary>Suspensa: bloqueada para novas averbacoes (contratos vigentes seguem ate quitacao/cancelamento).</summary>
    Suspensa = 2,
}

/// <summary>
/// Categoria da rubrica consignavel — define a PRIORIDADE de manutencao no corte por margem
/// (obrigatorias antes de facultativas antes de beneficio). Nao confundir com o
/// <see cref="GrupoMargem"/> (balde de limite). Design RH §2.1.b / §2.5.
/// </summary>
public enum CategoriaConsignavel
{
    /// <summary>Obrigatoria por lei/decisao (ex.: pensao por consignacao, contribuicao compulsoria) — mantida primeiro.</summary>
    Obrigatoria = 1,

    /// <summary>Facultativa (emprestimo consignado, mensalidade) — cortada antes das obrigatorias.</summary>
    Facultativa = 2,

    /// <summary>Beneficio (cartao beneficio, seguros) — primeira a ser cortada na insuficiencia de margem.</summary>
    Beneficio = 3,
}

/// <summary>
/// "Balde" de margem consignavel (reserva legal). Lei 14.131/2021: 35% geral + 5% cartao de credito
/// consignado + 5% cartao beneficio. Cada balde tem limite proprio e NAO invade os demais. Os percentuais
/// efetivos vem dos <see cref="MargemConsignavel"/> parametrizados por tenant/vigencia (nunca hardcoded).
/// </summary>
public enum GrupoMargem
{
    /// <summary>Margem GERAL (35% legal): emprestimos consignados, mensalidades, seguros, etc.</summary>
    Geral = 1,

    /// <summary>Reserva de CARTAO DE CREDITO consignado (5% legal).</summary>
    CartaoConsignado = 2,

    /// <summary>Reserva de CARTAO BENEFICIO (5% legal).</summary>
    CartaoBeneficio = 3,
}

/// <summary>Situacao do agregado <see cref="ContratoConsignacao"/> no seu ciclo de vida.</summary>
public enum SituacaoConsignacao
{
    /// <summary>Averbada: vigente e lancada na folha (consome margem).</summary>
    Averbada = 1,

    /// <summary>Suspensa: temporariamente sem lancamento na folha (libera margem; pode reativar).</summary>
    Suspensa = 2,

    /// <summary>Quitada: todas as parcelas pagas (libera margem; nao reativavel).</summary>
    Quitada = 3,

    /// <summary>Cancelada: encerrada antes da quitacao (libera margem; nao reativavel).</summary>
    Cancelada = 4,
}
