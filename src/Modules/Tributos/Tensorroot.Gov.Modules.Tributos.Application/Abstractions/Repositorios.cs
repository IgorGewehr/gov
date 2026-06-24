using Tensorroot.Gov.Modules.Tributos.Domain.Alvaras;
using Tensorroot.Gov.Modules.Tributos.Domain.Arrecadacao;
using Tensorroot.Gov.Modules.Tributos.Domain.Certidoes;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Cosip;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;
using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi;
using Tensorroot.Gov.Modules.Tributos.Domain.Itbi.Arbitramento;
using Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;
using Tensorroot.Gov.Modules.Tributos.Domain.Melhoria;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.Pgv;
using Tensorroot.Gov.Modules.Tributos.Domain.Taxas;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Application.Abstractions;

/// <summary>Repositório do agregado <see cref="Contribuinte"/>.</summary>
public interface IContribuinteRepository
{
    /// <summary>Marca um novo contribuinte para inserção.</summary>
    /// <param name="contribuinte">Contribuinte a adicionar.</param>
    void Adicionar(Contribuinte contribuinte);

    /// <summary>Obtém um contribuinte por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O contribuinte, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Contribuinte?> ObterPorIdAsync(ContribuinteId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Lancamento"/>.</summary>
public interface ILancamentoRepository
{
    /// <summary>Marca um novo lançamento para inserção.</summary>
    /// <param name="lancamento">Lançamento a adicionar.</param>
    void Adicionar(Lancamento lancamento);

