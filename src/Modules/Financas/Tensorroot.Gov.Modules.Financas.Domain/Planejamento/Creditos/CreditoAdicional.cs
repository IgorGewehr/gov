using Tensorroot.Gov.Modules.Financas.Domain.Dotacoes;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Events;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Loa;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Financas.Domain.Planejamento.Creditos;

/// <summary>
/// Crédito Adicional (Lei 4.320/64, arts. 40-46): o ato que ALTERA a LOA. Registra a espécie
/// (Suplementar/Especial/Extraordinário), o ato autorizador/abertura e a fonte de recurso, com
/// trilha imutável. Ao <see cref="Abrir"/>, o handler aplica o efeito nas dotações (reforço/anulação)
/// reusando o roteiro contábil EVT-DOT já provado.
/// </summary>
public sealed class CreditoAdicional : AggregateRoot<CreditoAdicionalId>, IMustHaveTenant
{
    private CreditoAdicional()
    {
    }

    private CreditoAdicional(
        CreditoAdicionalId id,
        Guid tenantId,
        LoaId loaId,
        int exercicio,
        EspecieCredito especie,
        FonteRecursoCredito fonte,
        ValorMonetario valor,
        string atoAutorizador,
        string atoAbertura)
        : base(id)
    {
        TenantId = tenantId;
        LoaId = loaId;
        Exercicio = exercicio;
        Especie = especie;
        Fonte = fonte;
        Valor = valor;
        AtoAutorizador = atoAutorizador;
        AtoAbertura = atoAbertura;
        Situacao = SituacaoCredito.Registrado;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>LOA alterada por este crédito.</summary>
    public LoaId LoaId { get; private set; }

    /// <summary>Exercício de referência.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Espécie do crédito (art. 41).</summary>
    public EspecieCredito Especie { get; private set; }

    /// <summary>Fonte de recurso (art. 43 §1º).</summary>
    public FonteRecursoCredito Fonte { get; private set; }

    /// <summary>Valor do crédito.</summary>
    public ValorMonetario Valor { get; private set; } = default!;

    /// <summary>Ato autorizador (lei/autorização na LOA p/ suplementar; lei específica p/ especial).</summary>
    public string AtoAutorizador { get; private set; } = default!;

    /// <summary>Ato de abertura (decreto/MP — arts. 42-44).</summary>
    public string AtoAbertura { get; private set; } = default!;

    /// <summary>Dotação reforçada/criada (alvo). Nulo até definida (especial cria depois).</summary>
    public DotacaoOrcamentariaId? DotacaoAlvoId { get; private set; }

    /// <summary>Dotação anulada como fonte (quando <see cref="FonteRecursoCredito.AnulacaoDotacao"/>).</summary>
    public DotacaoOrcamentariaId? DotacaoAnuladaId { get; private set; }

    /// <summary>Indica se decorre de suplementação por DECRETO (conta no limite do art. 7º).</summary>
    public bool PorDecreto { get; private set; }

    /// <summary>Situação do crédito.</summary>
    public SituacaoCredito Situacao { get; private set; }

    /// <summary>
    /// Registra um crédito SUPLEMENTAR (reforça dotação existente). Quando por decreto, conta no
    /// limite de suplementação autorizado pela LOA (validado no handler/serviço).
    /// </summary>
    /// <returns>Novo <see cref="CreditoAdicional"/> suplementar.</returns>
    public static CreditoAdicional Suplementar(
        Guid tenantId,
        LeiOrcamentariaAnual loa,
        ValorMonetario valor,
        FonteRecursoCredito fonte,
        DotacaoOrcamentariaId dotacaoAlvoId,
        DotacaoOrcamentariaId? dotacaoAnuladaId,
        string atoAutorizador,
        string atoAbertura,
        bool porDecreto)
    {
        var credito = CriarBase(tenantId, loa, EspecieCredito.Suplementar, fonte, valor, atoAutorizador, atoAbertura);
        ValidarFonteAnulacao(fonte, dotacaoAnuladaId);
        credito.DotacaoAlvoId = dotacaoAlvoId;
        credito.DotacaoAnuladaId = dotacaoAnuladaId;
        credito.PorDecreto = porDecreto;
        return credito;
    }

    /// <summary>
    /// Registra um crédito ESPECIAL (cria dotação nova — exige lei específica). O item de despesa
    /// e a dotação são gerados no handler; aqui apenas se registra o ato.
    /// </summary>
    /// <returns>Novo <see cref="CreditoAdicional"/> especial.</returns>
    public static CreditoAdicional Especial(
        Guid tenantId,
        LeiOrcamentariaAnual loa,
        ValorMonetario valor,
        FonteRecursoCredito fonte,
        DotacaoOrcamentariaId? dotacaoAnuladaId,
        string atoAutorizador,
        string atoAbertura)
    {
        var credito = CriarBase(tenantId, loa, EspecieCredito.Especial, fonte, valor, atoAutorizador, atoAbertura);
        ValidarFonteAnulacao(fonte, dotacaoAnuladaId);
        credito.DotacaoAnuladaId = dotacaoAnuladaId;
        return credito;
    }

    /// <summary>
    /// Registra um crédito EXTRAORDINÁRIO (despesa urgente/imprevista — CF 167 §3º; art. 44).
    /// Dispensa autorização legislativa prévia e indicação de fonte; cria dotação nova.
    /// </summary>
    /// <returns>Novo <see cref="CreditoAdicional"/> extraordinário.</returns>
    public static CreditoAdicional Extraordinario(
        Guid tenantId,
        LeiOrcamentariaAnual loa,
        ValorMonetario valor,
        string atoAbertura)
    {
        // Extraordinário: autorizador dispensado; fonte dispensada (art. 44).
        return CriarBase(tenantId, loa, EspecieCredito.Extraordinario, FonteRecursoCredito.Dispensada, valor, atoAutorizador: "DISPENSADO_CF167_3", atoAbertura);
    }

    /// <summary>Define a dotação alvo criada para um crédito Especial/Extraordinário (antes de abrir).</summary>
    /// <param name="dotacaoAlvoId">Dotação criada.</param>
    public void DefinirDotacaoAlvo(DotacaoOrcamentariaId dotacaoAlvoId)
    {
        if (Situacao != SituacaoCredito.Registrado)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(CreditoAdicional), Situacao.ToString(), nameof(DefinirDotacaoAlvo));
        }

