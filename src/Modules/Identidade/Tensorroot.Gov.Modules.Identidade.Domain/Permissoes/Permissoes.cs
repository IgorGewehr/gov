using System.Collections.Frozen;

namespace Tensorroot.Gov.Modules.Identidade.Domain.Permissoes;

/// <summary>
/// Catalogo canonico e imutavel das permissoes (RBAC) do Tensorroot.Gov. As constantes sao a
/// unica fonte de verdade dos escopos atribuiveis a um <see cref="Papeis.Papel"/>. Negar por padrao:
/// nenhuma permissao fora deste catalogo deve ser persistida (ver <see cref="EhConhecida"/>).
/// </summary>
public static class Permissoes
{
    // --- Modulos de negocio (par "ver" / "gerenciar" por modulo) ---

    /// <summary>Visualizar dados do modulo Administracao (compras, licitacoes, contratos).</summary>
    public const string AdministracaoVer = "administracao.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Administracao.</summary>
    public const string AdministracaoGerenciar = "administracao.gerenciar";

    /// <summary>Visualizar dados do modulo Financas (orcamento, empenho, contabilidade).</summary>
    public const string FinancasVer = "financas.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Financas.</summary>
    public const string FinancasGerenciar = "financas.gerenciar";

    /// <summary>
    /// Planejar o orcamento (PPA/LDO/LOA + creditos adicionais). Verbo SEPARADO da execucao
    /// (<see cref="FinancasGerenciar"/>) — segregacao de funcoes exigida pelo controle interno/TCE:
    /// quem planeja nao necessariamente executa. TODO(validar-oficial: matriz SoD/alcada do tenant).
    /// </summary>
    public const string FinancasPlanejar = "financas.planejar";

    /// <summary>Visualizar dados do modulo Tributos (IPTU, ISS, ITBI, Divida Ativa).</summary>
    public const string TributosVer = "tributos.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Tributos.</summary>
    public const string TributosGerenciar = "tributos.gerenciar";

    /// <summary>
    /// Instaurar/conduzir o processo de arbitramento da base de calculo do ITBI (CTN art. 148) —
    /// afastar a presuncao do valor declarado (Tema 1.113/STJ). Verbo fino de Segregacao de Funcoes:
    /// somente a autoridade lancadora pode arbitrar, separado da gestao geral do modulo.
    /// TODO(a confirmar: matriz SoD/procuradoria do tenant).
    /// </summary>
    public const string TributosItbiArbitrar = "tributos.itbi.arbitrar";

    /// <summary>Visualizar dados do modulo Recursos Humanos (folha, cargos, ponto).</summary>
    public const string RecursosHumanosVer = "recursoshumanos.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Recursos Humanos.</summary>
    public const string RecursosHumanosGerenciar = "recursoshumanos.gerenciar";

    /// <summary>Consultar consignacoes/margem consignavel (Onda 2 — Lei 14.131/2021).</summary>
    public const string RhConsignacaoVer = "rh.consignacao.ver";

    /// <summary>Gerenciar consignatarias/rubricas/consignacoes (suspender/reativar/cancelar).</summary>
    public const string RhConsignacaoGerenciar = "rh.consignacao.gerenciar";

    /// <summary>Averbar uma consignacao (verbo fino: respeita a margem do balde na competencia).</summary>
    public const string RhConsignacaoAverbar = "rh.consignacao.averbar";

    /// <summary>
    /// AUTOSSERVICO DO SERVIDOR ("Minha Folha"): permite ao PROPRIO servidor consultar SOMENTE os
    /// SEUS dados pessoais (contracheque, espelho de ponto, ferias, informe de rendimentos). Verbo
    /// DELIBERADAMENTE separado de <see cref="RecursosHumanosVer"/> (que e o "ver de todos", de
    /// gestor de RH) e, por isso, FORA de <see cref="Todas"/>: o papel "Servidor" recebe APENAS este
    /// escopo e NUNCA enxerga dados de terceiros. O endpoint resolve o ServidorId do PROPRIO usuario
    /// autenticado (nunca aceita um servidorId arbitrario do cliente) — ABAC dado-proprio a prova de
    /// bala. LG: contracheque/ponto sao dados pessoais (LGPD art. 5); todo acesso gera trilha (LG-2).
    /// </summary>
    public const string AutosservicoProprio = "autosservico.proprio";

    /// <summary>Visualizar dados do modulo Patrimonio (bens, frota, almoxarifado).</summary>
    public const string PatrimonioVer = "patrimonio.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Patrimonio.</summary>
    public const string PatrimonioGerenciar = "patrimonio.gerenciar";

