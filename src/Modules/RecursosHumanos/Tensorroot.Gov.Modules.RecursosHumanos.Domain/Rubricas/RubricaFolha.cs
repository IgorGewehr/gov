using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Rubricas;

/// <summary>Identificador forte do agregado <see cref="RubricaFolha"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct RubricaFolhaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="RubricaFolhaId"/>.</returns>
    public static RubricaFolhaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Rubrica (verba) parametrizavel da folha, configuravel por tenant e vigente por competencia
/// (tabela eSocial S-1010). Define a natureza (provento/desconto/informativa), a formula ou valor
/// fixo e — o coracao do motor — as INCIDENCIAS (integra base de INSS/RPPS/IRRF/FGTS). As bases sao
/// somadas POR INCIDENCIA, nunca por nome de rubrica (pesquisa-folha-calculo §4). Raiz de agregado.
/// </summary>
public sealed class RubricaFolha : AggregateRoot<RubricaFolhaId>, IMustHaveTenant
{
    /// <summary>Comprimento maximo do codigo da rubrica (S-1010).</summary>
    public const int CodigoComprimentoMaximo = 30;

    private RubricaFolha()
    {
    }

    private RubricaFolha(
        RubricaFolhaId id,
        Guid tenantId,
        Rubrica codigo,
        string descricao,
        NaturezaRubrica natureza,
        bool incideInss,
        bool incideRpps,
        bool incideIrrf,
        bool incideFgts,
        decimal? valorFixo,
        decimal? percentual,
        Competencia vigenciaInicio)
        : base(id)
    {
        TenantId = tenantId;
        Codigo = codigo;
        Descricao = descricao;
        Natureza = natureza;
        IncideInss = incideInss;
        IncideRpps = incideRpps;
        IncideIrrf = incideIrrf;
        IncideFgts = incideFgts;
        ValorFixo = valorFixo;
        Percentual = percentual;
        VigenciaInicio = vigenciaInicio;
        Ativa = true;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Codigo da rubrica (referencia a S-1010), unico por tenant na vigencia.</summary>
    public Rubrica Codigo { get; private set; } = default!;

    /// <summary>Descricao legivel da verba.</summary>
    public string Descricao { get; private set; } = default!;

    /// <summary>Natureza (provento/desconto/informativa — eSocial tpRubr).</summary>
    public NaturezaRubrica Natureza { get; private set; }

    /// <summary>Indica se a rubrica integra a base de INSS (RGPS).</summary>
    public bool IncideInss { get; private set; }

    /// <summary>Indica se a rubrica integra a base de RPPS.</summary>
    public bool IncideRpps { get; private set; }

    /// <summary>Indica se a rubrica integra a base do IRRF.</summary>
    public bool IncideIrrf { get; private set; }

    /// <summary>Indica se a rubrica integra a base do FGTS.</summary>
    public bool IncideFgts { get; private set; }

    /// <summary>Valor fixo da verba (quando configurada por valor; mutuamente exclusivo com <see cref="Percentual"/>).</summary>
    public decimal? ValorFixo { get; private set; }

    /// <summary>Percentual sobre a base (fracao decimal) quando configurada por formula simples.</summary>
    public decimal? Percentual { get; private set; }

    /// <summary>Competencia inicial de vigencia (AAAA-MM).</summary>
    public Competencia VigenciaInicio { get; private set; } = default!;

    /// <summary>Indica se a rubrica esta ativa (false = desativada para novas folhas).</summary>
    public bool Ativa { get; private set; }

    /// <summary>Indica se a verba reduz o liquido (desconto/informativa dedutora).</summary>
    public bool EhDesconto => Natureza is NaturezaRubrica.Desconto or NaturezaRubrica.InformativaDedutora;

    /// <summary>Indica se a verba aumenta o liquido (provento).</summary>
    public bool EhProvento => Natureza == NaturezaRubrica.Provento;

    /// <summary>
    /// Cria uma rubrica parametrizavel para o tenant, vigente a partir de uma competencia.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="codigo">Codigo da rubrica (S-1010).</param>
    /// <param name="descricao">Descricao legivel.</param>
    /// <param name="natureza">Natureza (provento/desconto/informativa).</param>
    /// <param name="vigenciaInicio">Competencia inicial de vigencia.</param>
    /// <param name="incideInss">Integra base de INSS.</param>
    /// <param name="incideRpps">Integra base de RPPS.</param>
    /// <param name="incideIrrf">Integra base do IRRF.</param>
    /// <param name="incideFgts">Integra base do FGTS.</param>
    /// <param name="valorFixo">Valor fixo (opcional; exclusivo com percentual).</param>
    /// <param name="percentual">Percentual sobre a base (opcional; exclusivo com valor fixo).</param>
    /// <returns>Nova <see cref="RubricaFolha"/> ativa.</returns>
    /// <exception cref="ArgumentException">Se descricao for vazia ou valor/percentual conflitarem.</exception>
    /// <exception cref="ArgumentNullException">Se codigo/competencia forem nulos.</exception>
    public static RubricaFolha Criar(
        Guid tenantId,
        Rubrica codigo,
        string descricao,
        NaturezaRubrica natureza,
        Competencia vigenciaInicio,
        bool incideInss = false,
        bool incideRpps = false,
        bool incideIrrf = false,
        bool incideFgts = false,
        decimal? valorFixo = null,
        decimal? percentual = null)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        ArgumentNullException.ThrowIfNull(vigenciaInicio);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        if (valorFixo is not null && percentual is not null)
        {
            throw new ArgumentException("Rubrica nao pode ter valor fixo e percentual simultaneamente.", nameof(valorFixo));
        }

        if (valorFixo is { } vf)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vf);
        }

        if (percentual is { } pc)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pc);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(pc, 1m);
        }

        // Rubrica informativa nao deve ter incidencia previdenciaria/IRRF positiva
        // (alinhado a verificacao-esocial §3.1: informativa => codIncCP=00, codIncIRRF=9).
        if (natureza is NaturezaRubrica.Informativa or NaturezaRubrica.InformativaDedutora
            && (incideInss || incideRpps || incideIrrf || incideFgts))
        {
            throw new ArgumentException("Rubrica informativa nao integra base de INSS/RPPS/IRRF/FGTS.", nameof(natureza));
        }

        return new RubricaFolha(
            RubricaFolhaId.New(),
            tenantId,
            codigo,
            descricao.Trim(),
            natureza,
            incideInss,
            incideRpps,
            incideIrrf,
            incideFgts,
            valorFixo,
            percentual,
            vigenciaInicio);
    }

    /// <summary>Reconfigura as incidencias da rubrica (ex.: ajuste apos mapeamento S-1010 oficial).</summary>
    /// <param name="incideInss">Integra base de INSS.</param>
    /// <param name="incideRpps">Integra base de RPPS.</param>
    /// <param name="incideIrrf">Integra base do IRRF.</param>
    /// <param name="incideFgts">Integra base do FGTS.</param>
    /// <exception cref="InvalidOperationException">Se a rubrica for informativa (sem incidencia).</exception>
    public void AjustarIncidencias(bool incideInss, bool incideRpps, bool incideIrrf, bool incideFgts)
    {
        if (Natureza is NaturezaRubrica.Informativa or NaturezaRubrica.InformativaDedutora
            && (incideInss || incideRpps || incideIrrf || incideFgts))
        {
            throw new InvalidOperationException("Rubrica informativa nao integra base de INSS/RPPS/IRRF/FGTS.");
        }

        IncideInss = incideInss;
        IncideRpps = incideRpps;
        IncideIrrf = incideIrrf;
        IncideFgts = incideFgts;
    }

    /// <summary>Desativa a rubrica para novas folhas (mantem o historico).</summary>
    public void Desativar() => Ativa = false;
}
