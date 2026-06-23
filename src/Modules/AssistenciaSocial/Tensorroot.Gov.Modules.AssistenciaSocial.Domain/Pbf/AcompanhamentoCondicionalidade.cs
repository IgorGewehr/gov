using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Events;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Familias;
using Tensorroot.Gov.Modules.AssistenciaSocial.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.AssistenciaSocial.Domain.Pbf;

/// <summary>Identificador forte de <see cref="AcompanhamentoCondicionalidade"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct AcompanhamentoCondicionalidadeId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="AcompanhamentoCondicionalidadeId"/>.</returns>
    public static AcompanhamentoCondicionalidadeId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// <b>3d.1 — Acompanhamento de Condicionalidades do PBF.</b> Acompanhamento <b>local</b> das
/// condicionalidades do Programa Bolsa Familia (educacao: frequencia escolar; saude: pre-natal,
/// vacina e nutricao) por <b>(Familia, Competencia)</b>, com registro de cumprimento/descumprimento
/// e calculo do <b>efeito gradativo gerencial</b> (advertencia → bloqueio → suspensao). Vincula-se as
/// familias do CadUnico por <see cref="FamiliaId"/> (FK logica). A base MDS/SICON e autoritativa e o
/// beneficio federal nunca e alterado por este agregado (I-9 de Familia) — o efeito aqui apoia a BUSCA
/// ATIVA do CRAS, conforme o Decreto 11.617/2023 (gestao de condicionalidades).
/// <para>
/// Raiz de agregado; os <see cref="RegistroCondicionalidade"/> sao entidades-filhas. O efeito e
/// derivado da contagem de descumprimentos efetivos (descumprida e nao justificada) no periodo.
/// </para>
/// // TODO(M10): sincronizar com o Sistema de Condicionalidades / SICON / CECAD do MDS (importar a
/// frequencia oficial do Sistema Presenca/MEC e o acompanhamento de saude do SISVAN) — requer
/// credencial/convenio MDS. Ate la, o registro e local (manual e/ou alimentado por evento Educacao/Saude
/// via Contracts).
/// </summary>
public sealed class AcompanhamentoCondicionalidade : AggregateRoot<AcompanhamentoCondicionalidadeId>, IMustHaveTenant
{
    /// <summary>Limiar de descumprimentos para advertencia (1º registro).</summary>
    public const int DescumprimentosParaAdvertencia = 1;

    /// <summary>Limiar de descumprimentos para bloqueio (2º registro).</summary>
    public const int DescumprimentosParaBloqueio = 2;

    /// <summary>Limiar de descumprimentos para suspensao (3º ou mais).</summary>
    public const int DescumprimentosParaSuspensao = 3;

    private readonly List<RegistroCondicionalidade> _registros = [];

    private AcompanhamentoCondicionalidade()
    {
    }

    private AcompanhamentoCondicionalidade(
        AcompanhamentoCondicionalidadeId id,
        Guid tenantId,
        FamiliaId familiaId,
        Competencia competencia)
        : base(id)
    {
        TenantId = tenantId;
        FamiliaId = familiaId;
        Competencia = competencia;
    }

    /// <summary>Tenant (municipio) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Familia beneficiaria acompanhada (FK logica para o CadUnico).</summary>
    public FamiliaId FamiliaId { get; private set; }

    /// <summary>Competencia (ano/mes) do periodo de acompanhamento.</summary>
    public Competencia Competencia { get; private set; }

    /// <summary>Efeito gradativo vigente no periodo (derivado dos descumprimentos efetivos).</summary>
    public EfeitoDescumprimento Efeito { get; private set; }

    /// <summary>Registros de condicionalidade do periodo (entidades-filhas).</summary>
    public IReadOnlyCollection<RegistroCondicionalidade> Registros => _registros.AsReadOnly();

    /// <summary>Quantidade de descumprimentos EFETIVOS (descumprida e nao justificada) no periodo.</summary>
    public int DescumprimentosEfetivos => _registros.Count(r => r.Status == StatusCondicionalidade.Descumprida);

