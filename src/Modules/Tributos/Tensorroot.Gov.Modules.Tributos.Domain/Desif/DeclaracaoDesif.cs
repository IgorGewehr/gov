using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Desif;

/// <summary>Identificador forte do agregado <see cref="DeclaracaoDesif"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct DeclaracaoDesifId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="DeclaracaoDesifId"/>.</returns>
    public static DeclaracaoDesifId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Módulo do leiaute DES-IF (modelo conceitual ABRASF). A DES-IF é composta de quatro módulos com
/// periodicidades distintas. Nesta implementação, modela-se a entrega do <b>Módulo 2 — Apuração Mensal
/// do ISSQN</b> (periodicidade mensal), que é o que constitui o crédito tributário do banco; os demais
/// módulos (anuais/sob demanda) são identificados para rastreabilidade do tipo de entrega.
/// </summary>
public enum ModuloDesif
{
    /// <summary>Módulo 1 — Demonstrativo Contábil (Balancete Analítico Mensal — BAM). Periodicidade anual.</summary>
    DemonstrativoContabil = 1,

    /// <summary>Módulo 2 — Apuração Mensal do ISSQN (DAS por subtítulo + ISSQN a recolher). Periodicidade mensal.</summary>
    ApuracaoMensalIssqn = 2,

    /// <summary>Módulo 3 — Informações Comuns aos Municípios (PGCC, tabelas). Periodicidade anual.</summary>
    InformacoesComuns = 3,

    /// <summary>Módulo 4 — Demonstrativo das Partidas dos Lançamentos Contábeis. Periodicidade sob demanda.</summary>
    PartidasLancamentos = 4,
}

/// <summary>Situação (estado) da declaração DES-IF.</summary>
public enum SituacaoDesif
{
    /// <summary>Em elaboração (subtítulos sendo escriturados; ainda editável).</summary>
    EmElaboracao = 1,

    /// <summary>Entregue/transmitida (constitui o crédito do ISSQN devido por homologação).</summary>
    Entregue = 2,

    /// <summary>Substituída por uma declaração retificadora (não exigível).</summary>
    Substituida = 3,
}

/// <summary>
/// Declaração Eletrônica de Serviços de Instituições Financeiras (DES-IF) — Módulo 2, Apuração Mensal do
/// ISSQN, segundo o modelo conceitual ABRASF. Obrigação acessória das instituições financeiras e
/// equiparadas autorizadas pelo BACEN (que usam o Plano Contábil COSIF), substituindo a NFS-e para o
/// setor bancário. A apuração é feita por SUBTÍTULO contábil COSIF (Registro 0430 — receita tributável e
/// ISSQN devido por subtítulo) e consolidada com as DEDUÇÕES legais (Registro 0440 — ISSQN a recolher:
/// deduções da receita declarada, incentivos autorizados em lei e depósitos judiciais). Lançamento por
/// homologação (CTN art. 150). Cobre o gap de PARIDADE-PoC (incumbente SAPI tem DES-IF). NÃO emitimos a
/// declaração: o banco DECLARA. Domínio rico: o agregado protege seus invariantes (CLAUDE.md §7).
/// </summary>
public sealed class DeclaracaoDesif : AggregateRoot<DeclaracaoDesifId>, IMustHaveTenant
{
    private readonly List<SubtituloDesif> _subtitulos = [];

    private DeclaracaoDesif()
    {
    }

    private DeclaracaoDesif(
        DeclaracaoDesifId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        Competencia competencia,
        string fundamentoLegal)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        Modulo = ModuloDesif.ApuracaoMensalIssqn;
        Competencia = competencia;
        FundamentoLegal = fundamentoLegal;
        Situacao = SituacaoDesif.EmElaboracao;
        ReceitaTributavelTotal = ValorMonetario.Zero;
        IssqnDevidoBruto = ValorMonetario.Zero;
        DeducoesReceita = ValorMonetario.Zero;
        IncentivosFiscais = ValorMonetario.Zero;
        DepositosJudiciais = ValorMonetario.Zero;
        IssqnARecolher = ValorMonetario.Zero;
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte declarante (instituição financeira/equiparada).</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Módulo do leiaute DES-IF entregue (nesta implementação, Apuração Mensal do ISSQN).</summary>
    public ModuloDesif Modulo { get; private set; }

    /// <summary>Competência (mês/ano) declarada.</summary>
    public Competencia Competencia { get; private set; } = default!;

