namespace Tensorroot.Gov.Modules.Financas.Domain.Cnab;

/// <summary>Tipo de inscrição do CNAB (1 = CPF, 2 = CNPJ).</summary>
public enum TipoInscricaoCnab
{
    /// <summary>Pessoa física (CPF).</summary>
    Cpf = 1,

    /// <summary>Pessoa jurídica (CNPJ).</summary>
    Cnpj = 2,
}

/// <summary>Forma de lançamento do lote (FEBRABAN, header de lote campo 11).</summary>
public enum FormaLancamentoCnab
{
    /// <summary>Crédito em conta corrente no mesmo banco.</summary>
    CreditoContaCorrente = 1,

    /// <summary>DOC/TED — outra titularidade.</summary>
    DocTed = 3,

    /// <summary>PIX transferência.</summary>
    PixTransferencia = 45,

    /// <summary>Crédito em conta poupança.</summary>
    CreditoPoupanca = 5,
}

/// <summary>
/// Dados do pagador (empresa/ente) — cabeçalho do arquivo e do lote CNAB240.
/// </summary>
/// <param name="CodigoBanco">Código COMPE do banco (3 dígitos).</param>
/// <param name="TipoInscricao">Tipo de inscrição do ente.</param>
/// <param name="NumeroInscricao">CNPJ/CPF do ente (somente dígitos).</param>
/// <param name="Convenio">Código do convênio com o banco.</param>
/// <param name="Agencia">Agência (somente dígitos).</param>
/// <param name="DvAgencia">Dígito da agência.</param>
/// <param name="Conta">Conta (somente dígitos).</param>
/// <param name="DvConta">Dígito da conta.</param>
/// <param name="DvAgenciaConta">Dígito agência/conta.</param>
/// <param name="NomeEmpresa">Nome do ente pagador.</param>
public sealed record PagadorCnab(
    string CodigoBanco,
    TipoInscricaoCnab TipoInscricao,
    string NumeroInscricao,
    string Convenio,
    string Agencia,
    string DvAgencia,
    string Conta,
    string DvConta,
    string DvAgenciaConta,
    string NomeEmpresa);

/// <summary>
/// Favorecido de um pagamento (linha de detalhe — Segmento A/B do CNAB240).
/// </summary>
/// <param name="CodigoBancoFavorecido">Código COMPE do banco do favorecido.</param>
/// <param name="AgenciaFavorecido">Agência do favorecido.</param>
/// <param name="DvAgenciaFavorecido">Dígito da agência.</param>
/// <param name="ContaFavorecido">Conta do favorecido.</param>
/// <param name="DvContaFavorecido">Dígito da conta.</param>
/// <param name="NomeFavorecido">Nome do favorecido.</param>
/// <param name="TipoInscricaoFavorecido">Tipo de inscrição do favorecido.</param>
/// <param name="NumeroInscricaoFavorecido">CNPJ/CPF do favorecido.</param>
/// <param name="NumeroDocumento">Número do documento/ordem (controle do ente).</param>
/// <param name="DataPagamento">Data do pagamento.</param>
/// <param name="Valor">Valor a creditar (líquido).</param>
/// <param name="ChavePix">Chave PIX (opcional — usada quando forma = PIX).</param>
public sealed record FavorecidoCnab(
    string CodigoBancoFavorecido,
    string AgenciaFavorecido,
    string DvAgenciaFavorecido,
    string ContaFavorecido,
    string DvContaFavorecido,
    string NomeFavorecido,
    TipoInscricaoCnab TipoInscricaoFavorecido,
    string NumeroInscricaoFavorecido,
    string NumeroDocumento,
    DateOnly DataPagamento,
    decimal Valor,
    string? ChavePix = null);

/// <summary>
/// Remessa de pagamento CNAB240 (FEBRABAN): um arquivo com um lote de pagamentos a fornecedores/servidores
/// a partir de uma ordem de pagamento. Modela a estrutura completa (header de arquivo/lote, Segmentos A e B,
/// trailers) — a TRANSMISSÃO real ao banco é diferida ao M10.
/// </summary>
/// <param name="Pagador">Dados do ente pagador.</param>
/// <param name="FormaLancamento">Forma de lançamento do lote.</param>
/// <param name="DataGeracao">Data da geração do arquivo.</param>
/// <param name="HoraGeracao">Hora da geração.</param>
/// <param name="SequencialArquivo">Sequencial do arquivo (controle do ente).</param>
/// <param name="Favorecidos">Favorecidos do lote.</param>
public sealed record RemessaCnab240(
    PagadorCnab Pagador,
    FormaLancamentoCnab FormaLancamento,
    DateOnly DataGeracao,
    TimeOnly HoraGeracao,
    int SequencialArquivo,
    IReadOnlyList<FavorecidoCnab> Favorecidos);
