namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Afastamentos;

/// <summary>
/// Tipos legais de afastamento/licenca do servidor publico, cada um com EFEITO DETERMINISTICO na folha
/// (suspende/reduz proventos do ente, conta ou nao tempo de servico) definido pela tabela de
/// <see cref="RegraAfastamento"/> parametrizada por tenant/vigencia (nunca <em>hardcoded</em> — CLAUDE.md S7).
/// As referencias legais sao documentais; os percentuais/prazos efetivos vem da regra do tenant.
/// </summary>
public enum TipoAfastamento
{
    /// <summary>Licenca-maternidade (CF art. 7 XVIII; 120d, ou 180d se o ente aderiu ao Empresa Cidada/lei municipal). Ente paga integral; reembolso INSS no RGPS.</summary>
    LicencaMaternidade = 1,

    /// <summary>Licenca-paternidade (CF art. 7 XIX; 5d, ou 20d em programa de adesao). Ente paga integral.</summary>
    LicencaPaternidade = 2,

    /// <summary>Afastamento por doenca ate 15 dias (RGPS — primeiros 15d pagos pelo ente/empregador).</summary>
    DoencaAte15Dias = 3,

    /// <summary>Auxilio-doenca / afastamento por doenca acima de 15 dias (RGPS): beneficio pago pelo INSS; SUSPENDE o provento do ente do 16o dia. No RPPS pode manter por lei.</summary>
    DoencaInss = 4,

    /// <summary>Acidente de trabalho: analogo a doenca, com estabilidade acidentaria; ente 15d, depois INSS/RPPS.</summary>
    AcidenteTrabalho = 5,

    /// <summary>Licenca-premio por assiduidade (se prevista em lei municipal; ~3 meses por quinquenio). Ente paga integral.</summary>
    LicencaPremio = 6,

    /// <summary>Licenca para tratar de interesse particular (sem vencimento): SUSPENDE 100% dos proventos e NAO conta tempo.</summary>
    LicencaSemVencimento = 7,

    /// <summary>Cessao COM onus para o cedente: o ente de origem continua pagando.</summary>
    CessaoComOnus = 8,

    /// <summary>Cessao SEM onus para o cedente: o ente de origem SUSPENDE o provento (paga o cessionario).</summary>
    CessaoSemOnus = 9,

    /// <summary>Afastamento para exercicio de mandato eletivo (CF art. 38): conforme opcao remuneratoria.</summary>
    MandatoEletivo = 10,
}

/// <summary>Situacao do agregado <see cref="Afastamento"/> no seu ciclo de vida.</summary>
public enum SituacaoAfastamento
{
    /// <summary>Afastamento em curso (com ou sem fim previsto).</summary>
    Vigente = 1,

    /// <summary>Afastamento encerrado (retorno do servidor); fim efetivo gravado.</summary>
    Encerrado = 2,

    /// <summary>Afastamento cancelado (lancado por engano/revogado); sem efeito na folha.</summary>
    Cancelado = 3,
}
