using Tensorroot.Gov.SharedKernel.Primitives;
using Tensorroot.Gov.SharedKernel.Tempo;

namespace Tensorroot.Gov.Modules.Administracao.Domain.Contratos;

/// <summary>
/// Triade de prazo PNCP no DOMINIO (quantidade/unidade/norma) — espelho domestico do parametro do tenant.
/// Mantida no Domain para que a factory <see cref="Contrato.Celebrar"/> receba o prazo sem o agregado
/// depender da Application (regra de dependencia — CLAUDE.md §2). A Application mapeia seu
/// <c>ParametroPrazo</c> para este tipo.
/// </summary>
/// <param name="Quantidade">Quantidade de dias (&gt;= 0).</param>
/// <param name="Unidade">Unidade de contagem (uteis|corridos).</param>
/// <param name="NormaFonte">Citacao legal (ex.: "Lei 14.133/2021 art. 94").</param>
public sealed record PrazoPncpParametro(int Quantidade, UnidadePrazo Unidade, string NormaFonte);

/// <summary>Veredito do alerta de prazo de divulgacao no PNCP (W9.1.d) para um contrato nao divulgado.</summary>
public enum AlertaPrazoPncp
{
    /// <summary>Nenhum alerta (ainda fora da janela, ja divulgado ou extinto).</summary>
    Nenhum = 0,

    /// <summary>A vencer: dentro da janela de antecedencia, ainda dentro do prazo.</summary>
    AVencer = 1,

    /// <summary>Vencido: prazo do art. 94 ultrapassado sem divulgacao (contrato ineficaz).</summary>
    Vencido = 2,
}

/// <summary>Natureza do prazo PNCP que esta sendo contado (Lei 14.133/2021, art. 94).</summary>
public enum TipoPrazoPncp
{
    /// <summary>Divulgacao do instrumento contratual no PNCP — condicao de eficacia (art. 94, caput; 20 d.u.).</summary>
    Divulgacao = 1,

    /// <summary>Registro/divulgacao do extrato do contrato (art. 94; 10 d.u.).</summary>
    RegistroExtrato = 2,

    /// <summary>Prazo especifico de OBRAS (art. 94 §3; 25/45 d.u.) — usado pelo modulo de Obras (W9.3).</summary>
    Obra = 3,
}

/// <summary>
/// Prazo de publicacao no PNCP de um contrato (Lei 14.133/2021, art. 94): "X dias uteis a partir da data
/// de assinatura". Value Object de DOMINIO que envelopa o <see cref="PrazoLegal"/> transversal
/// (SharedKernel) com a semantica do PNCP (qual prazo do art. 94 esta correndo) e a regra de eficacia:
/// <b>contrato divulgado no prazo = eficaz no PNCP; divulgado fora do prazo = publicado, porem com prazo
/// VENCIDO</b> (subsidio para alerta/auditoria do Tribunal de Contas).
/// <para>
/// Reproduzivel (CLAUDE.md §7): nao le relogio — o <c>hoje</c> das perguntas "vencido?/a vencer?" e
/// sempre informado pelo Application (via <c>TimeProvider</c>). A quantidade/unidade/norma vem dos
/// parametros do tenant (<c>IPncpParametros</c>), nunca literal (CLAUDE.md §16).
/// </para>
/// </summary>
public sealed class PrazoPncp : ValueObject
{
    private PrazoPncp(TipoPrazoPncp tipo, PrazoLegal prazo)
    {
        Tipo = tipo;
        Prazo = prazo;
    }

    /// <summary>Natureza do prazo (divulgacao/registro/obra).</summary>
    public TipoPrazoPncp Tipo { get; }

    /// <summary>Prazo legal subjacente (datas resolvidas + norma-fonte).</summary>
    public PrazoLegal Prazo { get; }

    /// <summary>Data de assinatura, a partir da qual a contagem do art. 94 corre.</summary>
    public DateOnly DataAssinatura => Prazo.Inicio;

    /// <summary>Data-limite legal para a publicacao no PNCP (vencimento ja resolvido).</summary>
    public DateOnly DataLimitePublicacao => Prazo.Vencimento;

