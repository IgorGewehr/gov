namespace Tensorroot.Gov.Modules.Administracao.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer (saida) do PNCP — Portal Nacional de Contratacoes Publicas (Lei 14.133/2021,
/// art. 174 institui o PNCP; art. 94 a divulgacao como condicao de EFICACIA). Encapsula a integracao com a
/// API do PNCP (Manual de Integracao 2.3.5, 12/02/2025): autenticacao JWT (token de vida curta, ~1h) e o
/// fluxo de pre-cadastro orgao -&gt; unidade -&gt; compra/edital -&gt; contrato -&gt; aditivo -&gt; arquivos. A
/// implementacao de producao e resiliente (Polly: timeout + retry + circuit breaker) e IDEMPOTENTE por
/// chave de transmissao (republicar o mesmo registro nao duplica). Chamado pelo HANDLER DO OUTBOX
/// (drenagem transacional), nunca direto do request.
/// <para>
/// FRONTEIRA M9/M10: no M9 a transmissao real ao PNCP de producao NAO ocorre — a impl. SIMULADA devolve
/// um numero de controle deterministico para exercitar o pipeline ponta-a-ponta contra WireMock. A
/// transmissao real (base <c>pncp.gov.br</c>, credenciais/JWT, validacao em <c>treina.pncp.gov.br</c>)
/// e M10. // TODO(M10): trocar o simulado pelo cliente HTTP real + credenciais no Key Vault.
/// </para>
/// </summary>
public interface IPncpGateway
{
    /// <summary>
    /// Divulga (publica) o EDITAL/aviso de uma contratacao (compra) no PNCP — divulgacao obrigatoria do
    /// edital (art. 54/174), executando o pre-cadastro orgao/unidade e retornando o NUMERO DE CONTROLE
    /// PNCP da compra (chave a que o contrato decorrente se vincula via <c>numeroControlePncpCompra</c>).
    /// Idempotente pela <see cref="PublicacaoEditalPncpRequest.ChaveIdempotencia"/>.
    /// </summary>
    /// <param name="request">Dados do edital/compra a divulgar (do agregado, resolvidos pelo handler).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o numero de controle PNCP da compra (ou falha mapeada).</returns>
    Task<PublicacaoEditalPncpResultado> PublicarEditalAsync(
        PublicacaoEditalPncpRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Divulga (publica) um contrato administrativo no PNCP, executando o pre-cadastro necessario
    /// (orgao/unidade/compra) e retornando o NUMERO DE CONTROLE PNCP do contrato. Idempotente pela
    /// <see cref="PublicacaoContratoPncpRequest.ChaveIdempotencia"/>.
    /// </summary>
    /// <param name="request">Dados do contrato a divulgar (do agregado, ja resolvidos pelo handler).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com o numero de controle PNCP (ou falha mapeada).</returns>
    Task<PublicacaoContratoPncpResultado> PublicarContratoAsync(
        PublicacaoContratoPncpRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Requisicao de divulgacao de EDITAL/compra no PNCP (DTO da ACL). Espelha o servico de inclusao de
/// contratacao (compra/edital) do Manual de Integracao PNCP 2.3.5: orgao -&gt; unidade -&gt; compra.
/// // TODO(M10-validate): conferir o conjunto exato de campos do schema de compra/edital contra
/// <c>treina.pncp.gov.br</c> no credenciamento (modalidadeId, amparoLegal, instrumentoConvocatorio etc.).
/// </summary>
/// <param name="LicitacaoId">Identificador interno da licitacao (rastreabilidade/idempotencia).</param>
/// <param name="CnpjOrgao">CNPJ do orgao/entidade comprador (14 posicoes).</param>
/// <param name="CodigoUnidade">Codigo da unidade administrativa compradora (cadastrada no orgao).</param>
/// <param name="AnoCompra">Ano da contratacao.</param>
/// <param name="NumeroCompra">Numero da contratacao no sistema de origem.</param>
/// <param name="ModalidadeId">Codigo da modalidade de contratacao (tabela de dominio PNCP).</param>
/// <param name="ModoDisputaId">Codigo do modo de disputa (tabela de dominio PNCP).</param>
/// <param name="AmparoLegalCodigo">Codigo do amparo legal (tabela de dominio PNCP).</param>
/// <param name="ObjetoCompra">Objeto da contratacao.</param>
/// <param name="ValorTotalEstimado">Valor total estimado da contratacao.</param>
/// <param name="ChaveIdempotencia">Chave de idempotencia da transmissao (mesma chave =&gt; mesma operacao).</param>
public sealed record PublicacaoEditalPncpRequest(
    Guid LicitacaoId,
    string CnpjOrgao,
    string CodigoUnidade,
    int AnoCompra,
    string NumeroCompra,
    int ModalidadeId,
    int ModoDisputaId,
    string AmparoLegalCodigo,
    string ObjetoCompra,
    decimal ValorTotalEstimado,
    string ChaveIdempotencia);

/// <summary>Resultado da divulgacao de um edital/compra no PNCP.</summary>
/// <param name="Sucesso">Verdadeiro se o PNCP registrou a compra/edital.</param>
/// <param name="NumeroControlePncpCompra">Numero de controle da compra atribuido pelo PNCP (quando sucesso).</param>
/// <param name="CodigoErro">Codigo do erro mapeado pela ACL (quando falha).</param>
/// <param name="MensagemErro">Mensagem do erro (quando falha).</param>
public sealed record PublicacaoEditalPncpResultado(
    bool Sucesso,
    string? NumeroControlePncpCompra,
    string? CodigoErro,
    string? MensagemErro)
{
    /// <summary>Fabrica de resultado de sucesso.</summary>
    /// <param name="numeroControlePncpCompra">Numero de controle da compra atribuido pelo PNCP.</param>
    /// <returns>Resultado de sucesso.</returns>
    public static PublicacaoEditalPncpResultado Ok(string numeroControlePncpCompra)
        => new(true, numeroControlePncpCompra, null, null);

    /// <summary>Fabrica de resultado de falha mapeada.</summary>
    /// <param name="codigo">Codigo do erro.</param>
    /// <param name="mensagem">Mensagem do erro.</param>
    /// <returns>Resultado de falha.</returns>
    public static PublicacaoEditalPncpResultado Falha(string codigo, string mensagem)
        => new(false, null, codigo, mensagem);
}

/// <summary>
/// Tipo de pessoa do fornecedor no PNCP (campo <c>tipoPessoaFornecedor</c>, Manual 2.3.5): PJ (juridica),
/// PF (fisica) ou PE (estrangeira). Modelado como enum domestico da ACL (sem string magica no handler).
/// </summary>
public enum TipoPessoaFornecedorPncp
{
    /// <summary>Pessoa juridica ("PJ").</summary>
    PessoaJuridica = 1,

    /// <summary>Pessoa fisica ("PF").</summary>
    PessoaFisica = 2,

    /// <summary>Pessoa estrangeira ("PE").</summary>
    PessoaEstrangeira = 3,
}

/// <summary>
/// Requisicao de divulgacao de contrato no PNCP (DTO da ACL — sem tipos do dominio cruzando a fronteira).
/// Modela os campos OBRIGATORIOS do servico "Inserir Contrato/Empenho" do Manual de Integracao PNCP 2.3.5
/// (<c>POST /v1/orgaos/{cnpj}/contratos</c>): o contrato amarra-se a uma compra ja publicada
/// (<see cref="CnpjCompra"/>/<see cref="AnoCompra"/>/<see cref="SequencialCompra"/>) e exige
/// tipo/categoria do processo, unidade compradora, identificacao e tipo de pessoa do fornecedor, objeto,
/// valores (precisao 4 decimais), parcelas e datas de vigencia.
/// <para>
/// // TODO(M10-validate): validar nomes/ordem/obrigatoriedade exatos do schema 2.3.5 contra
/// <c>treina.pncp.gov.br</c> no credenciamento (ex.: <c>tipoContratoId</c>, <c>categoriaProcessoId</c>,
/// <c>frutoAdesao</c>, <c>identificadorCipi</c> para obras).
/// </para>
/// </summary>
/// <param name="ContratoId">Identificador interno do contrato (rastreabilidade/idempotencia).</param>
/// <param name="CnpjOrgao">CNPJ do orgao do contrato (URL <c>{cnpj}</c> e cabecalho — 14 posicoes).</param>
/// <param name="CnpjCompra">CNPJ originario da contratacao (proprietario da compra) — <c>cnpjCompra</c>.</param>
/// <param name="AnoCompra">Ano da contratacao a que o contrato se vincula — <c>anoCompra</c>.</param>
/// <param name="SequencialCompra">Sequencial da contratacao gerado pelo PNCP — <c>sequencialCompra</c>.</param>
/// <param name="TipoContratoId">Codigo do tipo de contrato/empenho (tabela de dominio) — <c>tipoContratoId</c>.</param>
/// <param name="NumeroContratoEmpenho">Numero do contrato no sistema de origem — <c>numeroContratoEmpenho</c>.</param>
/// <param name="AnoContrato">Ano do contrato/empenho — <c>anoContrato</c>.</param>
/// <param name="Processo">Numero do processo administrativo — <c>processo</c>.</param>
/// <param name="CategoriaProcessoId">Codigo da categoria do processo (tabela de dominio) — <c>categoriaProcessoId</c>.</param>
/// <param name="Receita"><c>true</c> = receita; <c>false</c> = despesa — <c>receita</c>.</param>
/// <param name="CodigoUnidade">Codigo da unidade executora/compradora do orgao — <c>codigoUnidade</c>.</param>
/// <param name="NiFornecedor">Numero de identificacao do fornecedor (CNPJ/CPF/estrangeiro) — <c>niFornecedor</c>.</param>
/// <param name="TipoPessoaFornecedor">PJ/PF/PE do fornecedor — <c>tipoPessoaFornecedor</c>.</param>
/// <param name="NomeRazaoSocialFornecedor">Nome/razao social do fornecedor — <c>nomeRazaoSocialFornecedor</c>.</param>
/// <param name="ObjetoContrato">Descricao do objeto do contrato — <c>objetoContrato</c>.</param>
/// <param name="ValorInicial">Valor inicial do contrato (precisao 4 decimais) — <c>valorInicial</c>.</param>
/// <param name="NumeroParcelas">Numero de parcelas — <c>numeroParcelas</c>.</param>
/// <param name="ValorGlobal">Valor global do contrato (precisao 4 decimais) — <c>valorGlobal</c>.</param>
/// <param name="DataAssinatura">Data de assinatura do contrato — <c>dataAssinatura</c>.</param>
/// <param name="DataVigenciaInicio">Inicio de vigencia — <c>dataVigenciaInicio</c>.</param>
/// <param name="DataVigenciaFim">Fim de vigencia (opcional p/ contrato tipo 1) — <c>dataVigenciaFim</c>.</param>
/// <param name="NumeroControlePncpCompra">Numero de controle da compra ja publicada a que o contrato se liga.</param>
/// <param name="FrutoAdesao">Indica contrato fruto de adesao a ata de SRP (carona) — <c>frutoAdesao</c>.</param>
/// <param name="ChaveIdempotencia">
/// Chave de idempotencia da transmissao (mesma chave =&gt; mesma operacao; o gateway nao duplica registro).
/// </param>
public sealed record PublicacaoContratoPncpRequest(
    Guid ContratoId,
    string CnpjOrgao,
    string CnpjCompra,
    int AnoCompra,
    int SequencialCompra,
    int TipoContratoId,
    string NumeroContratoEmpenho,
    int AnoContrato,
    string Processo,
    int CategoriaProcessoId,
    bool Receita,
    string CodigoUnidade,
    string NiFornecedor,
    TipoPessoaFornecedorPncp TipoPessoaFornecedor,
    string NomeRazaoSocialFornecedor,
    string ObjetoContrato,
    decimal ValorInicial,
    int NumeroParcelas,
    decimal ValorGlobal,
    DateOnly DataAssinatura,
    DateOnly DataVigenciaInicio,
    DateOnly? DataVigenciaFim,
    string? NumeroControlePncpCompra,
    bool FrutoAdesao,
    string ChaveIdempotencia);

/// <summary>Resultado da divulgacao de um contrato no PNCP.</summary>
/// <param name="Sucesso">Verdadeiro se o PNCP registrou o contrato.</param>
/// <param name="NumeroControlePncp">Numero de controle atribuido pelo PNCP (quando sucesso).</param>
/// <param name="CodigoErro">Codigo do erro mapeado pela ACL (quando falha).</param>
/// <param name="MensagemErro">Mensagem do erro (quando falha).</param>
public sealed record PublicacaoContratoPncpResultado(
    bool Sucesso,
    string? NumeroControlePncp,
    string? CodigoErro,
    string? MensagemErro)
{
    /// <summary>Fabrica de resultado de sucesso.</summary>
    /// <param name="numeroControlePncp">Numero de controle atribuido pelo PNCP.</param>
    /// <returns>Resultado de sucesso.</returns>
    public static PublicacaoContratoPncpResultado Ok(string numeroControlePncp)
        => new(true, numeroControlePncp, null, null);

    /// <summary>Fabrica de resultado de falha mapeada.</summary>
    /// <param name="codigo">Codigo do erro.</param>
    /// <param name="mensagem">Mensagem do erro.</param>
    /// <returns>Resultado de falha.</returns>
    public static PublicacaoContratoPncpResultado Falha(string codigo, string mensagem)
        => new(false, null, codigo, mensagem);
}
