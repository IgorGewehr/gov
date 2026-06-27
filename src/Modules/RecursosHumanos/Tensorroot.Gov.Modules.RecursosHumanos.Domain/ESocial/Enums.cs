namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;

/// <summary>
/// Tipo de evento eSocial gerado/transmitido por este modulo. Os codigos seguem a nomenclatura
/// oficial dos leiautes S-1.3 (NT 06/2026). // TODO(validar-oficial): roster/codigos exatos da versao
/// travada do XSD (ESOCIAL-SPEC §1.0).
/// </summary>
public enum TipoEventoESocial
{
    /// <summary>S-1000 — Informacoes do empregador/orgao publico (evento de tabela, 1o de tudo).</summary>
    S1000Empregador = 1000,

    /// <summary>S-1005 — Tabela de estabelecimentos/unidades.</summary>
    S1005Estabelecimento = 1005,

    /// <summary>S-1010 — Tabela de rubricas (mapeada das nossas RubricaFolha).</summary>
    S1010Rubrica = 1010,

    /// <summary>S-2200 — Admissao/cadastramento inicial do vinculo (do agregado Servidor).</summary>
    S2200Admissao = 2200,

    /// <summary>S-2299 — Desligamento do vinculo.</summary>
    S2299Desligamento = 2299,

    /// <summary>S-2210 — Comunicacao de Acidente de Trabalho (CAT — SST).</summary>
    S2210Cat = 2210,

    /// <summary>S-2220 — Monitoramento da Saude do Trabalhador (ASO — SST).</summary>
    S2220MonitoramentoSaude = 2220,

    /// <summary>S-2240 — Condicoes Ambientais do Trabalho / Agentes Nocivos (SST).</summary>
    S2240AgentesNocivos = 2240,

    /// <summary>S-1200 — Remuneracao RGPS (da nossa folha fechada).</summary>
    S1200Remuneracao = 1200,

    /// <summary>S-1202 — Remuneracao de servidor vinculado a RPPS.</summary>
    S1202RemuneracaoRpps = 1202,

    /// <summary>S-1210 — Pagamentos de rendimentos do trabalho.</summary>
    S1210Pagamentos = 1210,

    /// <summary>S-1299 — Fechamento dos eventos periodicos da competencia.</summary>
    S1299Fechamento = 1299,
}

/// <summary>
/// Estado do <see cref="EventoESocial"/> na maquina de estados (ESOCIAL-SPEC §4.4). Transicoes:
/// <c>Gerado -> Assinado -> Transmitido -> Processado/Rejeitado</c>, com <c>RejeitadoLocal</c> para
/// falha de validacao antes da transmissao (nao gasta cota).
/// </summary>
public enum EstadoEventoESocial
{
    /// <summary>XML gerado a partir do dominio; ainda nao assinado (estado inicial).</summary>
    Gerado = 1,

    /// <summary>XML assinado (XML-DSig A1 via Cofre); pronto para empacotamento/envio.</summary>
    Assinado = 2,

    /// <summary>Lote enviado; protocolo de envio recebido; aguardando recibo por evento.</summary>
    Transmitido = 3,

    /// <summary>Processado e aceito pelo eSocial; recibo (nrRecibo) persistido (terminal de sucesso).</summary>
    Processado = 4,

    /// <summary>Rejeitado pelo eSocial no processamento do lote (erros por evento); re-gerável apos correcao.</summary>
    Rejeitado = 5,

    /// <summary>Rejeitado localmente (XSD/estrutura invalida) antes de transmitir; nao gastou cota.</summary>
    RejeitadoLocal = 6,
}

/// <summary>
/// Tipo de inscricao do declarante/estabelecimento (campo <c>tpInsc</c> dos leiautes).
/// // TODO(validar-oficial): dominio completo do tpInsc no XSD da versao travada.
/// </summary>
public enum TipoInscricao
{
    /// <summary>CNPJ (ente publico/estabelecimento com CNPJ).</summary>
    Cnpj = 1,

    /// <summary>CPF.</summary>
    Cpf = 2,
}

/// <summary>
/// Ambiente de transmissao (campo <c>tpAmb</c> do grupo <c>ideEvento</c>).
/// // TODO(validar-oficial): valores exatos no XSD (tipicamente 1=Producao, 2=Producao Restrita).
/// </summary>
public enum AmbienteESocial
{
    /// <summary>Producao (efeito juridico).</summary>
    Producao = 1,

    /// <summary>Producao Restrita (homologacao, sem efeito juridico — ESOCIAL-SPEC §3).</summary>
    ProducaoRestrita = 2,
}
