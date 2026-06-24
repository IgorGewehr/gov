using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Events;
using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Certidoes;

/// <summary>Identificador forte do agregado <see cref="CertidaoRegularidadeFiscal"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct CertidaoRegularidadeFiscalId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="CertidaoRegularidadeFiscalId"/>.</returns>
    public static CertidaoRegularidadeFiscalId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Espécie da certidão de regularidade fiscal emitida ao contribuinte (CTN art. 205/206).
/// </summary>
public enum TipoCertidaoRegularidade
{
    /// <summary>
    /// Certidão Negativa de Débitos (CND — CTN art. 205): NENHUM débito vencido em aberto e NENHUMA
    /// inscrição em dívida ativa exigível. Atesta regularidade plena.
    /// </summary>
    Negativa = 1,

    /// <summary>
    /// Certidão Positiva com efeitos de Negativa (CPEN — CTN art. 206): existem débitos, mas todos com
    /// exigibilidade SUSPENSA (parcelados) ou ainda NÃO vencidos / em execução fiscal com penhora —
    /// produz os mesmos efeitos da negativa.
    /// </summary>
    PositivaComEfeitoNegativa = 2,

    /// <summary>
    /// Certidão Positiva: há débito vencido em aberto e/ou dívida ativa exigível sem suspensão.
    /// O contribuinte NÃO está regular.
    /// </summary>
    Positiva = 3,
}

/// <summary>
/// Certidão de regularidade fiscal do contribuinte (CTN arts. 205/206): atesta, na DATA DE EMISSÃO
/// (data do fato — sem relógio no domínio, CLAUDE.md §16), a situação fiscal apurada — Negativa (CND),
/// Positiva-com-efeito-de-Negativa (CPEN) ou Positiva. Nasce com PRAZO DE VALIDADE parametrizável (lei
/// municipal) e CÓDIGO DE AUTENTICAÇÃO (hash determinístico dos dados essenciais) para conferência
/// pública de autenticidade. O resultado (tipo) é apurado FORA do agregado (varredura de débitos do
/// contribuinte) e informado na emissão; o agregado guarda o ATO imutável e auditável.
/// </summary>
public sealed class CertidaoRegularidadeFiscal : AggregateRoot<CertidaoRegularidadeFiscalId>, IMustHaveTenant
{
    /// <summary>Prazo de validade padrão da certidão, em dias (CTN art. 205 p.ú. — máx. 180 dias). // TODO(validar-oficial): prazo conforme o CTM.</summary>
    public const int DiasValidadePadrao = 90;

    private CertidaoRegularidadeFiscal()
    {
    }

    private CertidaoRegularidadeFiscal(
        CertidaoRegularidadeFiscalId id,
        Guid tenantId,
        ContribuinteId contribuinteId,
        string documento,
        string nomeContribuinte,
        string? inscricaoMunicipal,
        TipoCertidaoRegularidade tipo,
        DateOnly dataEmissao,
        DateOnly dataValidade,
        long numeroSequencial,
        string fundamentoLegal,
        string? observacao)
        : base(id)
    {
        TenantId = tenantId;
        ContribuinteId = contribuinteId;
        Documento = documento;
        NomeContribuinte = nomeContribuinte;
        InscricaoMunicipal = inscricaoMunicipal;
        Tipo = tipo;
        DataEmissao = dataEmissao;
        DataValidade = dataValidade;
        NumeroSequencial = numeroSequencial;
        Numero = MontarNumero(dataEmissao, numeroSequencial);
        FundamentoLegal = fundamentoLegal;
        Observacao = observacao;
        CodigoAutenticacao = CalcularCodigoAutenticacao();
        RaiseDomainEvent(new CertidaoRegularidadeEmitida(id, tenantId, contribuinteId, tipo, Numero));
    }

    /// <summary>Tenant (ente público) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Contribuinte a quem a certidão se refere.</summary>
    public ContribuinteId ContribuinteId { get; private set; }

    /// <summary>Documento (CPF/CNPJ) do contribuinte, sem máscara.</summary>
    public string Documento { get; private set; } = default!;

