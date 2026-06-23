using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Censo;

/// <summary>Identificador forte de <see cref="FormularioCensoSuas"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct FormularioCensoSuasId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="FormularioCensoSuasId"/>.</returns>
    public static FormularioCensoSuasId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>Situacao do formulario do Censo SUAS no exercicio.</summary>
public enum SituacaoCenso
{
    /// <summary>Em consolidacao — admite reconsolidacao a partir das fontes locais.</summary>
    EmConsolidacao = 1,

    /// <summary>Fechado — exercicio selado para envio ao SAGI/MDS (terminal; auditavel).</summary>
    Fechado = 2,
}

/// <summary>
/// <b>3d.2 — Formulario do Censo SUAS (consolidado anual por unidade).</b> Consolidado ANUAL de gestao
/// de uma <see cref="UnidadeSocioassistencial"/> num exercicio: estrutura/RH/servicos (da propria
/// unidade) e VOLUME de atendimentos (derivado do RMA/Prontuario ja existentes — sem dupla digitacao).
/// E a base do questionario do Censo SUAS (anual). Raiz de agregado por <b>(Unidade, Exercicio)</b>.
/// <para>
/// Os indicadores de volume sao AGREGADOS (somatorio anual dos RMAs da unidade) — nenhum dado sigiloso
/// identificavel do prontuario trafega (LGPD art. 11). A reconsolidacao e idempotente enquanto em
/// consolidacao; o fechamento sela o exercicio.
/// </para>
/// // TODO(M10): envio ao Censo SUAS / SAGI (MDS) e o conjunto exato de blocos do questionario vigente
/// dependem do aplicativo/manual oficial do exercicio — requer credencial MDS.
/// </summary>
public sealed class FormularioCensoSuas : AggregateRoot<FormularioCensoSuasId>, IMustHaveTenant
{
    private FormularioCensoSuas()
    {
    }

    private FormularioCensoSuas(
        FormularioCensoSuasId id,
        Guid tenantId,
        UnidadeSocioassistencialId unidadeId,
        int exercicio)
        : base(id)
    {
        TenantId = tenantId;
        UnidadeId = unidadeId;
        Exercicio = exercicio;
        Situacao = SituacaoCenso.EmConsolidacao;
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Unidade socioassistencial consolidada.</summary>
    public UnidadeSocioassistencialId UnidadeId { get; private set; }

    /// <summary>Exercicio (ano) de referencia do Censo.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Situacao do consolidado (em consolidacao/fechado).</summary>
    public SituacaoCenso Situacao { get; private set; }

    /// <summary>Tamanho da equipe de referencia consolidado (snapshot da unidade no exercicio).</summary>
    public int QuantidadeProfissionais { get; private set; }

    /// <summary>Quantidade de servicos tipificados ofertados (snapshot da unidade no exercicio).</summary>
    public int QuantidadeServicosOfertados { get; private set; }

    /// <summary>Familias referenciadas a unidade no exercicio (derivado do cadastro de familias).</summary>
    public int FamiliasReferenciadas { get; private set; }

    /// <summary>Volume anual de atendimentos consolidado dos RMAs da unidade no exercicio.</summary>
    public int VolumeAtendimentosAno { get; private set; }

    /// <summary>Data/hora (UTC) do fechamento do exercicio (nulo enquanto em consolidacao).</summary>
    public DateTime? FechadoEmUtc { get; private set; }

    /// <summary>Abre o formulario do Censo de uma unidade num exercicio (zerado; consolida-se das fontes locais).</summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="unidadeId">Unidade consolidada.</param>
    /// <param name="exercicio">Exercicio (ano) de referencia (&gt;= 2000).</param>
    /// <returns>Novo <see cref="FormularioCensoSuas"/> em consolidacao.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se o exercicio for invalido.</exception>
    public static FormularioCensoSuas Abrir(Guid tenantId, UnidadeSocioassistencialId unidadeId, int exercicio)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exercicio, 2000);
        if (unidadeId.Value == Guid.Empty)
        {
            throw new ArgumentException("Unidade e obrigatoria no formulario do Censo SUAS.", nameof(unidadeId));
        }

        return new FormularioCensoSuas(FormularioCensoSuasId.New(), tenantId, unidadeId, exercicio);
    }

    /// <summary>
    /// (Re)consolida os indicadores do exercicio a partir das fontes locais (unidade + RMA + familias).
    /// Idempotente enquanto em consolidacao.
    /// </summary>
    /// <param name="quantidadeProfissionais">Equipe de referencia (da unidade).</param>
    /// <param name="quantidadeServicosOfertados">Servicos tipificados ofertados (da unidade).</param>
    /// <param name="familiasReferenciadas">Familias referenciadas a unidade (do cadastro de familias).</param>
    /// <param name="volumeAtendimentosAno">Volume anual de atendimentos (somatorio dos RMAs da unidade).</param>
    /// <exception cref="InvalidOperationException">Se o exercicio ja estiver fechado.</exception>
    public void Consolidar(
        int quantidadeProfissionais,
        int quantidadeServicosOfertados,
        int familiasReferenciadas,
        int volumeAtendimentosAno)
    {
        GarantirEmConsolidacao();
        ArgumentOutOfRangeException.ThrowIfNegative(quantidadeProfissionais);
        ArgumentOutOfRangeException.ThrowIfNegative(quantidadeServicosOfertados);
        ArgumentOutOfRangeException.ThrowIfNegative(familiasReferenciadas);
        ArgumentOutOfRangeException.ThrowIfNegative(volumeAtendimentosAno);

        QuantidadeProfissionais = quantidadeProfissionais;
        QuantidadeServicosOfertados = quantidadeServicosOfertados;
        FamiliasReferenciadas = familiasReferenciadas;
        VolumeAtendimentosAno = volumeAtendimentosAno;
    }

    /// <summary>Fecha (sela) o exercicio do Censo para envio ao SAGI/MDS. Operacao terminal.</summary>
    /// <param name="fechadoEmUtc">Momento (UTC) do fechamento.</param>
    /// <exception cref="InvalidOperationException">Se ja estiver fechado.</exception>
    public void Fechar(DateTime fechadoEmUtc)
    {
        GarantirEmConsolidacao();
        Situacao = SituacaoCenso.Fechado;
        FechadoEmUtc = fechadoEmUtc;
    }

    private void GarantirEmConsolidacao()
    {
        if (Situacao != SituacaoCenso.EmConsolidacao)
        {
            throw new InvalidOperationException($"Censo ja fechado nao admite consolidacao/fechamento. Situacao atual: {Situacao}.");
        }
    }
}