    /// <summary>Visualizar dados do modulo Protocolo (processo eletronico, GED).</summary>
    public const string ProtocoloVer = "protocolo.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Protocolo.</summary>
    public const string ProtocoloGerenciar = "protocolo.gerenciar";

    /// <summary>
    /// Visualizar dados MINIMIZADOS do modulo Saude (listagens, agregados, metadados). LG-A3: NAO
    /// libera por si o conteudo clinico sigiloso (PEP, historico de CID-10/alergias, evolucoes SOAP)
    /// — esse conteudo exige o verbo fino <see cref="SaudeProntuarioLer"/>. Assim, ver o agregado
    /// nao expoe o prontuario sensivel sem o verbo de leitura clinica.
    /// </summary>
    public const string SaudeVer = "saude.ver";

    /// <summary>
    /// Ler o CONTEUDO CLINICO sigiloso do paciente (LGPD art. 11): historico clinico (CID-10/CIAP,
    /// alergias, condicoes cronicas), detalhe/evolucoes de atendimento e dados identificaveis por
    /// CNS. Verbo FINO de Segregacao de Funcoes (LG-A3) separado de <see cref="SaudeVer"/>: toda
    /// leitura assim gated tambem gera trilha de acesso LGPD (LG-2) com base legal (LG-A2).
    /// </summary>
    public const string SaudeProntuarioLer = "saude.prontuario.ler";

    /// <summary>Gerenciar (mutar) dados do modulo Saude.</summary>
    public const string SaudeGerenciar = "saude.gerenciar";

    /// <summary>Consultar a agenda/vagas/agendamentos (Onda 2 — agendamento de consultas/exames).</summary>
    public const string SaudeAgendaVer = "saude.agenda.ver";

    /// <summary>Gerenciar grades de agenda (publicar/bloquear/reabrir dia).</summary>
    public const string SaudeAgendaGerenciar = "saude.agenda.gerenciar";

    /// <summary>Marcar/confirmar/cancelar/registrar falta/realizar agendamentos e fila de espera.</summary>
    public const string SaudeAgendaMarcar = "saude.agenda.marcar";

    /// <summary>Consultar catalogo de medicamentos e posicao de estoque da Farmacia (Onda 3c).</summary>
    public const string SaudeFarmaciaVer = "saude.farmacia.ver";

    /// <summary>Gerenciar Farmacia: catalogo de medicamentos e entradas de estoque (lote/validade).</summary>
    public const string SaudeFarmaciaGerenciar = "saude.farmacia.gerenciar";

    /// <summary>Dispensar/estornar medicamentos ao paciente (baixa de estoque — Onda 3c).</summary>
    public const string SaudeFarmaciaDispensar = "saude.farmacia.dispensar";

    /// <summary>Consultar catalogo de imunobiologicos do PNI (Onda 3c).</summary>
    public const string SaudeImunizacaoVer = "saude.imunizacao.ver";

    /// <summary>Gerenciar o catalogo de imunobiologicos do PNI (Onda 3c).</summary>
    public const string SaudeImunizacaoGerenciar = "saude.imunizacao.gerenciar";

    /// <summary>Registrar a aplicacao de dose de vacina e o aprazamento da proxima (Onda 3c).</summary>
    public const string SaudeImunizacaoAplicar = "saude.imunizacao.aplicar";

    /// <summary>Visualizar dados do modulo Educacao (escolas, matriculas, merenda).</summary>
    public const string EducacaoVer = "educacao.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Educacao.</summary>
    public const string EducacaoGerenciar = "educacao.gerenciar";

    /// <summary>
    /// Visualizar dados MINIMIZADOS do modulo Assistencia Social (listagens territoriais, agregados
    /// de beneficios). LG-A3: NAO libera por si o conteudo sigiloso do prontuario SUAS (atendimentos,
    /// violacoes envolvendo crianca/adolescente) nem o resumo CadUnico (NIS/renda) — esse conteudo
    /// exige o verbo fino <see cref="AssistenciaSocialProntuarioLer"/>.
    /// </summary>
    public const string AssistenciaSocialVer = "assistenciasocial.ver";