    /// <summary>Fundamento legal da obrigação acessória (CTM/decreto) — parametrizável por tenant.</summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Situação atual.</summary>
    public SituacaoDesif Situacao { get; private set; }

    /// <summary>Receita tributável total dos subtítulos COSIF tributáveis (R$) — base do Registro 0430.</summary>
    public ValorMonetario ReceitaTributavelTotal { get; private set; } = default!;

    /// <summary>ISSQN devido BRUTO (Σ por subtítulo, base × alíquota) — antes das deduções (R$).</summary>
    public ValorMonetario IssqnDevidoBruto { get; private set; } = default!;

    /// <summary>Deduções da receita declarada (Registro 0440, R$) — reduzem o ISSQN a recolher.</summary>
    public ValorMonetario DeducoesReceita { get; private set; } = default!;

    /// <summary>Incentivos fiscais autorizados em lei (Registro 0440, R$).</summary>
    public ValorMonetario IncentivosFiscais { get; private set; } = default!;

    /// <summary>Depósitos judiciais (Registro 0440, R$) — suspendem a exigibilidade da parcela depositada (CTN art. 151, II).</summary>
    public ValorMonetario DepositosJudiciais { get; private set; } = default!;

    /// <summary>ISSQN MENSAL A RECOLHER após deduções/incentivos/depósitos (R$) — Registro 0440.</summary>
    public ValorMonetario IssqnARecolher { get; private set; } = default!;

    /// <summary>Data de entrega (transmissão) — data do fato; nula enquanto em elaboração.</summary>
    public DateOnly? DataEntrega { get; private set; }

    /// <summary>Subtítulos COSIF declarados (Registro 0430): escrituração analítica e auditável.</summary>
    public IReadOnlyCollection<SubtituloDesif> Subtitulos => _subtitulos;

    /// <summary>Quantidade de subtítulos escriturados.</summary>
    public int QuantidadeSubtitulos => _subtitulos.Count;

    /// <summary>Abre uma DES-IF (Módulo 2 — Apuração Mensal do ISSQN) em elaboração.</summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte declarante (instituição financeira).</param>
    /// <param name="competencia">Competência mês/ano.</param>
    /// <param name="fundamentoLegal">Fundamento legal da obrigação acessória.</param>
    /// <returns>Nova <see cref="DeclaracaoDesif"/>.</returns>
    public static DeclaracaoDesif Abrir(Guid tenantId, ContribuinteId contribuinteId, Competencia competencia, string fundamentoLegal)
    {
        ArgumentNullException.ThrowIfNull(competencia);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        return new DeclaracaoDesif(DeclaracaoDesifId.New(), tenantId, contribuinteId, competencia, fundamentoLegal.Trim());
    }

    /// <summary>
    /// Escritura um subtítulo COSIF tributável (Registro 0430): conta/subtítulo COSIF, código de
    /// tributação DES-IF, item da lista LC 116, base de cálculo e alíquota. O ISSQN do subtítulo é
    /// calculado pelo agregado e acumulado no devido bruto. Só é possível enquanto EM ELABORAÇÃO.
    /// </summary>
    /// <param name="contaCosif">Conta/subtítulo do Plano Contábil COSIF (ex.: "7.1.7.99.00-8").</param>
    /// <param name="codigoTributacaoDesif">Código de tributação da tabela DES-IF (Anexo 6).</param>
    /// <param name="itemListaServico">Item da lista de serviços LC 116 correlato (ex.: "15.01").</param>
    /// <param name="descricao">Descrição do subtítulo/serviço.</param>
    /// <param name="baseCalculo">Receita tributável do subtítulo (base de cálculo, R$).</param>
    /// <param name="aliquotaPercentual">Alíquota aplicável (%) conforme o item (lei municipal).</param>
    /// <exception cref="InvalidOperationException">Se a declaração não estiver em elaboração.</exception>
    public void EscriturarSubtitulo(
        string contaCosif,
        string codigoTributacaoDesif,
        string itemListaServico,
        string descricao,
        ValorMonetario baseCalculo,
        decimal aliquotaPercentual)
    {
        ArgumentNullException.ThrowIfNull(baseCalculo);
        if (Situacao != SituacaoDesif.EmElaboracao)
        {
            throw new InvalidOperationException($"Só é possível escriturar subtítulos numa DES-IF em elaboração. Situação atual: {Situacao}.");
        }

        if (aliquotaPercentual is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(aliquotaPercentual), aliquotaPercentual, "A alíquota do ISSQN deve estar entre 0 e 100.");
        }

