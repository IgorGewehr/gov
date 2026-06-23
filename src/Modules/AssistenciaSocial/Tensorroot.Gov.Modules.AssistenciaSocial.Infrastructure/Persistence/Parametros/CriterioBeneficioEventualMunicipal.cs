using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Beneficios;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure.Persistence.Parametros;

/// <summary>
/// <b>A-0 — registro versionado (tenant+vigencia) do criterio MUNICIPAL de beneficio eventual</b>
/// (LOAS art. 22, Lei 12.435/2011; Decreto 6.307/2007). Por modalidade, guarda o corte de renda em
/// multiplos do salario minimo (<see cref="MultiploRendaSalarioMinimo"/>) OU <c>null</c> quando a lei
/// municipal NAO impoe corte. NAO ha teto federal de "1/4 SM" embutido — a ausencia de registro para a
/// modalidade significa "lei municipal nao habilitou", nunca um default federal. Mora na Infrastructure
/// (registro de configuracao, nao invariante de dominio); isolado por tenant via Global Query Filter.
/// // TODO(validar-oficial): carregar a tabela do tenant piloto (Maximiliano de Almeida/RS) da
/// lei/decreto municipal de beneficios eventuais + resolucao do CMAS.
/// </summary>
public sealed class CriterioBeneficioEventualMunicipal : IMustHaveTenant
{
    private CriterioBeneficioEventualMunicipal()
    {
    }

    private CriterioBeneficioEventualMunicipal(
        Guid id,
        Guid tenantId,
        ModalidadeBeneficioEventual modalidade,
        DateOnly vigenciaInicio,
        decimal? multiploRendaSalarioMinimo)
    {
        Id = id;
        TenantId = tenantId;
        Modalidade = modalidade;
        VigenciaInicio = vigenciaInicio;
        MultiploRendaSalarioMinimo = multiploRendaSalarioMinimo;
    }

    /// <summary>Identificador do registro.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant (municipio) dono do criterio.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Modalidade do beneficio eventual.</summary>
    public ModalidadeBeneficioEventual Modalidade { get; private set; }

    /// <summary>Inicio de vigencia (inclusive) do criterio municipal.</summary>
    public DateOnly VigenciaInicio { get; private set; }

    /// <summary>
    /// Corte de renda per capita em multiplos do salario minimo (lei municipal); <c>null</c> = sem corte.
    /// </summary>
    public decimal? MultiploRendaSalarioMinimo { get; private set; }

    /// <summary>Cria um criterio municipal versionado.</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="modalidade">Modalidade do beneficio eventual.</param>
    /// <param name="vigenciaInicio">Inicio de vigencia.</param>
    /// <param name="multiploRendaSalarioMinimo">Corte de renda em multiplos de SM, ou <c>null</c> (sem corte).</param>
    /// <returns>Novo <see cref="CriterioBeneficioEventualMunicipal"/>.</returns>
    public static CriterioBeneficioEventualMunicipal Criar(
        Guid tenantId,
        ModalidadeBeneficioEventual modalidade,
        DateOnly vigenciaInicio,
        decimal? multiploRendaSalarioMinimo)
    {
        if (multiploRendaSalarioMinimo is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiploRendaSalarioMinimo.Value);
        }

        return new CriterioBeneficioEventualMunicipal(Guid.NewGuid(), tenantId, modalidade, vigenciaInicio, multiploRendaSalarioMinimo);
    }

    /// <summary>Converte o registro persistido no VO de dominio.</summary>
    /// <returns>Criterio de dominio equivalente.</returns>
    public CriterioBeneficioEventual ParaDominio()
        => CriterioBeneficioEventual.MunicipalVigente(Modalidade, MultiploRendaSalarioMinimo);
}
