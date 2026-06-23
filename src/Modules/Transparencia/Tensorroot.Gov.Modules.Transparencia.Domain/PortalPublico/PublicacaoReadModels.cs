using Tensorroot.Gov.SharedKernel;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

/// <summary>Fase do estagio da despesa publica (Lei 4.320/1964: empenho, liquidacao, pagamento).</summary>
public enum FaseDespesa
{
    /// <summary>1o estagio: empenho (reserva da dotacao).</summary>
    Empenhada = 1,

    /// <summary>2o estagio: liquidacao (verificacao do direito do credor — art. 63).</summary>
    Liquidada = 2,

    /// <summary>3o estagio: pagamento.</summary>
    Paga = 3,
}

/// <summary>
/// Read model de TRANSPARENCIA ATIVA de DESPESA, materializado de Integration Events JA publicados por
/// Financas (<c>DespesaEmpenhada/Liquidada/PagamentoEfetuado</c>). NAO tem comportamento de dominio — e
/// uma projecao tenant-scoped (Global Query Filter), idempotente por <see cref="OrigemEventoId"/>
/// (reprocessar a mesma origem nao duplica). Exposto SOMENTE LEITURA na superficie publica (LAI).
/// LGPD: <see cref="CredorDocMascarado"/> ja nasce mascarado (CPF nunca integral — Dec. 7.724/2012).
/// </summary>
public sealed class PublicacaoDespesa : Entity<Guid>, IMustHaveTenant
{
    private PublicacaoDespesa()
    {
    }

    private PublicacaoDespesa(Guid id, Guid tenantId, Guid origemEventoId)
        : base(id)
    {
        TenantId = tenantId;
        OrigemEventoId = origemEventoId;
    }

    /// <summary>Tenant (ente publico) dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave de origem (EventId do Integration Event) — idempotencia da projecao (I-13).</summary>
    public Guid OrigemEventoId { get; private set; }

    /// <summary>Exercicio (ano) de referencia.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Fase do estagio da despesa.</summary>
    public FaseDespesa Fase { get; private set; }

    /// <summary>Numero do empenho (quando aplicavel a fase).</summary>
    public string? NumeroEmpenho { get; private set; }

    /// <summary>Nome/razao social do credor (PUBLICO — transparencia ativa).</summary>
    public string? CredorNomeOuRazao { get; private set; }

    /// <summary>Documento do credor JA MASCARADO (CPF nunca integral; CNPJ publico). LGPD.</summary>
    public string? CredorDocMascarado { get; private set; }

    /// <summary>Funcao/subfuncao de governo (FS, ex.: "10301").</summary>
    public string? FuncaoSubfuncao { get; private set; }

    /// <summary>Fonte/destinacao de recurso (FR).</summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Valor da fase.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Data da fase.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>
    /// Materializa uma linha de despesa publica a partir de um evento de Financas. O documento do credor
    /// e mascarado AQUI (na projecao), de modo que a tabela publica jamais grava CPF integral.
    /// </summary>
    /// <param name="tenantId">Tenant dono do registro.</param>
    /// <param name="origemEventoId">EventId de origem (idempotencia).</param>
    /// <param name="exercicio">Exercicio.</param>
    /// <param name="fase">Fase do estagio da despesa.</param>
    /// <param name="valor">Valor.</param>
    /// <param name="data">Data.</param>
    /// <param name="numeroEmpenho">Numero do empenho (opcional).</param>
    /// <param name="credorNomeOuRazao">Nome/razao do credor (opcional).</param>
    /// <param name="credorDocumento">Documento do credor SEM mascara (sera mascarado).</param>
    /// <param name="funcaoSubfuncao">Funcao/subfuncao (opcional).</param>
    /// <param name="fonteRecurso">Fonte de recurso (opcional).</param>
    /// <returns>Nova projecao de despesa.</returns>
    public static PublicacaoDespesa Materializar(
        Guid tenantId,
        Guid origemEventoId,
        int exercicio,
        FaseDespesa fase,
        decimal valor,
        DateOnly data,
        string? numeroEmpenho,
        string? credorNomeOuRazao,
        string? credorDocumento,
        string? funcaoSubfuncao,
        string? fonteRecurso)
    {
        var registro = new PublicacaoDespesa(Guid.NewGuid(), tenantId, origemEventoId);
        registro.Aplicar(exercicio, fase, valor, data, numeroEmpenho, credorNomeOuRazao, credorDocumento, funcaoSubfuncao, fonteRecurso);
        return registro;
    }