    /// <summary>
    /// Ler o CONTEUDO SIGILOSO do prontuario SUAS e do CadUnico (LGPD art. 11; ECA quando ha
    /// violacao contra crianca/adolescente): detalhe do prontuario, trilha de acesso e resumo
    /// CadUnico (NIS/renda). Verbo FINO de Segregacao de Funcoes (LG-A3) separado de
    /// <see cref="AssistenciaSocialVer"/>; leituras assim gated geram trilha de acesso LGPD (LG-1/2)
    /// com base legal (LG-A2).
    /// </summary>
    public const string AssistenciaSocialProntuarioLer = "assistenciasocial.prontuario.ler";

    /// <summary>Gerenciar (mutar) dados do modulo Assistencia Social.</summary>
    public const string AssistenciaSocialGerenciar = "assistenciasocial.gerenciar";

    /// <summary>Visualizar dados do modulo Legislativo (sessoes, proposicoes, votacao).</summary>
    public const string LegislativoVer = "legislativo.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Legislativo.</summary>
    public const string LegislativoGerenciar = "legislativo.gerenciar";

    /// <summary>Gerenciar o cadastro de vereadores/parlamentares (criar/editar/situacao de mandato).</summary>
    public const string LegislativoVereadoresGerenciar = "legislativo.vereadores.gerenciar";

    /// <summary>Semear o cenario de demonstracao do modulo Legislativo (dados de exemplo por tenant).</summary>
    public const string LegislativoDemoSemear = "legislativo.demo.semear";

    /// <summary>Gerenciar a base de normas juridicas (cadastrar/revogar/alterar leis, decretos, resolucoes).</summary>
    public const string LegislativoNormasGerenciar = "legislativo.normas.gerenciar";

    /// <summary>Montar edicoes e materias do Diario Oficial Eletronico (sem efeito legal de publicacao).</summary>
    public const string LegislativoDiarioGerenciar = "legislativo.diario.gerenciar";

    /// <summary>
    /// Publicar a edicao do Diario Oficial (ato com efeito legal de eficacia dos atos) — verbo fino de
    /// Segregacao de Funcoes, separado de quem monta a edicao.
    /// </summary>
    public const string LegislativoDiarioPublicar = "legislativo.diario.publicar";

    /// <summary>
    /// Controlar o cronometro da tribuna (iniciar/pausar/retomar/encerrar a fala) — ato de mesa
    /// diretora/presidencia, separado da gestao geral do Legislativo.
    /// </summary>
    public const string LegislativoTribunaControlar = "legislativo.tribuna.controlar";

    /// <summary>Gerenciar o cadastro de comissoes (permanentes/temporarias) e sua composicao/presidencia.</summary>
    public const string LegislativoComissoesGerenciar = "legislativo.comissoes.gerenciar";

    /// <summary>Visualizar dados do modulo Transparencia (dados abertos, remessas TCE).</summary>
    public const string TransparenciaVer = "transparencia.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Transparencia.</summary>
    public const string TransparenciaGerenciar = "transparencia.gerenciar";

    /// <summary>Consultar pedidos e-SIC internamente (com PII do solicitante — LAI Lei 12.527/2011).</summary>
    public const string TransparenciaEsicVer = "transparencia.esic.ver";

    /// <summary>Responder/atender/prorrogar/indeferir/decidir recurso de pedidos e-SIC.</summary>
    public const string TransparenciaEsicResponder = "transparencia.esic.responder";

    /// <summary>
    /// Visualizar o PAINEL DO GESTOR + BI (modulo PainelGestor): indicadores consolidados do municipio
    /// por exercicio (execucao orcamentaria, minimos constitucionais, arrecadacao/divida, custo de
    /// pessoal/% RCL-LRF, prontidao de prestacao de contas). Modulo READ-ONLY: nao ha verbo "gerenciar"
    /// — o painel apenas agrega read models materializados a partir de Integration Events dos modulos-fonte.
    /// </summary>
    public const string PainelVer = "painel.ver";

    // --- Permissoes administrativas e transversais ---

    /// <summary>Gerenciar usuarios e papeis (RBAC) do tenant — modulo Identidade.</summary>
    public const string IdentidadeUsuariosGerenciar = "identidade.usuarios.gerenciar";

    /// <summary>
    /// Configurar a ativacao modular do tenant (tabela TenantModule). ATENCAO: e operacao de
    /// CONTROLE/COMERCIAL (escreve no banco de PLATAFORMA, nao isolado por tenant) — por isso
    /// migrou para a permissao de PLATAFORMA <see cref="PlataformaModulosConfigurar"/> e NAO
    /// pertence mais a <see cref="Todas"/> (achado XT-1: admin de A nao licencia modulos de B).
    /// Mantida como constante apenas para referencia/compatibilidade do catalogo; nao e mais o gate
    /// dos endpoints de modulo.
    /// </summary>
    public const string AdminModulosConfigurar = "admin.modulos.configurar";