    /// <summary>Citacao legal do prazo (ex.: "Lei 14.133/2021 art. 94").</summary>
    public string NormaFonte => Prazo.NormaFonte;

    /// <summary>
    /// Reidrata um <see cref="PrazoPncp"/> a partir de valores JA RESOLVIDOS (persistencia EF), sem reler
    /// o calendario — o vencimento persistido e a fonte de verdade do ato praticado na celebracao.
    /// </summary>
    /// <param name="tipo">Natureza do prazo.</param>
    /// <param name="dataAssinatura">Data de assinatura (inicio).</param>
    /// <param name="quantidade">Quantidade de dias original.</param>
    /// <param name="unidade">Unidade de contagem original.</param>
    /// <param name="dataLimite">Data-limite ja resolvida (persistida).</param>
    /// <param name="normaFonte">Norma-fonte original.</param>
    /// <returns>Prazo PNCP reconstruido.</returns>
    public static PrazoPncp Reidratar(
        TipoPrazoPncp tipo,
        DateOnly dataAssinatura,
        int quantidade,
        UnidadePrazo unidade,
        DateOnly dataLimite,
        string normaFonte)
        => new(tipo, PrazoLegal.Reidratar(dataAssinatura, quantidade, unidade, dataLimite, normaFonte));

    /// <summary>
    /// Constroi o prazo PNCP a partir da data de assinatura, usando o calendario de dias uteis do tenant.
    /// A quantidade/unidade/norma vem dos parametros do tenant (sem numero magico).
    /// </summary>
    /// <param name="tipo">Natureza do prazo (divulgacao/registro/obra).</param>
    /// <param name="dataAssinatura">Data de assinatura do contrato (inicio da contagem do art. 94).</param>
    /// <param name="quantidade">Quantidade de dias do prazo (dos parametros do tenant).</param>
    /// <param name="unidade">Unidade de contagem (dos parametros do tenant).</param>
    /// <param name="normaFonte">Norma-fonte citavel (dos parametros do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns>Novo <see cref="PrazoPncp"/> com o vencimento resolvido.</returns>
    public static PrazoPncp Criar(
        TipoPrazoPncp tipo,
        DateOnly dataAssinatura,
        int quantidade,
        UnidadePrazo unidade,
        string normaFonte,
        ICalendarioDiasUteis calendario)
    {
        var prazo = PrazoLegal.Criar(dataAssinatura, quantidade, unidade, normaFonte, calendario);
        return new PrazoPncp(tipo, prazo);
    }

    /// <summary>Vencido em <paramref name="hoje"/> (sem publicacao tempestiva). 'hoje' vem do TimeProvider.</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se o prazo legal ja venceu.</returns>
    public bool Vencido(DateOnly hoje) => Prazo.Vencido(hoje);

    /// <summary>A vencer em <paramref name="hoje"/> (ainda dentro do prazo).</summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <returns><c>true</c> se ainda dentro do prazo.</returns>
    public bool AVencer(DateOnly hoje) => Prazo.AVencer(hoje);

    /// <summary>
    /// Verdadeiro se o prazo entra na janela de ALERTA preventivo: ainda nao venceu, mas faltam
    /// <paramref name="antecedenciaDiasUteis"/> dias uteis ou menos ate o vencimento.
    /// </summary>
    /// <param name="hoje">Data de referencia.</param>
    /// <param name="antecedenciaDiasUteis">Janela de antecedencia (dos parametros do tenant).</param>
    /// <param name="calendario">Calendario de dias uteis do tenant.</param>
    /// <returns><c>true</c> se deve alertar a gestao.</returns>
    public bool DentroDaJanelaDeAlerta(DateOnly hoje, int antecedenciaDiasUteis, ICalendarioDiasUteis calendario)
    {
        ArgumentNullException.ThrowIfNull(calendario);
        if (Vencido(hoje))
        {
            return false;
        }

        var restantes = Prazo.DiasUteisRestantes(hoje, calendario);
        return restantes <= antecedenciaDiasUteis;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return (int)Tipo;
        yield return Prazo;
    }
}