    /// <summary>Reaplica os dados (upsert idempotente sobre a mesma origem).</summary>
    public void Aplicar(
        int exercicio,
        FaseDespesa fase,
        decimal valor,
        DateOnly data,
        string? numeroEmpenho,
        string? credorNomeOuRazao,
        string? credorDocumento,
        string? funcaoSubfuncao,
        string? fonteRecurso)
    {
        Exercicio = exercicio;
        Fase = fase;
        Valor = valor;
        Data = data;
        NumeroEmpenho = numeroEmpenho;
        CredorNomeOuRazao = credorNomeOuRazao;
        CredorDocMascarado = credorDocumento is null ? null : Mascaramento.MascararDocumento(credorDocumento);
        FuncaoSubfuncao = funcaoSubfuncao;
        FonteRecurso = fonteRecurso;
    }
}

/// <summary>
/// Read model publico de RECEITA arrecadada, materializado de eventos de Financas/Tributos. Projecao
/// tenant-scoped, idempotente por <see cref="OrigemEventoId"/>. Receita nao tem PII.
/// </summary>
public sealed class PublicacaoReceita : Entity<Guid>, IMustHaveTenant
{
    private PublicacaoReceita()
    {
    }

    private PublicacaoReceita(Guid id, Guid tenantId, Guid origemEventoId)
        : base(id)
    {
        TenantId = tenantId;
        OrigemEventoId = origemEventoId;
    }

    /// <summary>Tenant dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave de origem (idempotencia).</summary>
    public Guid OrigemEventoId { get; private set; }

    /// <summary>Exercicio (ano).</summary>
    public int Exercicio { get; private set; }

    /// <summary>Rubrica da receita (classificacao).</summary>
    public string? RubricaReceita { get; private set; }

    /// <summary>Fonte de recurso.</summary>
    public string? FonteRecurso { get; private set; }

    /// <summary>Valor arrecadado.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Data de referencia.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Materializa uma linha de receita publica.</summary>
    public static PublicacaoReceita Materializar(
        Guid tenantId,
        Guid origemEventoId,
        int exercicio,
        decimal valor,
        DateOnly data,
        string? rubricaReceita,
        string? fonteRecurso)
    {
        var registro = new PublicacaoReceita(Guid.NewGuid(), tenantId, origemEventoId);
        registro.Aplicar(exercicio, valor, data, rubricaReceita, fonteRecurso);
        return registro;
    }

    /// <summary>Reaplica os dados (upsert idempotente).</summary>
    public void Aplicar(int exercicio, decimal valor, DateOnly data, string? rubricaReceita, string? fonteRecurso)
    {
        Exercicio = exercicio;
        Valor = valor;
        Data = data;
        RubricaReceita = rubricaReceita;
        FonteRecurso = fonteRecurso;
    }
}

/// <summary>
/// Read model publico de CONTRATO, materializado de eventos de Administracao
/// (<c>ContratoAssinado/ContratoPublicadoPncp/LicitacaoHomologada</c>). Projecao tenant-scoped,
/// idempotente por <see cref="OrigemEventoId"/>. Fornecedor (pessoa juridica) e dado publico.
/// </summary>
public sealed class PublicacaoContrato : Entity<Guid>, IMustHaveTenant
{
    private PublicacaoContrato()
    {
    }

    private PublicacaoContrato(Guid id, Guid tenantId, Guid origemEventoId)
        : base(id)
    {
        TenantId = tenantId;
        OrigemEventoId = origemEventoId;
    }

    /// <summary>Tenant dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Chave de origem (idempotencia).</summary>
    public Guid OrigemEventoId { get; private set; }

    /// <summary>Exercicio (ano) do contrato.</summary>
    public int Exercicio { get; private set; }

    /// <summary>Numero do contrato (ou identificador interno enquanto o numero formal nao chega).</summary>
    public string? NumeroContrato { get; private set; }

    /// <summary>Fornecedor (razao social / nome empresarial) — PUBLICO.</summary>
    public string? Fornecedor { get; private set; }

    /// <summary>Objeto do contrato.</summary>
    public string? Objeto { get; private set; }

    /// <summary>Valor global.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Modalidade (Lei 14.133/2021), quando conhecida.</summary>
    public string? Modalidade { get; private set; }

    /// <summary>Identificador no PNCP (link de publicidade), quando publicado.</summary>
    public string? NumeroContratoPncp { get; private set; }

    /// <summary>Materializa/atualiza uma linha de contrato publico.</summary>
    public static PublicacaoContrato Materializar(
        Guid tenantId,
        Guid origemEventoId,
        int exercicio,
        decimal valor,
        string? numeroContrato,
        string? fornecedor,
        string? objeto,
        string? modalidade,
        string? numeroContratoPncp)
    {
        var registro = new PublicacaoContrato(Guid.NewGuid(), tenantId, origemEventoId);
        registro.Aplicar(exercicio, valor, numeroContrato, fornecedor, objeto, modalidade, numeroContratoPncp);
        return registro;
    }