    /// <summary>Gerenciar os certificados digitais A1 (.pfx) do tenant no Key Vault.</summary>
    public const string AdminCertificadoGerenciar = "admin.certificado.gerenciar";

    /// <summary>Consultar a trilha de auditoria imutavel (para o Tribunal de Contas).</summary>
    public const string AdminAuditoriaVer = "admin.auditoria.ver";

    /// <summary>
    /// Verificar a INTEGRIDADE da trilha de auditoria (recomputar a cadeia de hash e detectar
    /// adulteracao/remocao). Verbo separado de <see cref="AdminAuditoriaVer"/> — quem le a trilha
    /// nao necessariamente atesta sua imutabilidade ao Tribunal de Contas.
    /// </summary>
    public const string AdminAuditoriaVerificar = "admin.auditoria.verificar";

    /// <summary>Assinar documentos eletronicamente (Lei 14.063/2020) no Protocolo.</summary>
    public const string DocumentosAssinar = "documentos.assinar";

    // --- Verbos finos de Segregacao de Funcoes (SoD) — atos com efeito legal/fiscal ---
    // TODO(a confirmar: matriz SoD TCE-RS). Acrescimo COMPATIVEL (escopos novos, nada removido):
    // base da invariante I9 (permissoes conflitantes nao coexistem no mesmo sujeito/UO — M1.x).

    /// <summary>Assinar o empenho da despesa (ato com efeito fiscal — Lei 4.320). TODO(a confirmar: matriz SoD TCE-RS).</summary>
    public const string FinancasEmpenhoAssinar = "financas.empenho.assinar";

    /// <summary>Atestar a liquidacao da despesa (verificacao do direito do credor). TODO(a confirmar: matriz SoD TCE-RS).</summary>
    public const string FinancasLiquidacaoAtestar = "financas.liquidacao.atestar";

    /// <summary>Ordenar o pagamento (ordenador de despesa). TODO(a confirmar: matriz SoD TCE-RS).</summary>
    public const string FinancasPagamentoOrdenar = "financas.pagamento.ordenar";

    /// <summary>Encerrar o exercicio financeiro/contabil. TODO(a confirmar: matriz SoD TCE-RS).</summary>
    public const string FinancasExercicioEncerrar = "financas.exercicio.encerrar";

    /// <summary>Transmitir a remessa oficial (TCE-RS SIAPC/PAD, SICONFI/MSC). TODO(a confirmar: matriz SoD TCE-RS).</summary>
    public const string TransparenciaRemessaTransmitir = "transparencia.remessa.transmitir";

    /// <summary>Assinar documento no Protocolo com valor de ato (Lei 14.063/2020). TODO(a confirmar: matriz SoD TCE-RS).</summary>
    public const string ProtocoloDocumentoAssinar = "protocolo.documento.assinar";

    // --- Permissao de PLATAFORMA (operador Tensorroot) — NAO e permissao de tenant ---
    // MODELO §7: o Admin da Plataforma provisiona/licencia tenants e NAO tem leitura de negocio.
    // ATENCAO: este escopo NAO entra em "Todas" de proposito — o papel "Administrador" semeado por
    // tenant recebe Permissoes.Todas e, portanto, JAMAIS recebe este verbo. Assim, "admin de tenant
    // != operador de plataforma" fica garantido por CONSTRUCAO (deny-by-default preservado): a claim
    // "perm:plataforma.tenants.provisionar" so existe em principais de plataforma, emitidos por
    // mecanismo dedicado (fora do RBAC por tenant).

    /// <summary>
    /// PLATAFORMA: provisionar/licenciar tenants (endpoint <c>/admin/tenants</c>). Acao de OPERADOR
    /// DA PLATAFORMA (Tensorroot), nao de administrador de tenant. NAO pertence a <see cref="Todas"/>
    /// (nunca concedida pelo papel "Administrador" de um tenant).
    /// </summary>
    public const string PlataformaTenantsProvisionar = "plataforma.tenants.provisionar";

    /// <summary>
    /// PLATAFORMA: configurar a ativacao modular (licencas) de um tenant — endpoints
    /// <c>/api/admin/tenants/{tenantId}/modulos</c>, que escrevem no banco de CONTROLE
    /// (<c>TenantModule</c>) por tenant arbitrario da rota. Acao de OPERADOR DA PLATAFORMA, NAO de
    /// administrador de tenant. NAO pertence a <see cref="Todas"/> (nunca concedida pelo papel
    /// "Administrador" de um tenant) — fecha o achado XT-1 (admin de A licenciava modulos de B).
    /// </summary>
    public const string PlataformaModulosConfigurar = "plataforma.modulos.configurar";

