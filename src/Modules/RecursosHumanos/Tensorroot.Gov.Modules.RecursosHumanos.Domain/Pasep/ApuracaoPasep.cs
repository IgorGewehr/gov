using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Events;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Folha;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Pasep;

/// <summary>Identificador forte do agregado <see cref="ApuracaoPasep"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ApuracaoPasepId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ApuracaoPasepId"/>.</returns>
    public static ApuracaoPasepId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Estado (ciclo de vida) de uma apuracao do PASEP.</summary>
public enum SituacaoApuracaoPasep
{
    /// <summary>Base e valor apurados (estado inicial); pronta para transmissao (M10).</summary>
    Apurada = 1,

    /// <summary>Transmitida/recolhida ao agente arrecadador (// TODO(M10): transmissao real).</summary>
    Transmitida = 2,
}

/// <summary>
/// Apuracao do PASEP (Programa de Formacao do Patrimonio do Servidor Publico — LC 8/1970): contribuicao
/// do ENTE publico incidente sobre a folha de pagamento, recolhida mensalmente (1% da base por padrao,
/// aliquota parametrizavel por tenant — sem numero magico). Apura BASE (folha bruta da competencia) e
/// VALOR (base x aliquota); a transmissao/recolhimento real e' diferida ao M10. Unica por
/// <c>(TenantId, Competencia)</c>. Raiz de agregado, nasce valida via <see cref="Apurar"/>.
/// </summary>
public sealed class ApuracaoPasep : AggregateRoot<ApuracaoPasepId>, IMustHaveTenant
{
    private ApuracaoPasep()
    {
    }

    private ApuracaoPasep(
        ApuracaoPasepId id,
        Guid tenantId,
        Competencia competencia,
        decimal baseContribuicao,
        decimal aliquota,
        decimal valor)
        : base(id)
    {
        TenantId = tenantId;
        Competencia = competencia;
        BaseContribuicao = baseContribuicao;
        Aliquota = aliquota;
        Valor = valor;
        Situacao = SituacaoApuracaoPasep.Apurada;
        RaiseDomainEvent(new PasepApurado(id, competencia, baseContribuicao, valor));
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Competencia (mes/ano) de referencia.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Base de calculo: folha bruta (total de proventos) da competencia, em BRL.</summary>
    public decimal BaseContribuicao { get; private set; }

    /// <summary>Aliquota aplicada (percentual; ex.: 1.00 = 1%), parametrizada na apuracao.</summary>
    public decimal Aliquota { get; private set; }

    /// <summary>Valor apurado da contribuicao (base x aliquota), arredondado a 2 casas.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Situacao (estado) atual da apuracao.</summary>
    public SituacaoApuracaoPasep Situacao { get; private set; }

    /// <summary>
    /// Apura o PASEP da competencia a partir da base (folha bruta) e da aliquota informada (em
    /// percentual). Calcula o valor (base x aliquota / 100) arredondado a 2 casas e nasce
    /// <see cref="SituacaoApuracaoPasep.Apurada"/>, emitindo <see cref="PasepApurado"/>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="competencia">Competencia de referencia.</param>
    /// <param name="baseContribuicao">Base de calculo (folha bruta da competencia); maior ou igual a zero.</param>
    /// <param name="aliquota">Aliquota em percentual (ex.: 1.00 = 1%); em (0, 100].</param>
    /// <returns>Nova <see cref="ApuracaoPasep"/> em situacao <see cref="SituacaoApuracaoPasep.Apurada"/>.</returns>
    /// <exception cref="ArgumentNullException">Se a competencia for nula.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a base for negativa ou a aliquota estiver fora de (0, 100].</exception>
    public static ApuracaoPasep Apurar(
        Guid tenantId,
        Competencia competencia,
        decimal baseContribuicao,
        decimal aliquota)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentOutOfRangeException.ThrowIfNegative(baseContribuicao);
        if (aliquota <= 0m || aliquota > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquota), aliquota, "Aliquota do PASEP deve estar em (0, 100].");
        }

        var valor = decimal.Round(baseContribuicao * aliquota / 100m, 2, MidpointRounding.AwayFromZero);
        return new ApuracaoPasep(ApuracaoPasepId.New(), tenantId, competencia, baseContribuicao, aliquota, valor);
    }

    /// <summary>
    /// Marca a apuracao como transmitida/recolhida. // TODO(M10): integrar a transmissao/recolhimento real
    /// (GPS/DARF/agente arrecadador) atras de ACL com Polly + certificado A1 do Key Vault.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a apuracao ja estiver transmitida.</exception>
    public void MarcarTransmitida()
    {
        if (Situacao == SituacaoApuracaoPasep.Transmitida)
        {
            throw new InvalidOperationException("Apuracao do PASEP ja transmitida.");
        }

        Situacao = SituacaoApuracaoPasep.Transmitida;
    }
}