    /// <summary>Nome/razão social do contribuinte na data de emissão.</summary>
    public string NomeContribuinte { get; private set; } = default!;

    /// <summary>Inscrição municipal (mobiliária), quando houver.</summary>
    public string? InscricaoMunicipal { get; private set; }

    /// <summary>Resultado apurado: Negativa, Positiva-com-efeito-Negativa ou Positiva.</summary>
    public TipoCertidaoRegularidade Tipo { get; private set; }

    /// <summary>Número da certidão (sequencial por exercício de emissão).</summary>
    public string Numero { get; private set; } = default!;

    /// <summary>Número sequencial bruto da emissão no exercício.</summary>
    public long NumeroSequencial { get; private set; }

    /// <summary>Data de emissão (data do fato — afere a situação fiscal).</summary>
    public DateOnly DataEmissao { get; private set; }

    /// <summary>Data-limite de validade da certidão.</summary>
    public DateOnly DataValidade { get; private set; }

    /// <summary>Fundamento legal (CTN art. 205/206 + lei municipal).</summary>
    public string FundamentoLegal { get; private set; } = default!;

    /// <summary>Observação (ex.: detalhamento dos débitos suspensos na CPEN), opcional.</summary>
    public string? Observacao { get; private set; }

    /// <summary>
    /// Código de autenticação para conferência pública (hash determinístico dos dados essenciais).
    /// Permite ao terceiro (licitação/cartório) validar a autenticidade da certidão apresentada.
    /// </summary>
    public string CodigoAutenticacao { get; private set; } = default!;