    /// <summary>Reaplica os dados (upsert idempotente).</summary>
    public void Aplicar(
        int exercicio,
        decimal valor,
        string? numeroContrato,
        string? fornecedor,
        string? objeto,
        string? modalidade,
        string? numeroContratoPncp)
    {
        Exercicio = exercicio;
        Valor = valor;
        NumeroContrato = numeroContrato;
        Fornecedor = fornecedor;
        Objeto = objeto;
        Modalidade = modalidade;
        NumeroContratoPncp = numeroContratoPncp;
    }

    /// <summary>Registra a publicacao no PNCP preservando os demais campos (chega em evento separado).</summary>
    /// <param name="numeroContratoPncp">Identificador do contrato no PNCP.</param>
    public void RegistrarPncp(string? numeroContratoPncp) => NumeroContratoPncp = numeroContratoPncp;
}

/// <summary>
/// Read model publico de FOLHA NOMINAL, derivado do <c>FolhaResumoRemessaTceIntegrationEvent</c> (RH).
/// LGPD CRITICO: expoe nome do servidor, cargo, lotacao e remuneracao bruta/descontos/liquido (PUBLICO
/// por Dec. 7.724/2012), mas <b>NUNCA CPF nem matricula</b> — esses campos NAO existem nesta entidade,
/// logo nao ha como vazar por uma rota publica. Projecao tenant-scoped, idempotente por
/// <see cref="OrigemEventoId"/> (chave natural = competencia + codigo do servidor).
/// </summary>
public sealed class PublicacaoFolhaNominal : Entity<Guid>, IMustHaveTenant
{
    private PublicacaoFolhaNominal()
    {
    }

    private PublicacaoFolhaNominal(Guid id, Guid tenantId, string origemEventoId)
        : base(id)
    {
        TenantId = tenantId;
        OrigemEventoId = origemEventoId;
    }

    /// <summary>Tenant dono do registro.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Chave de origem composta (competencia + codigo do servidor) — idempotencia da projecao. String
    /// porque a folha nominal vem agregada por evento; cada servidor vira uma linha estavel reprocessavel.
    /// </summary>
    public string OrigemEventoId { get; private set; } = default!;

    /// <summary>Competencia (<c>AAAA-MM</c>).</summary>
    public string Competencia { get; private set; } = default!;

    /// <summary>Nome civil do servidor (PUBLICO — transparencia ativa).</summary>
    public string ServidorNome { get; private set; } = default!;

    /// <summary>Descricao do cargo (PUBLICO).</summary>
    public string? CargoDescricao { get; private set; }

    /// <summary>Lotacao/unidade (PUBLICO).</summary>
    public string? Lotacao { get; private set; }

    /// <summary>Remuneracao bruta (soma das vantagens).</summary>
    public decimal RemuneracaoBruta { get; private set; }

    /// <summary>Total de descontos.</summary>
    public decimal Descontos { get; private set; }

    /// <summary>Liquido (bruta - descontos).</summary>
    public decimal Liquido { get; private set; }

    /// <summary>
    /// Materializa/atualiza uma linha de folha nominal publica. NAO recebe CPF nem matricula por
    /// construcao — somente os campos publicos por lei.
    /// </summary>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="competencia">Competencia (<c>AAAA-MM</c>).</param>
    /// <param name="codigoServidor">Codigo estavel do servidor na folha (compoe a chave de origem; NAO e exposto).</param>
    /// <param name="servidorNome">Nome do servidor.</param>
    /// <param name="cargoDescricao">Descricao do cargo.</param>
    /// <param name="lotacao">Lotacao.</param>
    /// <param name="remuneracaoBruta">Remuneracao bruta.</param>
    /// <param name="descontos">Total de descontos.</param>
    /// <returns>Nova projecao de folha nominal.</returns>
    public static PublicacaoFolhaNominal Materializar(
        Guid tenantId,
        string competencia,
        string codigoServidor,
        string servidorNome,
        string? cargoDescricao,
        string? lotacao,
        decimal remuneracaoBruta,
        decimal descontos)
    {
        var origem = $"{competencia}|{codigoServidor}";
        var registro = new PublicacaoFolhaNominal(Guid.NewGuid(), tenantId, origem);
        registro.Aplicar(competencia, servidorNome, cargoDescricao, lotacao, remuneracaoBruta, descontos);
        return registro;
    }

    /// <summary>Reaplica os dados (upsert idempotente).</summary>
    public void Aplicar(
        string competencia,
        string servidorNome,
        string? cargoDescricao,
        string? lotacao,
        decimal remuneracaoBruta,
        decimal descontos)
    {
        Competencia = competencia;
        ServidorNome = servidorNome;
        CargoDescricao = cargoDescricao;
        Lotacao = lotacao;
        RemuneracaoBruta = remuneracaoBruta;
        Descontos = descontos;
        Liquido = remuneracaoBruta - descontos;
    }
}