        DotacaoAlvoId = dotacaoAlvoId;
    }

    /// <summary>
    /// Marca o crédito como aberto (efeito aplicado às dotações). Levanta o evento de auditoria/contábil.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a dotação alvo não estiver definida.</exception>
    public void Abrir()
    {
        if (Situacao != SituacaoCredito.Registrado)
        {
            throw new TransicaoPlanejamentoInvalidaException(nameof(CreditoAdicional), Situacao.ToString(), nameof(Abrir));
        }

        if (DotacaoAlvoId is null)
        {
            throw new InvalidOperationException("Credito adicional exige dotacao alvo definida antes da abertura.");
        }

        Situacao = SituacaoCredito.Aberto;
        RaiseDomainEvent(new CreditoAdicionalAberto(Id, LoaId, Valor.Valor));
    }

    private static CreditoAdicional CriarBase(
        Guid tenantId,
        LeiOrcamentariaAnual loa,
        EspecieCredito especie,
        FonteRecursoCredito fonte,
        ValorMonetario valor,
        string atoAutorizador,
        string atoAbertura)
    {
        ArgumentNullException.ThrowIfNull(loa);
        ArgumentNullException.ThrowIfNull(valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(atoAutorizador);
        ArgumentException.ThrowIfNullOrWhiteSpace(atoAbertura);
        if (!valor.EhPositivo())
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "Valor do credito deve ser positivo.");
        }

        if (loa.Situacao != SituacaoLoa.EmExecucao)
        {
            throw new InvalidOperationException("Credito adicional exige a LOA em execucao.");
        }

        return new CreditoAdicional(CreditoAdicionalId.New(), tenantId, loa.Id, loa.Exercicio, especie, fonte, valor, atoAutorizador.Trim(), atoAbertura.Trim());
    }

    private static void ValidarFonteAnulacao(FonteRecursoCredito fonte, DotacaoOrcamentariaId? dotacaoAnuladaId)
    {
        if (fonte == FonteRecursoCredito.AnulacaoDotacao && dotacaoAnuladaId is null)
        {
            throw new InvalidOperationException("Fonte por anulacao exige a dotacao a anular.");
        }

        if (fonte != FonteRecursoCredito.AnulacaoDotacao && dotacaoAnuladaId is not null)
        {
            throw new InvalidOperationException("Dotacao anulada so se aplica a fonte por anulacao de dotacao.");
        }
    }
}
