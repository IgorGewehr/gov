using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Seed;

/// <summary>
/// Definição declarativa de uma conta do seed PCASP (sem TenantId — resolvido na aplicação).
/// </summary>
/// <param name="Codigo">Código PCASP.</param>
/// <param name="Titulo">Título.</param>
/// <param name="Funcao">Função.</param>
/// <param name="Funcionamento">Funcionamento.</param>
/// <param name="Tipo">Tipo (Sintética/Analítica).</param>
/// <param name="Indicador">Indicador F/P.</param>
/// <param name="Encerramento">Se o saldo encerra no exercício.</param>
/// <param name="NaturezaSaldoOverride">Sobreposição de natureza de saldo (mista/redutora).</param>
public sealed record ContaSeed(
    string Codigo,
    string Titulo,
    string Funcao,
    string Funcionamento,
    TipoConta Tipo,
    IndicadorSuperavitFinanceiro Indicador = IndicadorSuperavitFinanceiro.NaoAplicavel,
    bool Encerramento = false,
    NaturezaSaldo? NaturezaSaldoOverride = null);
