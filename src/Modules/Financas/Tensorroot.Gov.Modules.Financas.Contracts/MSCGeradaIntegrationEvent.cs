using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Financas.Contracts;

/// <summary>
/// Informacoes complementares (atributos) de uma linha da Matriz de Saldos Contabeis (MSC), conforme o
/// quadro da MSC SICONFI (Anexo II Portaria STN 642/2019). Todos os campos sao opcionais: no M3 apenas
/// <see cref="PoderOrgao"/> (do tenant) e <see cref="AtributoSuperavitFinanceiro"/> (da conta) sao
/// preenchidos; os demais dependem de atributos (Fonte de Recurso, Natureza de Despesa/Receita, Funcao/
/// Subfuncao, RP) que o ciclo orcamentario ainda nao carrega na partida contabil — serao preenchidos em
/// M3.x/M4 sem nova versao de contrato. // TODO(validar-oficial): codigos e tamanhos exatos das tabelas.
/// </summary>
/// <param name="PoderOrgao">Poder/Orgao (PO, 5 digitos). [validar-oficial] tabela PO.</param>
/// <param name="AtributoSuperavitFinanceiro">Atributo Superavit Financeiro (FP: 1=F, 2=P). Classes 1/2.</param>
/// <param name="DividaConsolidada">Divida Consolidada (DC, 1 digito). // TODO(validar-oficial).</param>
/// <param name="FonteRecurso">Fonte/Destinacao de Recurso (FR, 4 digitos). [validar-oficial] Port. 710/2021.</param>
/// <param name="CodigoAcompanhamento">Cod. Acompanhamento Exec. Orcam. (CO, 4 digitos). // TODO(validar-oficial).</param>
/// <param name="NaturezaReceita">Natureza da Receita (NR, 8 digitos). [validar-oficial] Port. 163/2001.</param>
/// <param name="NaturezaDespesa">Natureza da Despesa (ND, 8 digitos). [validar-oficial] Port. 163/2001.</param>
/// <param name="FuncaoSubfuncao">Funcao + Subfuncao (FS, 5 digitos). [validar-oficial] Port. MOG 42/1999.</param>
/// <param name="AnoInscricaoRp">Ano de Inscricao em Restos a Pagar (AI, 4 digitos). M3.x (RP).</param>
public sealed record InformacoesComplementaresMscDto(
    string? PoderOrgao,
    string? AtributoSuperavitFinanceiro,
    string? DividaConsolidada,
    string? FonteRecurso,
    string? CodigoAcompanhamento,
    string? NaturezaReceita,
    string? NaturezaDespesa,
    string? FuncaoSubfuncao,
    string? AnoInscricaoRp);

/// <summary>
/// Linha de saldo contabil (PCASP) da Matriz de Saldos Contabeis gerada por Financas. Cada linha e uma
/// conta analitica + um tipo de valor (saldo inicial / movimento / saldo final) + a natureza do saldo do
/// registro + o valor (sempre &gt;= 0) + as informacoes complementares.
/// </summary>
/// <param name="ContaPcasp">Conta do PCASP (codigo, &lt;= 30 chars).</param>
/// <param name="NaturezaSaldo">Natureza do saldo do registro (1 = Devedor, 2 = Credor).</param>
/// <param name="TipoValor">Tipo do valor (1 = SaldoInicial, 2 = Movimento, 3 = SaldoFinal).</param>
/// <param name="Valor">Valor do saldo (sempre &gt;= 0; MSC nao admite negativos).</param>
/// <param name="InformacaoComplementar">Representacao textual das informacoes complementares (compat.).</param>
/// <param name="Complementares">Informacoes complementares estruturadas (campos opcionais).</param>
public sealed record LinhaMscDto(
    string ContaPcasp,
    int NaturezaSaldo,
    int TipoValor,
    decimal Valor,
    string? InformacaoComplementar,
    InformacoesComplementaresMscDto? Complementares);

/// <summary>
/// Evento de integracao publicado pelo modulo Financas quando a Matriz de Saldos Contabeis (MSC) de uma
/// competencia e gerada a partir do balancete (PCASP/MCASP). Transparencia o consome para consolidar a
/// <c>DeclaracaoFiscal</c> do periodo. Idempotente por <c>(TenantId, Exercicio, Mes)</c> (I-13).
/// </summary>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="Exercicio">Ano de exercicio.</param>
/// <param name="Mes">Mes da competencia da MSC (1-12; 13 reservado a Encerramento — M3.x).</param>
/// <param name="TipoMatriz">Tipo da matriz (1 = Agregada, 2 = Encerramento).</param>
/// <param name="Linhas">Saldos contabeis (PCASP) da competencia.</param>
public sealed record MSCGeradaIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    int Exercicio,
    int Mes,
    int TipoMatriz,
    IReadOnlyList<LinhaMscDto> Linhas) : IntegrationEvent(EventId, OccurredOnUtc);
