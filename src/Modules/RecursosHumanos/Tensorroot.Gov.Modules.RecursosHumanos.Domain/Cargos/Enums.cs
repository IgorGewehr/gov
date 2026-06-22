namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;

/// <summary>Tipo (natureza jurídica do provimento) de um cargo público.</summary>
public enum TipoCargo
{
    /// <summary>Provido por concurso público (CF art. 37, II); regime RPPS; estabilidade após 3 anos.</summary>
    Efetivo = 1,

    /// <summary>Livre nomeação e exoneração; regime RGPS.</summary>
    Comissionado = 2,

    /// <summary>Contratação por tempo determinado; regime RGPS.</summary>
    Temporario = 3,
}

/// <summary>Regime previdenciário associado ao cargo (EC 103/2019). Derivado do <see cref="TipoCargo"/>.</summary>
public enum RegimePrevidenciario
{
    /// <summary>Regime Próprio de Previdência Social — cargo efetivo.</summary>
    Rpps = 1,

    /// <summary>Regime Geral de Previdência Social — cargo comissionado/temporário.</summary>
    Rgps = 2,
}

/// <summary>Situação (estado) atual do cargo na estrutura de pessoal.</summary>
public enum SituacaoCargo
{
    /// <summary>Cargo ativo (com ao menos uma vaga ocupável; estado inicial).</summary>
    Ativo = 1,

    /// <summary>Cargo ativo, porém sem ocupante provido.</summary>
    Vago = 2,

    /// <summary>Cargo extinto por lei (estado terminal).</summary>
    Extinto = 3,
}