    /// <summary>
    /// Emite a certidão de regularidade fiscal. O <paramref name="tipo"/> é o resultado da varredura
    /// de débitos do contribuinte (feita na Application). A validade é parametrizável (lei municipal).
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="contribuinteId">Contribuinte a quem se refere.</param>
    /// <param name="documento">CPF/CNPJ (somente dígitos).</param>
    /// <param name="nomeContribuinte">Nome/razão social.</param>
    /// <param name="inscricaoMunicipal">Inscrição municipal (opcional).</param>
    /// <param name="tipo">Resultado apurado (Negativa/CPEN/Positiva).</param>
    /// <param name="dataEmissao">Data de emissão (data do fato).</param>
    /// <param name="numeroSequencial">Sequencial da emissão no exercício (&gt;= 1).</param>
    /// <param name="fundamentoLegal">Fundamento legal (CTN + CTM).</param>
    /// <param name="diasValidade">Prazo de validade em dias (parametrizável); padrão 90.</param>
    /// <param name="observacao">Observação (opcional).</param>
    /// <returns>Nova <see cref="CertidaoRegularidadeFiscal"/>.</returns>
    public static CertidaoRegularidadeFiscal Emitir(
        Guid tenantId,
        ContribuinteId contribuinteId,
        string documento,
        string nomeContribuinte,
        string? inscricaoMunicipal,
        TipoCertidaoRegularidade tipo,
        DateOnly dataEmissao,
        long numeroSequencial,
        string fundamentoLegal,
        int diasValidade = DiasValidadePadrao,
        string? observacao = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        ArgumentException.ThrowIfNullOrWhiteSpace(nomeContribuinte);
        ArgumentException.ThrowIfNullOrWhiteSpace(fundamentoLegal);
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de certidão inválido.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(numeroSequencial, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(diasValidade, 1);

        return new CertidaoRegularidadeFiscal(
            CertidaoRegularidadeFiscalId.New(),
            tenantId,
            contribuinteId,
            documento.Trim(),
            nomeContribuinte.Trim(),
            string.IsNullOrWhiteSpace(inscricaoMunicipal) ? null : inscricaoMunicipal.Trim(),
            tipo,
            dataEmissao,
            dataEmissao.AddDays(diasValidade),
            numeroSequencial,
            fundamentoLegal.Trim(),
            string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim());
    }

    /// <summary>
    /// Indica se a certidão tem EFEITO de regularidade (Negativa ou Positiva-com-efeito-Negativa).
    /// </summary>
    public bool AtestaRegularidade => Tipo is TipoCertidaoRegularidade.Negativa or TipoCertidaoRegularidade.PositivaComEfeitoNegativa;

    /// <summary>
    /// Decide o TIPO da certidão a partir da situação fiscal apurada (CTN arts. 205/206): regra ÚNICA
    /// reusada por todos os pontos de emissão (balcão e autosserviço). Positiva se há débito EXIGÍVEL
    /// (lançamento vencido em aberto OU dívida ativa exigível); Positiva-com-efeito-de-Negativa (CPEN)
    /// se NÃO há exigível mas há débito SUSPENSO (parcelado — CTN art. 151, VI); Negativa (CND) se nada
    /// consta.
    /// </summary>
    /// <param name="lancamentosVencidosEmAberto">Qtde de lançamentos próprios vencidos e em aberto (exigíveis).</param>
    /// <param name="dividasAtivasExigiveis">Qtde de inscrições em dívida ativa exigíveis (não suspensas/extintas).</param>
    /// <param name="dividasAtivasSuspensas">Qtde de inscrições com exigibilidade suspensa (parceladas).</param>
    /// <returns>O tipo de certidão correspondente.</returns>
    public static TipoCertidaoRegularidade DecidirTipo(
        int lancamentosVencidosEmAberto,
        int dividasAtivasExigiveis,
        int dividasAtivasSuspensas)
    {
        if (lancamentosVencidosEmAberto > 0 || dividasAtivasExigiveis > 0)
        {
            return TipoCertidaoRegularidade.Positiva;
        }

        return dividasAtivasSuspensas > 0
            ? TipoCertidaoRegularidade.PositivaComEfeitoNegativa
            : TipoCertidaoRegularidade.Negativa;
    }

    /// <summary>Indica se a certidão está dentro do prazo de validade na data informada.</summary>
    /// <param name="referencia">Data de referência (informada — sem relógio no domínio).</param>
    /// <returns><c>true</c> se vigente.</returns>
    public bool EstaVigente(DateOnly referencia) => referencia <= DataValidade;

    /// <summary>
    /// Confere a autenticidade da certidão pelo código apresentado por um terceiro. A comparação é
    /// case-insensitive e em tempo fixo (anti-timing). Não vaza dados se o código não bater.
    /// </summary>
    /// <param name="codigoApresentado">Código apresentado para conferência.</param>
    /// <returns><c>true</c> se o código corresponde ao da certidão.</returns>
    public bool ConferirAutenticidade(string? codigoApresentado)
    {
        if (string.IsNullOrWhiteSpace(codigoApresentado))
        {
            return false;
        }

        var esperado = Encoding.UTF8.GetBytes(CodigoAutenticacao);
        var apresentado = Encoding.UTF8.GetBytes(codigoApresentado.Trim().ToUpperInvariant());
        return CryptographicOperations.FixedTimeEquals(esperado, apresentado);
    }

    /// <summary>
    /// Número da certidão: "CRF-AAAA-NNNNNNNN" (exercício de emissão + sequencial zero-paddeado).
    /// // TODO(validar-oficial): formato oficial do número da certidão conforme o CTM.
    /// </summary>
    private static string MontarNumero(DateOnly dataEmissao, long numeroSequencial)
        => string.Create(CultureInfo.InvariantCulture, $"CRF-{dataEmissao.Year:0000}-{numeroSequencial:00000000}");

    /// <summary>
    /// Código de autenticação determinístico: SHA-256 dos dados essenciais (tenant, documento, tipo,
    /// número, datas), em hexadecimal maiúsculo (16 primeiros caracteres). Determinístico e verificável.
    /// </summary>
    private string CalcularCodigoAutenticacao()
    {
        var material = string.Create(
            CultureInfo.InvariantCulture,
            $"{TenantId:N}|{Documento}|{(int)Tipo}|{Numero}|{DataEmissao:yyyy-MM-dd}|{DataValidade:yyyy-MM-dd}");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash)[..16];
    }
}
