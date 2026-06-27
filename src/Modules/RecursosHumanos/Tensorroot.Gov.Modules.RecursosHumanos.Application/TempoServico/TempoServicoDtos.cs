using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TempoServico;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.TempoServico;

/// <summary>
/// Periodo averbado informado na emissao de uma certidao (tempo de outro orgao/regime). O efetivo exercicio
/// proprio NAO entra aqui — e' apurado automaticamente do vinculo do servidor pelo handler.
/// </summary>
/// <param name="Inicio">Inicio do periodo (inclusivo).</param>
/// <param name="Fim">Fim do periodo (inclusivo).</param>
/// <param name="RegimeOrigem">Regime previdenciario de origem.</param>
/// <param name="Origem">Orgao/certidao de origem.</param>
/// <param name="DiasNaoComputaveis">Dias nao-computaveis sobrepostos a abater (opcional; default 0).</param>
/// <param name="Fator">Fator de conversao (>= 1,0; default comum).</param>
/// <param name="Observacao">Observacao do periodo (opcional).</param>
public sealed record PeriodoAverbadoInput(
    DateOnly Inicio,
    DateOnly Fim,
    RegimeOrigemPeriodo RegimeOrigem,
    string Origem,
    int DiasNaoComputaveis = 0,
    decimal Fator = 1.0m,
    string? Observacao = null);

/// <summary>Projecao de um periodo computado na certidao (linha do documento).</summary>
/// <param name="Inicio">Inicio do periodo.</param>
/// <param name="Fim">Fim do periodo.</param>
/// <param name="Natureza">Natureza (efetivo exercicio proprio ou averbado).</param>
/// <param name="RegimeOrigem">Regime de origem (quando averbado).</param>
/// <param name="Origem">Orgao/certidao de origem (quando averbado).</param>
/// <param name="DiasBrutos">Dias brutos do intervalo.</param>
/// <param name="DiasNaoComputaveis">Dias nao-computaveis abatidos.</param>
/// <param name="DiasLiquidos">Dias liquidos (brutos menos nao-computaveis).</param>
/// <param name="Fator">Fator de conversao aplicado.</param>
/// <param name="DiasEquivalentes">Dias equivalentes (apos fator) que entram no total.</param>
/// <param name="Observacao">Observacao do periodo.</param>
public sealed record PeriodoTempoDto(
    DateOnly Inicio,
    DateOnly Fim,
    NaturezaPeriodo Natureza,
    RegimeOrigemPeriodo? RegimeOrigem,
    string? Origem,
    int DiasBrutos,
    int DiasNaoComputaveis,
    int DiasLiquidos,
    decimal Fator,
    int DiasEquivalentes,
    string? Observacao);

/// <summary>Resumo de uma certidao (lista/ficha).</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Numero">Numeracao oficial formatada (NNNN/AAAA).</param>
/// <param name="Finalidade">Finalidade da certidao.</param>
/// <param name="DataEmissao">Data de emissao.</param>
/// <param name="Situacao">Situacao (emitida/anulada).</param>
/// <param name="TotalDias">Total de dias equivalentes certificados.</param>
/// <param name="TempoFormatado">Tempo total em anos/meses/dias.</param>
/// <param name="CodigoAutenticacao">Codigo de autenticacao (validacao publica).</param>
public sealed record CertidaoResumoDto(
    Guid Id,
    string Numero,
    FinalidadeCertidao Finalidade,
    DateOnly DataEmissao,
    SituacaoCertidao Situacao,
    int TotalDias,
    string TempoFormatado,
    string CodigoAutenticacao);

/// <summary>Detalhe completo de uma certidao (documento), incluindo os periodos computados.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="ServidorId">Servidor certificado.</param>
/// <param name="ServidorNome">Nome do servidor.</param>
/// <param name="Matricula">Matricula do servidor.</param>
/// <param name="CpfMascarado">CPF mascarado (LGPD).</param>
/// <param name="Numero">Numeracao oficial formatada (NNNN/AAAA).</param>
/// <param name="Finalidade">Finalidade da certidao.</param>
/// <param name="FinalidadeDescrita">Texto descritivo da finalidade.</param>
/// <param name="DataEmissao">Data de emissao.</param>
/// <param name="OrgaoEmissor">Orgao/setor emissor.</param>
/// <param name="Situacao">Situacao (emitida/anulada).</param>
/// <param name="MotivoAnulacao">Motivo da anulacao (quando anulada).</param>
/// <param name="Observacao">Observacao geral.</param>
/// <param name="CodigoAutenticacao">Codigo de autenticacao (validacao publica).</param>
/// <param name="TotalDias">Total de dias equivalentes certificados.</param>
/// <param name="TotalDiasLiquidos">Total de dias liquidos (sem fator).</param>
/// <param name="Anos">Componente de anos do tempo total.</param>
/// <param name="Meses">Componente de meses do tempo total.</param>
/// <param name="Dias">Componente de dias do tempo total.</param>
/// <param name="TempoFormatado">Tempo total em anos/meses/dias.</param>
/// <param name="Periodos">Periodos computados.</param>
public sealed record CertidaoDetalheDto(
    Guid Id,
    Guid ServidorId,
    string ServidorNome,
    string Matricula,
    string CpfMascarado,
    string Numero,
    FinalidadeCertidao Finalidade,
    string? FinalidadeDescrita,
    DateOnly DataEmissao,
    string OrgaoEmissor,
    SituacaoCertidao Situacao,
    string? MotivoAnulacao,
    string? Observacao,
    string CodigoAutenticacao,
    int TotalDias,
    int TotalDiasLiquidos,
    int Anos,
    int Meses,
    int Dias,
    string TempoFormatado,
    IReadOnlyList<PeriodoTempoDto> Periodos);

/// <summary>
/// Resultado da VALIDACAO PUBLICA de uma certidao por codigo de autenticacao: confirma (ou nao) a existencia
/// de uma certidao VIGENTE com aquele codigo, expondo apenas o minimo necessario para conferencia (LGPD —
/// sem dados sensiveis alem do nome e do total certificado).
/// </summary>
/// <param name="Valida"><c>true</c> se ha certidao vigente com o codigo.</param>
/// <param name="Numero">Numeracao oficial (quando valida).</param>
/// <param name="ServidorNome">Nome do servidor (quando valida).</param>
/// <param name="Finalidade">Finalidade (quando valida).</param>
/// <param name="DataEmissao">Data de emissao (quando valida).</param>
/// <param name="TempoFormatado">Tempo total certificado (quando valida).</param>
public sealed record ValidacaoCertidaoDto(
    bool Valida,
    string? Numero,
    string? ServidorNome,
    FinalidadeCertidao? Finalidade,
    DateOnly? DataEmissao,
    string? TempoFormatado);
