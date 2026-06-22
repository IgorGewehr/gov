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

    /// <summary>Visualizar dados do modulo Tributos (IPTU, ISS, ITBI, Divida Ativa).</summary>
    public const string TributosVer = "tributos.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Tributos.</summary>
    public const string TributosGerenciar = "tributos.gerenciar";

    /// <summary>Visualizar dados do modulo Recursos Humanos (folha, cargos, ponto).</summary>
    public const string RecursosHumanosVer = "recursoshumanos.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Recursos Humanos.</summary>
    public const string RecursosHumanosGerenciar = "recursoshumanos.gerenciar";

    /// <summary>Visualizar dados do modulo Patrimonio (bens, frota, almoxarifado).</summary>
    public const string PatrimonioVer = "patrimonio.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Patrimonio.</summary>
    public const string PatrimonioGerenciar = "patrimonio.gerenciar";

    /// <summary>Visualizar dados do modulo Protocolo (processo eletronico, GED).</summary>
    public const string ProtocoloVer = "protocolo.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Protocolo.</summary>
    public const string ProtocoloGerenciar = "protocolo.gerenciar";

    /// <summary>Visualizar dados do modulo Saude (UBS, PEP, farmacia).</summary>
    public const string SaudeVer = "saude.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Saude.</summary>
    public const string SaudeGerenciar = "saude.gerenciar";

    /// <summary>Visualizar dados do modulo Educacao (escolas, matriculas, merenda).</summary>
    public const string EducacaoVer = "educacao.ver";

    /// <summary>Gerenciar (mutar) dados do modulo Educacao.</summary>
    public const string EducacaoGerenciar = "educacao.gerenciar";

    /// <summary>Visualizar dados do modulo Assistencia Social (SUAS, CadUnico, beneficios).</summary>
    public const string AssistenciaSocialVer = "assistenciasocial.ver";

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

    // --- Permissoes administrativas e transversais ---

    /// <summary>Gerenciar usuarios e papeis (RBAC) do tenant — modulo Identidade.</summary>
    public const string IdentidadeUsuariosGerenciar = "identidade.usuarios.gerenciar";

    /// <summary>Configurar a ativacao modular do tenant (tabela TenantModule).</summary>
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

    /// <summary>Conjunto canonico, imutavel e ordenado de todas as permissoes conhecidas.</summary>
    public static readonly FrozenSet<string> Todas = new[]
    {
        AdministracaoVer,
        AdministracaoGerenciar,
        FinancasVer,
        FinancasGerenciar,
        TributosVer,
        TributosGerenciar,
        RecursosHumanosVer,
        RecursosHumanosGerenciar,
        PatrimonioVer,
        PatrimonioGerenciar,
        ProtocoloVer,
        ProtocoloGerenciar,
        SaudeVer,
        SaudeGerenciar,
        EducacaoVer,
        EducacaoGerenciar,
        AssistenciaSocialVer,
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
        IdentidadeUsuariosGerenciar,
        AdminModulosConfigurar,
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

    /// <summary>Indica se a permissao informada pertence ao catalogo canonico.</summary>
    /// <param name="permissao">Escopo a verificar.</param>
    /// <returns><c>true</c> se conhecida; caso contrario, <c>false</c>.</returns>
    public static bool EhConhecida(string? permissao)
        => !string.IsNullOrWhiteSpace(permissao) && Todas.Contains(permissao);
}