    /// <summary>Obtém um lançamento por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O lançamento, ou <c>null</c>.</returns>
    Task<Lancamento?> ObterPorIdAsync(LancamentoId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="DividaAtiva"/>.</summary>
public interface IDividaAtivaRepository
{
    /// <summary>Marca uma nova dívida ativa para inserção.</summary>
    /// <param name="dividaAtiva">Dívida ativa a adicionar.</param>
    void Adicionar(DividaAtiva dividaAtiva);

    /// <summary>Obtém uma dívida ativa por identificador.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A dívida ativa, ou <c>null</c>.</returns>
    Task<DividaAtiva?> ObterPorIdAsync(DividaAtivaId id, CancellationToken cancellationToken);

    /// <summary>Lista as dívidas ativas de um contribuinte.</summary>
    /// <param name="contribuinteId">Contribuinte.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dívidas ativas do contribuinte.</returns>
    Task<IReadOnlyList<DividaAtiva>> ListarPorContribuinteAsync(ContribuinteId contribuinteId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o próximo número sequencial de inscrição no Registro de Dívida Ativa do tenant (inc. V da CDA).
    /// Deriva do total já inscrito + 1. // TODO(validar-oficial): formato/sequência oficial do TIDA por exercício.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Próximo número de inscrição (&gt;= 1).</returns>
    Task<long> ObterProximoNumeroInscricaoAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Apura a posição consolidada da Dívida Ativa de um exercício (agrupado pelo ano de inscrição):
    /// estoque inscrito vigente, parcela ajuizada (execução fiscal) e valor recuperado (quitado) no exercício.
    /// Insumo do <c>PosicaoDividaAtivaIntegrationEvent</c> para o Painel do Gestor.
    /// </summary>
    /// <param name="exercicio">Exercício (ano de inscrição) de referência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Posição consolidada do exercício.</returns>
    Task<PosicaoDividaAtivaProjecao> ObterPosicaoPorExercicioAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>
/// Projeção da posição consolidada da Dívida Ativa de um exercício (read model interno do Tributos,
/// sem vazar o domínio para fora do módulo).
/// </summary>
/// <param name="SaldoInscrito">Estoque total inscrito vigente (não quitado/cancelado) ao fim do período.</param>
/// <param name="SaldoAjuizado">Parcela do estoque em execução fiscal.</param>
/// <param name="RecuperadoNoExercicio">Valor de dívida ativa quitado no exercício.</param>
public readonly record struct PosicaoDividaAtivaProjecao(decimal SaldoInscrito, decimal SaldoAjuizado, decimal RecuperadoNoExercicio);

/// <summary>
/// Apuração da situação fiscal de um contribuinte para fins de CND/CPEN (CTN arts. 205/206): conta os
/// débitos próprios vencidos em aberto e a dívida ativa, separando os EXIGÍVEIS dos com exigibilidade
/// SUSPENSA (parcelados). Read model interno do Tributos (não vaza o domínio para fora do módulo).
/// </summary>
/// <param name="LancamentosVencidosEmAberto">Qtde de lançamentos próprios vencidos e em aberto (exigíveis) na data-base.</param>
/// <param name="DividasAtivasExigiveis">Qtde de inscrições em dívida ativa exigíveis (não suspensas/quitadas/canceladas).</param>
/// <param name="DividasAtivasSuspensas">Qtde de inscrições com exigibilidade suspensa (parceladas).</param>
public readonly record struct SituacaoFiscalContribuinte(
    int LancamentosVencidosEmAberto,
    int DividasAtivasExigiveis,
    int DividasAtivasSuspensas);

/// <summary>Apuração da situação fiscal de um contribuinte (insumo da CND/CPEN — SW-A3).</summary>
public interface ISituacaoFiscalConsulta
{
    /// <summary>
    /// Apura a situação fiscal do contribuinte na DATA-BASE informada (data do fato): débitos próprios
    /// vencidos em aberto e dívida ativa, separando exigíveis de suspensos. Respeita o Global Query
    /// Filter por tenant.
    /// </summary>
    /// <param name="contribuinteId">Contribuinte a apurar.</param>
    /// <param name="dataBase">Data-base de referência (afere o vencimento e a prescrição).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A situação fiscal apurada.</returns>
    Task<SituacaoFiscalContribuinte> ApurarAsync(ContribuinteId contribuinteId, DateOnly dataBase, CancellationToken cancellationToken);

    /// <summary>Resolve o contribuinte do tenant pelo documento (CPF/CNPJ — somente dígitos), ou <c>null</c>.</summary>
    /// <param name="documento">Documento sem máscara.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O contribuinte, ou <c>null</c> se inexistente no tenant.</returns>
    Task<Contribuinte?> ResolverContribuintePorDocumentoAsync(string documento, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="CertidaoRegularidadeFiscal"/> (CND/CPEN — CTN 205/206).</summary>
public interface ICertidaoRegularidadeFiscalRepository
{
    /// <summary>Marca uma nova certidão para inserção.</summary>
    /// <param name="certidao">Certidão a adicionar.</param>
    void Adicionar(CertidaoRegularidadeFiscal certidao);

    /// <summary>
    /// Obtém o próximo número sequencial de certidão do tenant no exercício (deriva do total emitido no
    /// ano + 1). // TODO(validar-oficial): sequência oficial conforme o CTM.
    /// </summary>
    /// <param name="exercicio">Exercício (ano) de emissão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Próximo sequencial (&gt;= 1).</returns>
    Task<long> ObterProximoSequencialAsync(int exercicio, CancellationToken cancellationToken);

    /// <summary>Obtém uma certidão pelo número, ou <c>null</c> (para conferência de autenticidade).</summary>
    /// <param name="numero">Número da certidão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A certidão, ou <c>null</c>.</returns>
    Task<CertidaoRegularidadeFiscal?> ObterPorNumeroAsync(string numero, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="DeclaracaoGiaIss"/> (GIA mensal de ISS — SW-A10).</summary>
public interface IDeclaracaoGiaIssRepository
{
    /// <summary>Marca uma nova declaração para inserção.</summary>
    /// <param name="declaracao">Declaração a adicionar.</param>
    void Adicionar(DeclaracaoGiaIss declaracao);

    /// <summary>Obtém uma declaração por identificador (com itens), ou <c>null</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A declaração, ou <c>null</c>.</returns>
    Task<DeclaracaoGiaIss?> ObterPorIdAsync(DeclaracaoGiaIssId id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém a GIA VIGENTE (não substituída) de um contribuinte numa competência (com itens), ou <c>null</c>.
    /// </summary>
    /// <param name="contribuinteId">Contribuinte declarante.</param>
    /// <param name="competencia">Competência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A declaração vigente, ou <c>null</c>.</returns>
    Task<DeclaracaoGiaIss?> ObterVigentePorContribuinteCompetenciaAsync(ContribuinteId contribuinteId, Competencia competencia, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Imovel"/> (cadastro imobiliário).</summary>
public interface IImovelRepository
{
    /// <summary>Marca um novo imóvel para inserção.</summary>
    /// <param name="imovel">Imóvel a adicionar.</param>
    void Adicionar(Imovel imovel);

    /// <summary>Obtém um imóvel por identificador (respeitando o filtro de tenant).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O imóvel, ou <c>null</c>.</returns>
    Task<Imovel?> ObterPorIdAsync(ImovelId id, CancellationToken cancellationToken);

    /// <summary>Lista os imóveis de um proprietário.</summary>
    /// <param name="proprietarioId">Contribuinte proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Imóveis do proprietário.</returns>
    Task<IReadOnlyList<Imovel>> ListarPorProprietarioAsync(ContribuinteId proprietarioId, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="PlantaValores"/> (PGV).</summary>
public interface IPlantaValoresRepository
{
    /// <summary>Marca uma nova PGV para inserção.</summary>
    /// <param name="planta">PGV a adicionar.</param>
    void Adicionar(PlantaValores planta);

    /// <summary>Obtém uma PGV por identificador (com zonas e fatores).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A PGV, ou <c>null</c>.</returns>
    Task<PlantaValores?> ObterPorIdAsync(PlantaValoresId id, CancellationToken cancellationToken);

    /// <summary>Obtém a PGV vigente de um exercício (com zonas e fatores).</summary>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A PGV vigente, ou <c>null</c> se não houver.</returns>
    Task<PlantaValores?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="TabelaAliquotaIptu"/>.</summary>
public interface ITabelaAliquotaIptuRepository
{
    /// <summary>Marca uma nova tabela de alíquotas para inserção.</summary>
    /// <param name="tabela">Tabela a adicionar.</param>
    void Adicionar(TabelaAliquotaIptu tabela);

    /// <summary>Obtém uma tabela por identificador (com faixas).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela, ou <c>null</c>.</returns>
    Task<TabelaAliquotaIptu?> ObterPorIdAsync(TabelaAliquotaIptuId id, CancellationToken cancellationToken);

    /// <summary>Obtém a tabela vigente de um exercício (predial ou territorial).</summary>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="edificado">Tabela predial (true) ou territorial (false).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela vigente, ou <c>null</c>.</returns>
    Task<TabelaAliquotaIptu?> ObterVigenteAsync(int exercicio, bool edificado, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Dam"/> (guia/carnê).</summary>
public interface IDamRepository
{
    /// <summary>Marca um novo DAM para inserção.</summary>
    /// <param name="dam">DAM a adicionar.</param>
    void Adicionar(Dam dam);

    /// <summary>Obtém um DAM por identificador (com parcelas).</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O DAM, ou <c>null</c>.</returns>
    Task<Dam?> ObterPorIdAsync(DamId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="TabelaAliquotaIss"/> (alíquotas ISS por item LC 116).</summary>
public interface ITabelaAliquotaIssRepository
{
    /// <summary>Marca uma nova tabela de ISS para inserção.</summary>
    /// <param name="tabela">Tabela a adicionar.</param>
    void Adicionar(TabelaAliquotaIss tabela);

    /// <summary>
    /// Obtém a tabela de ISS vigente para uma competência (a de maior início de vigência ≤ AAAAMM da
    /// competência), com itens. // TODO(validar-oficial): regra de seleção por vigência conforme o CTM.
    /// </summary>
    /// <param name="competencia">Competência a apurar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela vigente, ou <c>null</c>.</returns>
    Task<TabelaAliquotaIss?> ObterVigentePorCompetenciaAsync(Competencia competencia, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="ApuracaoIss"/> (livro/escrituração mensal do ISS).</summary>
public interface IApuracaoIssRepository
{
    /// <summary>Marca uma nova apuração para inserção.</summary>
    /// <param name="apuracao">Apuração a adicionar.</param>
    void Adicionar(ApuracaoIss apuracao);

    /// <summary>Obtém a apuração de um contribuinte numa competência (com itens), ou <c>null</c>.</summary>
    /// <param name="contribuinteId">Contribuinte (prestador).</param>
    /// <param name="competencia">Competência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A apuração, ou <c>null</c>.</returns>
    Task<ApuracaoIss?> ObterPorContribuinteCompetenciaAsync(ContribuinteId contribuinteId, Competencia competencia, CancellationToken cancellationToken);
}

/// <summary>Consulta às NFS-e ingeridas (base da apuração do ISS).</summary>
public interface INotaFiscalServicoConsulta
{
    /// <summary>
    /// Lista as NFS-e VIGENTES (situação normal) de um prestador numa competência — base da apuração.
    /// </summary>
    /// <param name="prestadorCnpj">CNPJ do prestador.</param>
    /// <param name="competencia">Competência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>NFS-e vigentes do prestador na competência.</returns>
    Task<IReadOnlyList<NotaFiscalServico>> ListarVigentesPorPrestadorCompetenciaAsync(
        string prestadorCnpj,
        Competencia competencia,
        CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="AliquotaItbi"/>.</summary>
public interface IAliquotaItbiRepository
{
    /// <summary>Marca uma nova configuração de alíquota do ITBI para inserção.</summary>
    /// <param name="aliquota">Configuração a adicionar.</param>
    void Adicionar(AliquotaItbi aliquota);

    /// <summary>Obtém a alíquota do ITBI vigente de um exercício, ou <c>null</c>.</summary>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A configuração vigente, ou <c>null</c>.</returns>
    Task<AliquotaItbi?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="TransmissaoImobiliaria"/>.</summary>
public interface ITransmissaoImobiliariaRepository
{
    /// <summary>Marca uma nova transmissão para inserção.</summary>
    /// <param name="transmissao">Transmissão a adicionar.</param>
    void Adicionar(TransmissaoImobiliaria transmissao);

    /// <summary>Obtém uma transmissão por identificador, ou <c>null</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A transmissão, ou <c>null</c>.</returns>
    Task<TransmissaoImobiliaria?> ObterPorIdAsync(TransmissaoImobiliariaId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="ProcessoArbitramentoItbi"/> (arbitramento CTN art. 148).</summary>
public interface IProcessoArbitramentoItbiRepository
{
    /// <summary>Marca um novo processo de arbitramento para inserção.</summary>
    /// <param name="processo">Processo a adicionar.</param>
    void Adicionar(ProcessoArbitramentoItbi processo);

    /// <summary>Obtém um processo de arbitramento por identificador, ou <c>null</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O processo, ou <c>null</c>.</returns>
    Task<ProcessoArbitramentoItbi?> ObterPorIdAsync(ProcessoArbitramentoItbiId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="TabelaTaxa"/> (taxas e TLL — lei municipal).</summary>
public interface ITabelaTaxaRepository
{
    /// <summary>Marca uma nova tabela de taxa para inserção.</summary>
    /// <param name="tabela">Tabela a adicionar.</param>
    void Adicionar(TabelaTaxa tabela);

    /// <summary>Obtém uma tabela de taxa por identificador (com faixas), ou <c>null</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela, ou <c>null</c>.</returns>
    Task<TabelaTaxa?> ObterPorIdAsync(TabelaTaxaId id, CancellationToken cancellationToken);

    /// <summary>Obtém a tabela de taxa VIGENTE por código e exercício (com faixas), ou <c>null</c>.</summary>
    /// <param name="codigo">Código da taxa no CTM.</param>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela vigente, ou <c>null</c>.</returns>
    Task<TabelaTaxa?> ObterVigentePorCodigoAsync(string codigo, int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="Alvara"/> (ato de polícia + TLL).</summary>
public interface IAlvaraRepository
{
    /// <summary>Marca um novo alvará para inserção.</summary>
    /// <param name="alvara">Alvará a adicionar.</param>
    void Adicionar(Alvara alvara);

    /// <summary>Obtém um alvará por identificador, ou <c>null</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O alvará, ou <c>null</c>.</returns>
    Task<Alvara?> ObterPorIdAsync(AlvaraId id, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="TabelaCosip"/> (COSIP — lei municipal).</summary>
public interface ITabelaCosipRepository
{
    /// <summary>Marca uma nova tabela de COSIP para inserção.</summary>
    /// <param name="tabela">Tabela a adicionar.</param>
    void Adicionar(TabelaCosip tabela);

    /// <summary>Obtém a tabela de COSIP VIGENTE de um exercício (com faixas), ou <c>null</c>.</summary>
    /// <param name="exercicio">Exercício fiscal.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A tabela vigente, ou <c>null</c>.</returns>
    Task<TabelaCosip?> ObterVigenteAsync(int exercicio, CancellationToken cancellationToken);
}

/// <summary>Repositório do agregado <see cref="ObraContribuicaoMelhoria"/> (Contribuição de Melhoria).</summary>
public interface IObraContribuicaoMelhoriaRepository
{
    /// <summary>Marca uma nova obra para inserção.</summary>
    /// <param name="obra">Obra a adicionar.</param>
    void Adicionar(ObraContribuicaoMelhoria obra);

    /// <summary>Obtém uma obra por identificador (com imóveis beneficiados), ou <c>null</c>.</summary>
    /// <param name="id">Identificador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A obra, ou <c>null</c>.</returns>
    Task<ObraContribuicaoMelhoria?> ObterPorIdAsync(ObraContribuicaoMelhoriaId id, CancellationToken cancellationToken);
}
