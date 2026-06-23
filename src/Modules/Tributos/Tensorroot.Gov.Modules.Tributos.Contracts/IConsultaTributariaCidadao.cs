namespace Tensorroot.Gov.Modules.Tributos.Contracts;

/// <summary>
/// Porta de LEITURA cidada do modulo Tributos (cross-module via Contracts — CLAUDE.md §2). Expoe,
/// para o Portal do Cidadao, SOMENTE consultas por DOCUMENTO (CPF/CNPJ ja resolvido server-side pelo
/// modulo Cidadao a partir do principal autenticado). O Portal NUNCA conhece a entidade interna
/// <c>Contribuinte</c>/<c>ContribuinteId</c> do Tributos — passa apenas os digitos do documento; o
/// proprio Tributos resolve o contribuinte do tenant (Global Query Filter) e filtra so o dado-proprio.
/// <para>
/// SEGURANCA (anti-IDOR): nenhum metodo aceita um <c>contribuinteId</c>/<c>lancamentoId</c> arbitrario
/// do cliente. A consulta por <c>damId</c> (2a via) REVALIDA a titularidade contra o documento
/// resolvido — um DAM de outro contribuinte retorna <c>null</c> (indistinguivel de inexistente).
/// </para>
/// </summary>
public interface IConsultaTributariaCidadao
{
    /// <summary>
    /// Lista os lancamentos tributarios EM ABERTO (exigiveis, nao pagos) do contribuinte identificado
    /// pelo <paramref name="documento"/> no tenant atual. Read-only.
    /// </summary>
    /// <param name="documento">CPF/CNPJ (somente digitos) do proprio cidadao, resolvido server-side.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lancamentos em aberto (vazio se nao houver contribuinte/lancamento no tenant).</returns>
    Task<IReadOnlyList<MeuLancamentoDto>> ObterMeusLancamentosEmAbertoAsync(
        string documento,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtem a posicao consolidada da Divida Ativa do contribuinte identificado pelo
    /// <paramref name="documento"/> no tenant atual (valor originario + encargos atualizados na data-base
    /// + situacao + flag de parcelamento). Read-only.
    /// </summary>
    /// <param name="documento">CPF/CNPJ (somente digitos) do proprio cidadao, resolvido server-side.</param>
    /// <param name="dataBase">Data-base para apurar encargos (informada — sem relogio no dominio).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Posicao por CDA/inscricao (vazio se nao houver divida do contribuinte no tenant).</returns>
    Task<IReadOnlyList<MinhaDividaAtivaDto>> ObterMinhaDividaAtivaAsync(
        string documento,
        DateOnly dataBase,
        CancellationToken cancellationToken);

    /// <summary>
    /// 2a VIA de DAM/boleto: obtem um DAM por id REVALIDANDO a titularidade contra o
    /// <paramref name="documento"/> resolvido (anti-IDOR). Retorna <c>null</c> quando o DAM nao existe
    /// no tenant OU nao pertence ao contribuinte do documento — indistinguivel para nao vazar existencia.
    /// </summary>
    /// <param name="documento">CPF/CNPJ (somente digitos) do proprio cidadao, resolvido server-side.</param>
    /// <param name="damId">Identificador do DAM cuja 2a via se deseja.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A 2a via (com parcelas) ou <c>null</c>.</returns>
    Task<MeuDamDto?> ObterMeuDamAsync(
        string documento,
        Guid damId,
        CancellationToken cancellationToken);
}

/// <summary>Lancamento tributario proprio (projecao de leitura cidada).</summary>
/// <param name="LancamentoId">Identificador do lancamento.</param>
/// <param name="Tributo">Especie tributaria (ex.: "Iptu", "Iss").</param>
/// <param name="Competencia">Competencia fiscal (AAAA-MM).</param>
/// <param name="Vencimento">Data de vencimento.</param>
/// <param name="ValorPrincipal">Valor principal lancado.</param>
/// <param name="Situacao">Situacao do lancamento (ex.: "Aberto").</param>
public sealed record MeuLancamentoDto(
    Guid LancamentoId,
    string Tributo,
    string Competencia,
    DateOnly Vencimento,
    decimal ValorPrincipal,
    string Situacao);

/// <summary>Posicao de uma inscricao em Divida Ativa do proprio cidadao (projecao de leitura).</summary>
/// <param name="DividaAtivaId">Identificador da inscricao.</param>
/// <param name="Tributo">Especie tributaria de origem.</param>
/// <param name="NumeroInscricao">Numero sequencial da inscricao no Registro de Divida Ativa.</param>
/// <param name="NumeroCda">Numero da CDA, quando emitida.</param>
/// <param name="ValorOriginario">Valor originario inscrito.</param>
/// <param name="ValorAtualizado">Valor atualizado (originario + encargos) na data-base.</param>
/// <param name="Situacao">Situacao atual (ex.: "Inscrita", "Parcelada").</param>
/// <param name="Parcelada">Indica se a divida esta sob parcelamento (exigibilidade suspensa).</param>
/// <param name="DataInscricao">Data de inscricao.</param>
public sealed record MinhaDividaAtivaDto(
    Guid DividaAtivaId,
    string Tributo,
    long NumeroInscricao,
    string? NumeroCda,
    decimal ValorOriginario,
    decimal ValorAtualizado,
    string Situacao,
    bool Parcelada,
    DateOnly DataInscricao);

/// <summary>Parcela de uma 2a via de DAM (projecao de leitura cidada).</summary>
/// <param name="Numero">Numero da parcela (1..N; cota unica = 1).</param>
/// <param name="Valor">Valor da parcela.</param>
/// <param name="Vencimento">Vencimento da parcela.</param>
/// <param name="Paga">Indica se a parcela ja foi paga.</param>
public sealed record MinhaParcelaDamDto(int Numero, decimal Valor, DateOnly Vencimento, bool Paga);

/// <summary>2a via de DAM/boleto do proprio cidadao (projecao de leitura).</summary>
/// <param name="DamId">Identificador do DAM.</param>
/// <param name="LancamentoId">Lancamento de origem.</param>
/// <param name="ValorTotal">Valor total do documento.</param>
/// <param name="Quitado">Indica se todas as parcelas estao pagas.</param>
/// <param name="Parcelas">Parcelas do DAM.</param>
public sealed record MeuDamDto(
    Guid DamId,
    Guid LancamentoId,
    decimal ValorTotal,
    bool Quitado,
    IReadOnlyList<MinhaParcelaDamDto> Parcelas);