    /// <summary>
    /// Abre o acompanhamento de condicionalidades de uma familia numa competencia (sem registros).
    /// </summary>
    /// <param name="tenantId">Tenant dono.</param>
    /// <param name="familiaId">Familia beneficiaria (obrigatoria).</param>
    /// <param name="competencia">Competencia (ano/mes) valida.</param>
    /// <returns>Novo <see cref="AcompanhamentoCondicionalidade"/> sem efeito.</returns>
    /// <exception cref="ArgumentException">Se a familia for vazia ou a competencia invalida.</exception>
    public static AcompanhamentoCondicionalidade Abrir(Guid tenantId, FamiliaId familiaId, Competencia competencia)
    {
        if (familiaId.Value == Guid.Empty)
        {
            throw new ArgumentException("Familia e obrigatoria no acompanhamento de condicionalidades.", nameof(familiaId));
        }

        if (!competencia.EhValida())
        {
            throw new ArgumentException("Competencia (ano/mes) do acompanhamento e obrigatoria e valida.", nameof(competencia));
        }

        return new AcompanhamentoCondicionalidade(AcompanhamentoCondicionalidadeId.New(), tenantId, familiaId, competencia);
    }

    /// <summary>
    /// Registra uma condicionalidade (educacao/saude) de um membro e recalcula o efeito gradativo.
    /// </summary>
    /// <param name="tipo">Eixo da condicionalidade.</param>
    /// <param name="membroId">Membro da familia ao qual a condicionalidade se aplica.</param>
    /// <param name="status">Status do cumprimento (cumprida/descumprida/justificada/pendente).</param>
    /// <param name="observacao">Observacao/motivo (obrigatorio na justificativa).</param>
    /// <returns>Identificador do registro criado.</returns>
    public RegistroCondicionalidadeId RegistrarCondicionalidade(
        TipoCondicionalidade tipo,
        Guid membroId,
        StatusCondicionalidade status,
        string? observacao)
    {
        var registro = RegistroCondicionalidade.Criar(Id, tipo, membroId, status, observacao);
        _registros.Add(registro);
        RecalcularEfeito();
        return registro.Id;
    }

    /// <summary>
    /// Justifica um descumprimento (motivo do CRAS): retira o registro da contagem de descumprimentos
    /// efetivos e recalcula o efeito gradativo (pode reduzir a gradacao).
    /// </summary>
    /// <param name="registroId">Registro de condicionalidade descumprida.</param>
    /// <param name="motivo">Motivo da justificativa (obrigatorio).</param>
    /// <exception cref="InvalidOperationException">Se o registro nao existir ou nao estiver descumprido.</exception>
    public void JustificarDescumprimento(RegistroCondicionalidadeId registroId, string motivo)
    {
        var registro = _registros.Find(r => r.Id == registroId)
            ?? throw new InvalidOperationException("Registro de condicionalidade inexistente neste acompanhamento.");

        registro.Justificar(motivo);
        RecalcularEfeito();
    }

    private void RecalcularEfeito()
    {
        var anterior = Efeito;

        // R-1/R-2: a gradacao deriva da contagem de descumprimentos efetivos no periodo. A base
        // federal (SICON) e autoritativa — este e o efeito GERENCIAL local para a busca ativa.
        Efeito = DescumprimentosEfetivos switch
        {
            0 => EfeitoDescumprimento.Nenhum,
            DescumprimentosParaAdvertencia => EfeitoDescumprimento.Advertencia,
            DescumprimentosParaBloqueio => EfeitoDescumprimento.Bloqueio,
            _ => EfeitoDescumprimento.Suspensao,
        };

        // Emite o evento apenas quando o efeito ESCALA para uma gradacao com impacto (>= Bloqueio):
        // a busca ativa do CRAS e disparada por esses casos (nunca trafega dado sigiloso — so o efeito).
        if (Efeito != anterior && Efeito >= EfeitoDescumprimento.Bloqueio)
        {
            RaiseDomainEvent(new CondicionalidadeDescumprida(Id, FamiliaId, Competencia, Efeito, DescumprimentosEfetivos));
        }
    }
}
