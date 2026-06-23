using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Cargos;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>
/// Pensao alimenticia judicial ATIVA de um servidor, projetada para o calculo da folha sem acoplar a
/// Application ao agregado <see cref="Servidor"/>. Espelha a modalidade (percentual sobre base ou valor
/// fixo) definida na decisao. Imutavel.
/// </summary>
/// <param name="Beneficiario">Nome do alimentando (repasse).</param>
/// <param name="Modalidade">Percentual sobre base ou valor fixo.</param>
/// <param name="Percentual">Fracao (0..1) quando percentual; zero no valor fixo.</param>
/// <param name="BaseIncidencia">Base do percentual (proventos brutos ou liquido apos descontos legais).</param>
/// <param name="ValorFixo">Valor fixo mensal quando aplicavel; zero no percentual.</param>
public sealed record PensaoAlimenticiaCalculo(
    string Beneficiario,
    ModalidadePensao Modalidade,
    decimal Percentual,
    BasePensao BaseIncidencia,
    decimal ValorFixo)
{
    /// <summary>
    /// Apura o valor mensal desta pensao a partir das bases ja calculadas (reproduz a regra do dominio).
    /// </summary>
    /// <param name="proventosBrutos">Total bruto de proventos do servidor na competencia.</param>
    /// <param name="descontosLegais">INSS/RPPS/IRRF apurados na competencia.</param>
    /// <returns>Valor mensal da pensao (2 casas), nao-negativo.</returns>
    public decimal Apurar(decimal proventosBrutos, decimal descontosLegais)
    {
        if (Modalidade == ModalidadePensao.ValorFixo)
        {
            return decimal.Round(ValorFixo, 2, MidpointRounding.AwayFromZero);
        }

        var baseCalculo = BaseIncidencia == BasePensao.ProventosBrutos
            ? proventosBrutos
            : proventosBrutos - descontosLegais;
        return baseCalculo <= 0m ? 0m : decimal.Round(baseCalculo * Percentual, 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// Dados do servidor relevantes para o motor de calculo: regime previdenciario, quantidade de
/// dependentes (deducao de IRRF) e as pensoes alimenticias ativas (deducao do IRRF + desconto/repasse).
/// Imutavel.
/// </summary>
/// <param name="Regime">Regime previdenciario (RPPS para efetivo; RGPS para os demais).</param>
/// <param name="QuantidadeDependentes">Numero de dependentes para deducao de IRRF.</param>
/// <param name="PensoesAlimenticias">Pensoes alimenticias judiciais ativas do servidor (pode haver mais de uma).</param>
public sealed record DadosCalculoServidor(
    RegimePrevidenciario Regime,
    int QuantidadeDependentes,
    IReadOnlyList<PensaoAlimenticiaCalculo> PensoesAlimenticias)
{
    /// <summary>Cria os dados de calculo sem pensoes (compatibilidade).</summary>
    /// <param name="regime">Regime previdenciario.</param>
    /// <param name="quantidadeDependentes">Dependentes para deducao de IRRF.</param>
    public DadosCalculoServidor(RegimePrevidenciario regime, int quantidadeDependentes)
        : this(regime, quantidadeDependentes, [])
    {
    }
}

/// <summary>
/// Consulta o regime previdenciario vigente de um servidor (I-4: efetivo -> RPPS / S-1202;
/// temporario/comissionado/celetista -> RGPS / S-1200). Abstrai o agregado Servidor (mesmo modulo)
/// para o lancamento de eventos de folha, sem acoplar o handler ao seu repositorio concreto.
/// </summary>
public interface IServidorRegimeConsulta
{
    /// <summary>Obtem o regime previdenciario do servidor no tenant atual.</summary>
    /// <param name="servidorId">Identificador do servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O regime previdenciario, ou <c>null</c> se o servidor nao existir no tenant.</returns>
    Task<RegimePrevidenciario?> ObterRegimeAsync(Guid servidorId, CancellationToken cancellationToken);

    /// <summary>Obtem regime e quantidade de dependentes do servidor (para o motor de calculo).</summary>
    /// <param name="servidorId">Identificador do servidor.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados de calculo, ou <c>null</c> se o servidor nao existir no tenant.</returns>
    Task<DadosCalculoServidor?> ObterDadosCalculoAsync(Guid servidorId, CancellationToken cancellationToken);
}
