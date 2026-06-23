using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Contracts;

/// <summary>
/// Evento de integracao publico (ponte RH -> Transparencia): expoe um RESUMO da folha fechada de uma
/// competencia — servidores (cadastro TCE_4820), rubricas (TCE_4960) e lancamentos/valores por servidor
/// (TCE_4810) — para que a Transparencia monte a REMESSA DE FOLHA ao TCE-RS (Resolucao 1099/2018,
/// SIAPC/PAD Volume V). Espelha o padrao da MSC (Financas -> Transparencia): a Transparencia e CONSUMIDORA
/// e nao acessa o interno do RH; recebe apenas este snapshot via Contracts. Idempotente por
/// <c>EventId</c>/competencia no consumidor.
/// </summary>
/// <remarks>
/// O snapshot e DESNORMALIZADO de proposito: carrega tudo o que os tres arquivos posicionais precisam,
/// sem expor agregados do RH. Os codigos/campos exatos do leiaute de folha (Res. 1099 / MT SIAPC Vol. V)
/// sao materializados na Transparencia com <c>// TODO(validar-leiaute-folha-1099)</c>.
/// </remarks>
/// <param name="EventId">Identificador unico do evento.</param>
/// <param name="OccurredOnUtc">Momento (UTC) de ocorrencia.</param>
/// <param name="TenantId">Tenant (ente publico) dono do registro.</param>
/// <param name="FolhaDePagamentoId">Identificador da folha fechada de origem.</param>
/// <param name="Competencia">Competencia de referencia (<c>AAAA-MM</c>).</param>
/// <param name="TipoFolha">Tipo da folha (<c>Mensal</c>/<c>DecimoTerceiro</c>/<c>Ferias</c>/<c>Rescisao</c>).</param>
/// <param name="DataPagamento">Data de pagamento da folha (nula se ainda nao paga) — TCE_4810 campo 4.</param>
/// <param name="Servidores">Cadastro dos servidores da competencia (TCE_4820).</param>
/// <param name="Rubricas">Tabela de vantagens/descontos/totalizadores (TCE_4960).</param>
/// <param name="Lancamentos">Lancamentos (vantagem/desconto/totalizador) por servidor (TCE_4810).</param>
public sealed record FolhaResumoRemessaTceIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid TenantId,
    Guid FolhaDePagamentoId,
    string Competencia,
    string TipoFolha,
    DateOnly? DataPagamento,
    IReadOnlyList<ServidorFolhaTceDto> Servidores,
    IReadOnlyList<RubricaFolhaTceDto> Rubricas,
    IReadOnlyList<LancamentoFolhaTceDto> Lancamentos) : IntegrationEvent(EventId, OccurredOnUtc);

/// <summary>
/// Servidor da folha para o cadastro TCE_4820 (Resolucao 1099 / MT SIAPC Vol. V §3.1.2). Strings/primitivos
/// para nao vazar tipos do dominio do RH entre modulos. Campos opcionais ficam vazios/nulos quando o ente
/// ainda nao tiver o dado (a Transparencia trata na pre-validacao local). // TODO(validar-leiaute-folha-1099).
/// </summary>
/// <param name="CodigoRegistro">Codigo de registro do funcionario (FK p/ TCE_4810; codificacao propria).</param>
/// <param name="Cpf">CPF (somente digitos).</param>
/// <param name="Nome">Nome civil do servidor.</param>
/// <param name="Matricula">Matricula (com sufixo de vinculo).</param>
/// <param name="DataNascimento">Data de nascimento (nula se ausente).</param>
/// <param name="DataAdmissao">Data de admissao/nomeacao.</param>
/// <param name="DataDemissao">Data de demissao/desligamento (nula se ativo).</param>
/// <param name="CodigoCargo">Codigo do cargo.</param>
/// <param name="NomeCargo">Nome do cargo.</param>
/// <param name="Regime">Regime previdenciario (<c>Rpps</c>/<c>Rgps</c>).</param>
public sealed record ServidorFolhaTceDto(
    string CodigoRegistro,
    string Cpf,
    string Nome,
    string Matricula,
    DateOnly? DataNascimento,
    DateOnly? DataAdmissao,
    DateOnly? DataDemissao,
    string CodigoCargo,
    string NomeCargo,
    string Regime);

/// <summary>
/// Rubrica (vantagem/desconto/totalizador) da folha para a tabela TCE_4960 (MT SIAPC Vol. V §3.1.3).
/// </summary>
/// <param name="Codigo">Codigo da rubrica (S-1010; FK p/ TCE_4810).</param>
/// <param name="Descricao">Nome/descricao da vantagem/desconto/totalizador.</param>
/// <param name="Operacao">Identificacao da operacao: <c>V</c>=Vantagem, <c>D</c>=Desconto, <c>T</c>=Totalizador, <c>O</c>=Outros.</param>
/// <param name="IncideIrrf">Indicador de incidencia do IRRF.</param>
/// <param name="IncideRpps">Indicador de incidencia do RPPS.</param>
/// <param name="IncideInss">Indicador de incidencia do INSS.</param>
/// <param name="BaseLegal">Resumo da base legal (TCE_4960 — ate 150 caracteres). // TODO(validar-leiaute-folha-1099).</param>
/// <param name="ContaPlanoFolhaTce">Codigo da conta do Plano de Contas da Folha (codificacao TCE — 6). // TODO(validar-leiaute-folha-1099).</param>
public sealed record RubricaFolhaTceDto(
    string Codigo,
    string Descricao,
    string Operacao,
    bool IncideIrrf,
    bool IncideRpps,
    bool IncideInss,
    string BaseLegal,
    string ContaPlanoFolhaTce);

/// <summary>
/// Lancamento (vantagem/desconto/totalizador) de um servidor numa folha, para o arquivo TCE_4810
/// (MT SIAPC Vol. V §3.1.1).
/// </summary>
/// <param name="CodigoRegistroServidor">Codigo de registro do funcionario (FK p/ TCE_4820).</param>
/// <param name="CodigoRubrica">Codigo da vantagem/desconto/totalizador (FK p/ TCE_4960).</param>
/// <param name="Operacao">Identificacao da operacao: <c>V</c>/<c>D</c>/<c>T</c>/<c>O</c>.</param>
/// <param name="Valor">Valor da vantagem/desconto/totalizador (sempre &gt;= 0; o sinal vem da operacao).</param>
public sealed record LancamentoFolhaTceDto(
    string CodigoRegistroServidor,
    string CodigoRubrica,
    string Operacao,
    decimal Valor);