        var issqnSubtitulo = baseCalculo.AplicarPercentual(aliquotaPercentual);
        var subtitulo = SubtituloDesif.Criar(
            Id,
            contaCosif,
            codigoTributacaoDesif,
            itemListaServico,
            descricao,
            baseCalculo,
            aliquotaPercentual,
            issqnSubtitulo);
        _subtitulos.Add(subtitulo);

        ReceitaTributavelTotal = ReceitaTributavelTotal.Somar(baseCalculo);
        IssqnDevidoBruto = IssqnDevidoBruto.Somar(issqnSubtitulo);
    }

    /// <summary>
    /// ENTREGA (transmite) a DES-IF: aplica as deduções legais (Registro 0440 — deduções da receita
    /// declarada, incentivos autorizados em lei e depósitos judiciais), apura o ISSQN A RECOLHER líquido,
    /// fecha a declaração e constitui o crédito por homologação (CTN art. 150). Emite o evento com o
    /// ISSQN a recolher, que dispara o lançamento a jusante.
    /// </summary>
    /// <param name="deducoesReceita">Deduções da receita declarada (R$).</param>
    /// <param name="incentivosFiscais">Incentivos fiscais autorizados em lei (R$).</param>
    /// <param name="depositosJudiciais">Depósitos judiciais — suspendem a exigibilidade (R$).</param>
    /// <param name="dataEntrega">Data da entrega (data do fato — "hoje" administrativo).</param>
    /// <exception cref="InvalidOperationException">Se não estiver em elaboração ou estiver vazia.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se as deduções totais excederem o ISSQN devido bruto.</exception>
    public void Entregar(
        ValorMonetario deducoesReceita,
        ValorMonetario incentivosFiscais,
        ValorMonetario depositosJudiciais,
        DateOnly dataEntrega)
    {
        ArgumentNullException.ThrowIfNull(deducoesReceita);
        ArgumentNullException.ThrowIfNull(incentivosFiscais);
        ArgumentNullException.ThrowIfNull(depositosJudiciais);
        if (Situacao != SituacaoDesif.EmElaboracao)
        {
            throw new InvalidOperationException($"Só é possível entregar uma DES-IF em elaboração. Situação atual: {Situacao}.");
        }

        if (_subtitulos.Count == 0)
        {
            throw new InvalidOperationException("Não é possível entregar uma DES-IF sem subtítulos escriturados.");
        }

        // O ISSQN A RECOLHER é o devido bruto menos deduções/incentivos/depósitos (Registro 0440). As
        // deduções nunca podem exceder o imposto devido (não há "crédito negativo" de ISSQN a recolher):
        // fail-closed contra leiaute inconsistente que geraria base negativa (CLAUDE.md §7).
        var totalAbatimentos = deducoesReceita.Valor + incentivosFiscais.Valor + depositosJudiciais.Valor;
        if (totalAbatimentos > IssqnDevidoBruto.Valor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deducoesReceita),
                totalAbatimentos,
                $"As deduções/incentivos/depósitos ({totalAbatimentos:0.00}) não podem exceder o ISSQN devido bruto ({IssqnDevidoBruto.Valor:0.00}).");
        }

        DeducoesReceita = deducoesReceita;
        IncentivosFiscais = incentivosFiscais;
        DepositosJudiciais = depositosJudiciais;
        IssqnARecolher = ValorMonetario.De(IssqnDevidoBruto.Valor - totalAbatimentos);
        Situacao = SituacaoDesif.Entregue;
        DataEntrega = dataEntrega;
        RaiseDomainEvent(new DeclaracaoDesifEntregue(Id, TenantId, ContribuinteId, IssqnARecolher.Valor));
    }

    /// <summary>
    /// Marca a declaração como SUBSTITUÍDA por uma retificadora (deixa de ser exigível). A nova
    /// declaração retificadora é um agregado distinto da mesma competência.
    /// </summary>
    /// <exception cref="InvalidOperationException">Se a declaração não estiver entregue.</exception>
    public void MarcarSubstituida()
    {
        if (Situacao != SituacaoDesif.Entregue)
        {
            throw new InvalidOperationException($"Só uma DES-IF entregue pode ser substituída. Situação atual: {Situacao}.");
        }

        Situacao = SituacaoDesif.Substituida;
    }
}
