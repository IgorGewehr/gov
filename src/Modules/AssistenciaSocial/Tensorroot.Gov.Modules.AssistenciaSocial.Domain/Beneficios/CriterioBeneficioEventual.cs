using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

/// <summary>
/// <b>A-0 — criterio de elegibilidade do BENEFICIO EVENTUAL, 100% definido por LEI/DECRETO MUNICIPAL
/// + CMAS</b> (LOAS art. 22, redacao da Lei 12.435/2011; Decreto 6.307/2007). NAO existe teto federal
/// de "1/4 do salario minimo" (esse limite — antiga referencia da LOAS — foi removido pela Lei
/// 12.435/2011 e NUNCA deve ser embutido como default). Aqui o municipio informa, por tenant+vigencia:
/// <list type="bullet">
///   <item>a <b>modalidade</b> habilitada (natalidade/morte/vulnerabilidade temporaria/calamidade);</item>
///   <item>o <b>criterio de renda</b> — expresso como multiplo do salario minimo vigente (parametro),
///   ou <c>null</c> para indicar que a lei municipal NAO impoe corte de renda para a modalidade.</item>
/// </list>
/// O salario minimo continua sendo um parametro versionado informado pelo chamador (nunca hardcoded —
/// CLAUDE.md §16). O proprio multiplo de renda vem da lei municipal — pode ser 1/2, 1/4, 1 SM, etc.
/// // TODO(validar-oficial): a tabela de modalidades/valores/criterios de renda do tenant piloto
/// (Maximiliano de Almeida/RS) deve ser carregada da lei/decreto municipal de beneficios eventuais.
/// </summary>
public sealed record CriterioBeneficioEventual
{
    private CriterioBeneficioEventual(
        ModalidadeBeneficioEventual modalidade,
        decimal? multiploRendaSalarioMinimo)
    {
        Modalidade = modalidade;
        MultiploRendaSalarioMinimo = multiploRendaSalarioMinimo;
    }

    /// <summary>Modalidade do beneficio eventual habilitada pela lei municipal.</summary>
    public ModalidadeBeneficioEventual Modalidade { get; }

    /// <summary>
    /// Limite de renda per capita, em multiplos do salario minimo vigente, definido pela lei municipal;
    /// <c>null</c> quando a lei municipal NAO impoe corte de renda para a modalidade (ex.: morte/calamidade).
    /// </summary>
    public decimal? MultiploRendaSalarioMinimo { get; }

    /// <summary>
    /// Constroi o criterio municipal vigente. O municipio define a modalidade e, opcionalmente, o
    /// multiplo de renda (em SM). NAO ha teto federal embutido: ausencia de multiplo significa "sem
    /// corte de renda por lei municipal" — nunca o revogado 1/4 SM.
    /// </summary>
    /// <param name="modalidade">Modalidade habilitada pela lei municipal.</param>
    /// <param name="multiploRendaSalarioMinimo">Multiplo de SM do corte de renda municipal, ou <c>null</c> (sem corte).</param>
    /// <returns>Criterio municipal de beneficio eventual.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o multiplo for informado e nao for positivo.</exception>
    public static CriterioBeneficioEventual MunicipalVigente(
        ModalidadeBeneficioEventual modalidade,
        decimal? multiploRendaSalarioMinimo)
    {
        if (multiploRendaSalarioMinimo is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiploRendaSalarioMinimo.Value);
        }

        return new CriterioBeneficioEventual(modalidade, multiploRendaSalarioMinimo);
    }

    /// <summary>
    /// Avalia a elegibilidade ao beneficio eventual conforme a LEI MUNICIPAL. Quando a lei municipal
    /// fixa um corte de renda (multiplo de SM), a renda per capita deve ser &lt;= ao limite calculado a
    /// partir do salario minimo vigente; quando nao fixa, a renda NAO e barreira (a modalidade decorre
    /// do fato gerador — ex.: natalidade/morte). Sem teto federal de 1/4 SM (revogado).
    /// </summary>
    /// <param name="rendaPerCapita">Renda per capita apurada da familia.</param>
    /// <param name="salarioMinimoVigente">Salario minimo vigente na competencia (parametro versionado).</param>
    /// <returns>Resultado da avaliacao (elegivel ou nao, com motivo).</returns>
    /// <exception cref="ArgumentNullException">Se a renda ou o salario minimo forem nulos.</exception>
    public ResultadoElegibilidade Avaliar(RendaPerCapita rendaPerCapita, ValorMonetario salarioMinimoVigente)
    {
        ArgumentNullException.ThrowIfNull(rendaPerCapita);
        ArgumentNullException.ThrowIfNull(salarioMinimoVigente);

        // Sem corte de renda na lei municipal: a modalidade decorre do fato gerador (LOAS art. 22).
        if (MultiploRendaSalarioMinimo is null)
        {
            return ResultadoElegibilidade.Aprovar();
        }

        var limite = salarioMinimoVigente.Valor * MultiploRendaSalarioMinimo.Value;
        return rendaPerCapita.Valor <= limite
            ? ResultadoElegibilidade.Aprovar()
            : ResultadoElegibilidade.Negar(
                $"Renda per capita acima do limite definido pela lei municipal de beneficios eventuais ({MultiploRendaSalarioMinimo.Value:0.####} SM) para a modalidade {Modalidade}.");
    }
}