    /// <summary>Conjunto canonico, imutavel e ordenado de todas as permissoes conhecidas.</summary>
    public static readonly FrozenSet<string> Todas = new[]
    {
        AdministracaoVer,
        AdministracaoGerenciar,
        FinancasVer,
        FinancasGerenciar,
        FinancasPlanejar,
        TributosVer,
        TributosGerenciar,
        TributosItbiArbitrar,
        RecursosHumanosVer,
        RecursosHumanosGerenciar,
        RhConsignacaoVer,
        RhConsignacaoGerenciar,
        RhConsignacaoAverbar,
        PatrimonioVer,
        PatrimonioGerenciar,
        ProtocoloVer,
        ProtocoloGerenciar,
        SaudeVer,
        SaudeProntuarioLer,
        SaudeGerenciar,
        SaudeAgendaVer,
        SaudeAgendaGerenciar,
        SaudeAgendaMarcar,
        SaudeFarmaciaVer,
        SaudeFarmaciaGerenciar,
        SaudeFarmaciaDispensar,
        SaudeImunizacaoVer,
        SaudeImunizacaoGerenciar,
        SaudeImunizacaoAplicar,
        EducacaoVer,
        EducacaoGerenciar,
        AssistenciaSocialVer,
        AssistenciaSocialProntuarioLer,
        AssistenciaSocialGerenciar,
        LegislativoVer,
        LegislativoGerenciar,
        LegislativoVereadoresGerenciar,
        LegislativoDemoSemear,
        LegislativoNormasGerenciar,
        LegislativoDiarioGerenciar,
        LegislativoDiarioPublicar,
        LegislativoTribunaControlar,
        LegislativoComissoesGerenciar,
        TransparenciaVer,
        TransparenciaGerenciar,
        TransparenciaEsicVer,
        TransparenciaEsicResponder,
        PainelVer,
        IdentidadeUsuariosGerenciar,

        // NOTA (XT-1): AdminModulosConfigurar foi DELIBERADAMENTE removida de "Todas". Licenciar
        // modulos e operacao de CONTROLE/plataforma (banco nao isolado por tenant) — agora gated por
        // PlataformaModulosConfigurar, fora deste conjunto (admin de tenant nunca a recebe).
        AdminCertificadoGerenciar,
        AdminAuditoriaVer,
        AdminAuditoriaVerificar,
        DocumentosAssinar,
        FinancasEmpenhoAssinar,
        FinancasLiquidacaoAtestar,
        FinancasPagamentoOrdenar,
        FinancasExercicioEncerrar,
        TransparenciaRemessaTransmitir,
        ProtocoloDocumentoAssinar,
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Conjunto CANONICO de TODAS as permissoes atribuiveis a um <see cref="Papeis.Papel"/> de
    /// tenant — superset de <see cref="Todas"/>. Acrescenta os escopos que existem no catalogo, sao
    /// atribuiveis por RBAC, mas NAO sao concedidos ao papel "Administrador" (que recebe
    /// <see cref="Todas"/>): hoje, <see cref="AutosservicoProprio"/>, que so faz sentido no papel
    /// "Servidor" (autosservico do dado-proprio) e nao no admin (que ja ve tudo). E esta a fonte de
    /// verdade de <see cref="EhConhecida"/> — o gate de persistencia de permissoes do papel.
    /// <para>
    /// Permissoes de PLATAFORMA (<see cref="PlataformaTenantsProvisionar"/>,
    /// <see cref="PlataformaModulosConfigurar"/>) e <see cref="AdminModulosConfigurar"/> NAO entram
    /// aqui de proposito: nao sao atribuiveis pelo RBAC de tenant (vivem em principais de plataforma).
    /// </para>
    /// </summary>
    public static readonly FrozenSet<string> Conhecidas = Todas
        .Append(AutosservicoProprio)
        .ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Indica se a permissao informada pertence ao catalogo canonico atribuivel por tenant.</summary>
    /// <param name="permissao">Escopo a verificar.</param>
    /// <returns><c>true</c> se conhecida; caso contrario, <c>false</c>.</returns>
    public static bool EhConhecida(string? permissao)
        => !string.IsNullOrWhiteSpace(permissao) && Conhecidas.Contains(permissao);
}
