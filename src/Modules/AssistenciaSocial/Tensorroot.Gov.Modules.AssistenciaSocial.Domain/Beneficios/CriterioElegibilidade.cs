using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;

/// <summary>Resultado de uma avaliacao de elegibilidade.</summary>
/// <param name="Elegivel">Indica se o requerente e elegivel.</param>
/// <param name="Motivo">Motivo do indeferimento, quando nao elegivel (vazio quando elegivel).</param>
public readonly record struct ResultadoElegibilidade(bool Elegivel, string Motivo)
{
    /// <summary>Cria um resultado elegivel.</summary>
    /// <returns>Resultado elegivel, sem motivo de indeferimento.</returns>
    public static ResultadoElegibilidade Aprovar() => new(true, string.Empty);

    /// <summary>Cria um resultado nao elegivel com o motivo fundamentado.</summary>
    /// <param name="motivo">Motivo do indeferimento.</param>
    /// <returns>Resultado nao elegivel.</returns>
    public static ResultadoElegibilidade Negar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        return new ResultadoElegibilidade(false, motivo);
    }
}

/// <summary>
/// Encapsula os limiares vigentes na competencia (renda, idade, vedacoes) e decide a elegibilidade
/// dos beneficios de criterio FEDERAL: BPC (LOAS art. 20 — renda &lt; 1/4 SM, criterio legal vigente)
/// e PBF (baixa renda). Nunca hardcoded quanto ao valor: recebe o salario minimo vigente da competencia
/// (Beneficio I-1). O salario minimo e parametrizado por vigencia e informado pelo chamador (CLAUDE.md
/// secao 7).
/// <para>
/// <b>A-0 (risco juridico):</b> o BENEFICIO EVENTUAL NAO e avaliado aqui — seu criterio e 100% de LEI
/// MUNICIPAL (<see cref="CriterioBeneficioEventual"/>), sem teto federal de "1/4 SM" (revogado pela Lei
/// 12.435/2011). Encaminhar eventual por aqui lanca, para impedir reintroducao do teto revogado.
/// </para>
/// </summary>
public sealed record CriterioElegibilidade
{
    /// <summary>Idade minima do idoso para o BPC (anos) — Beneficio I-2.</summary>
    public const int IdadeMinimaBpcIdoso = 65;

    private CriterioElegibilidade(TipoBeneficio tipo, ValorMonetario salarioMinimoVigente)
    {
        Tipo = tipo;
        SalarioMinimoVigente = salarioMinimoVigente;
    }

    /// <summary>Tipo do beneficio avaliado.</summary>
    public TipoBeneficio Tipo { get; }

    /// <summary>Salario minimo vigente na competencia (parametro versionado, nunca hardcoded).</summary>
    public ValorMonetario SalarioMinimoVigente { get; }

    /// <summary>Constroi o criterio vigente para o tipo e a competencia.</summary>
    /// <param name="tipo">Tipo do beneficio.</param>
    /// <param name="salarioMinimoVigente">Salario minimo vigente na competencia (nao hardcoded).</param>
    /// <returns>Criterio de elegibilidade vigente.</returns>
    /// <exception cref="ArgumentNullException">Se o salario minimo for nulo (I-1/B-11).</exception>
    public static CriterioElegibilidade Vigente(TipoBeneficio tipo, ValorMonetario salarioMinimoVigente)
    {
        ArgumentNullException.ThrowIfNull(salarioMinimoVigente);
        return new CriterioElegibilidade(tipo, salarioMinimoVigente);
    }

    /// <summary>Avalia a elegibilidade do requerente conforme o criterio vigente.</summary>
    /// <param name="dados">Contexto fatico do requerente.</param>
    /// <param name="rendaPerCapita">Renda per capita apurada da familia (projetada do CadUnico/Familia).</param>
    /// <returns>Resultado da avaliacao (elegivel ou nao, com motivo).</returns>
    /// <exception cref="ArgumentNullException">Se a renda per capita for nula.</exception>
    public ResultadoElegibilidade Avaliar(DadosElegibilidade dados, RendaPerCapita rendaPerCapita)
    {
        ArgumentNullException.ThrowIfNull(rendaPerCapita);
        return Tipo switch
        {
            TipoBeneficio.Bpc => AvaliarBpc(dados, rendaPerCapita),
            TipoBeneficio.Pbf => AvaliarPbf(rendaPerCapita),
            // A-0: eventual jamais e decidido por criterio federal — usa CriterioBeneficioEventual (lei municipal).
            TipoBeneficio.Eventual => throw new InvalidOperationException(
                "Beneficio eventual e avaliado por CriterioBeneficioEventual (lei municipal), nunca por criterio federal (sem teto de 1/4 SM revogado)."),
            _ => ResultadoElegibilidade.Negar("Tipo de beneficio nao suportado."),
        };
    }

    private ResultadoElegibilidade AvaliarBpc(DadosElegibilidade dados, RendaPerCapita rendaPerCapita)
    {
        // I-3: BPC nao acumula com beneficio da Seguridade Social.
        if (dados.AcumulaSeguridadeSocial)
        {
            return ResultadoElegibilidade.Negar("BPC nao acumula com outro beneficio da Seguridade Social.");
        }

        // I-2: idoso >= 65 OU PCD com avaliacao biopsicossocial.
        var idoso = dados.Idade >= IdadeMinimaBpcIdoso;
        var pcdComprovado = dados.PossuiDeficiencia && dados.PossuiAvaliacaoBiopsicossocial;
        if (!idoso && !pcdComprovado)
        {
            return ResultadoElegibilidade.Negar("Nao atende ao criterio de idade (>= 65) nem de deficiencia comprovada (avaliacao biopsicossocial).");
        }

        // I-2/B-1: renda per capita estritamente < 1/4 do salario minimo vigente.
        if (!rendaPerCapita.EhAbaixoUmQuarto(SalarioMinimoVigente.Valor))
        {
            return ResultadoElegibilidade.Negar("Renda per capita igual ou superior a 1/4 do salario minimo vigente.");
        }

        return ResultadoElegibilidade.Aprovar();
    }

    private ResultadoElegibilidade AvaliarPbf(RendaPerCapita rendaPerCapita)
    {
        // PBF: criterio de baixa renda (proxy 1/2 SM nesta versao); vinculo/condicionalidades via SISC.
        return rendaPerCapita.EhAteMeioSalario(SalarioMinimoVigente.Valor)
            ? ResultadoElegibilidade.Aprovar()
            : ResultadoElegibilidade.Negar("Renda per capita acima do limiar de baixa renda do PBF.");
    }
}
