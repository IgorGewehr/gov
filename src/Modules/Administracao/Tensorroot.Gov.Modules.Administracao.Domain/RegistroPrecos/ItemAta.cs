using Tensorroot.Gov.Modules.Administracao.Domain.Catalogo;
using Tensorroot.Gov.Modules.Administracao.Domain.ValueObjects;
using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Administracao.Domain.RegistroPrecos;

/// <summary>Identificador forte de um <see cref="ItemAta"/>.</summary>
/// <param name="Value">Valor GUID subjacente.</param>
public readonly record struct ItemAtaId(Guid Value)
{
    /// <summary>Gera um novo identificador.</summary>
    /// <returns>Novo <see cref="ItemAtaId"/>.</returns>
    public static ItemAtaId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Item de uma Ata de Registro de Precos: vincula um item do catalogo a um preco registrado, uma
/// quantidade maxima registrada (estimativa do orgao gerenciador + participantes — art. 86, §1º) e o
/// fornecedor beneficiario. Controla DOIS saldos distintos (Lei 14.133/2021, art. 86, §§ 4º e 5º; Dec.
/// 11.462/2023, art. 32):
/// <list type="bullet">
/// <item>o <b>saldo registrado</b> — consumido por gerenciador/participantes (uso da propria ata), nao
/// pode ser excedido pelo conjunto de consumos diretos;</item>
/// <item>o <b>saldo de adesao (carona)</b> — quantidade adicional que orgaos nao participantes podem
/// aderir, limitada a 50% do registrado POR orgao aderente (§4º) e ao DOBRO (200%) do registrado no
/// TOTAL de adesoes, independentemente do numero de aderentes (§5º).</item>
/// </list>
/// Admite ainda <b>remanejamento</b> do quantitativo registrado (Dec. 11.462/2023, art. 33). Entidade
/// filha da <see cref="Ata"/>.
/// </summary>
public sealed class ItemAta : Entity<ItemAtaId>
{
    private ItemAta()
    {
    }

    private ItemAta(
        ItemAtaId id,
        ItemCatalogoId itemCatalogoId,
        Guid fornecedorBeneficiarioId,
        ValorMonetario precoRegistrado,
        decimal quantidadeRegistrada)
        : base(id)
    {
        ItemCatalogoId = itemCatalogoId;
        FornecedorBeneficiarioId = fornecedorBeneficiarioId;
        PrecoRegistrado = precoRegistrado;
        QuantidadeRegistrada = quantidadeRegistrada;
        QuantidadeContratada = 0m;
        QuantidadeAderida = 0m;
    }

    /// <summary>Item de catalogo registrado.</summary>
    public ItemCatalogoId ItemCatalogoId { get; private set; }

    /// <summary>Fornecedor beneficiario do preco registrado para este item.</summary>
    public Guid FornecedorBeneficiarioId { get; private set; }

    /// <summary>Preco unitario registrado.</summary>
    public ValorMonetario PrecoRegistrado { get; private set; } = default!;

    /// <summary>
    /// Quantidade maxima registrada para o orgao gerenciador + participantes (art. 86, §1º). Base de
    /// calculo dos limites de adesao (50% por aderente; 200% no total).
    /// </summary>
    public decimal QuantidadeRegistrada { get; private set; }

    /// <summary>Quantidade ja contratada/empenhada por gerenciador/participantes (consome o saldo registrado).</summary>
    public decimal QuantidadeContratada { get; private set; }

    /// <summary>Quantidade ja aderida por orgaos nao participantes (carona — consome o saldo de adesao).</summary>
    public decimal QuantidadeAderida { get; private set; }

    /// <summary>Saldo do quantitativo registrado ainda disponivel para gerenciador/participantes.</summary>
    public decimal SaldoDisponivel => QuantidadeRegistrada - QuantidadeContratada;

    /// <summary>
    /// Limite TOTAL de adesoes (carona) deste item: o dobro (200%) do quantitativo registrado, conforme
    /// art. 86, §5º, da Lei 14.133/2021 e art. 32, II, do Dec. 11.462/2023.
    /// </summary>
    public decimal LimiteTotalAdesao => QuantidadeRegistrada * QuantidadeMaximaAdesaoTotalEmRegistros;

    /// <summary>Saldo ainda disponivel para novas adesoes (carona), respeitado o teto total de 200%.</summary>
    public decimal SaldoAdesaoDisponivel => LimiteTotalAdesao - QuantidadeAderida;

    /// <summary>Limite de adesao POR orgao nao participante: 50% do registrado (art. 86, §4º; Dec. 11.462/2023, art. 32, I).</summary>
    public decimal LimiteAdesaoPorOrgao => QuantidadeRegistrada * FracaoMaximaAdesaoPorOrgao;

    /// <summary>Teto de adesao por orgao aderente, em fracao do registrado (art. 86, §4º): 50%.</summary>
    private const decimal FracaoMaximaAdesaoPorOrgao = 0.5m;

    /// <summary>Teto total de adesoes, em multiplos do registrado (art. 86, §5º): 2 (dobro).</summary>
    private const decimal QuantidadeMaximaAdesaoTotalEmRegistros = 2m;

    /// <summary>Cria um item de ata com preco e quantidade registrados.</summary>
    /// <param name="itemCatalogoId">Item de catalogo registrado.</param>
    /// <param name="fornecedorBeneficiarioId">Fornecedor beneficiario.</param>
    /// <param name="precoRegistrado">Preco unitario registrado (positivo).</param>
    /// <param name="quantidadeRegistrada">Quantidade maxima registrada (positiva).</param>
    /// <returns>Novo <see cref="ItemAta"/>.</returns>
    /// <exception cref="ArgumentNullException">Se o preco for nulo.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva ou o fornecedor for vazio.</exception>
    public static ItemAta Criar(
        ItemCatalogoId itemCatalogoId,
        Guid fornecedorBeneficiarioId,
        ValorMonetario precoRegistrado,
        decimal quantidadeRegistrada)
    {
        ArgumentNullException.ThrowIfNull(precoRegistrado);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidadeRegistrada);
        if (fornecedorBeneficiarioId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(fornecedorBeneficiarioId), "Fornecedor beneficiario obrigatorio.");
        }

        return new ItemAta(ItemAtaId.New(), itemCatalogoId, fornecedorBeneficiarioId, precoRegistrado, quantidadeRegistrada);
    }

    /// <summary>
    /// Consome quantidade do saldo REGISTRADO (uso direto do gerenciador ou participante), abatendo do
    /// disponivel. Nao computa para os limites de adesao (que sao especificos da carona).
    /// </summary>
    /// <param name="quantidade">Quantidade a contratar (positiva).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se a quantidade exceder o saldo registrado disponivel.</exception>
    public void ConsumirSaldo(decimal quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (quantidade > SaldoDisponivel)
        {
            throw new InvalidOperationException(
                $"Quantidade {quantidade} excede o saldo registrado disponivel {SaldoDisponivel} do item.");
        }

        QuantidadeContratada += quantidade;
    }

    /// <summary>
    /// Consome quantidade do saldo de ADESAO (carona — orgao nao participante), validando os DOIS tetos
    /// legais (Lei 14.133/2021, art. 86, §§ 4º e 5º; Dec. 11.462/2023, art. 32):
    /// (a) o ja aderido por ESTE orgao + a nova quantidade nao pode exceder 50% do registrado; e
    /// (b) o total geral de adesoes nao pode exceder o dobro (200%) do registrado.
    /// </summary>
    /// <param name="quantidade">Quantidade a aderir (positiva).</param>
    /// <param name="jaAderidoPeloOrgao">Quantidade que o mesmo orgao ja aderiu neste item.</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a quantidade nao for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se exceder o teto por orgao (50%) ou o teto total (200%).</exception>
    public void ConsumirAdesao(decimal quantidade, decimal jaAderidoPeloOrgao)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        ArgumentOutOfRangeException.ThrowIfNegative(jaAderidoPeloOrgao);

        if (jaAderidoPeloOrgao + quantidade > LimiteAdesaoPorOrgao)
        {
            throw new InvalidOperationException(
                $"Adesao excede o limite por orgao nao participante (50% do registrado = {LimiteAdesaoPorOrgao}; " +
                $"art. 86, §4º, Lei 14.133/2021). Ja aderido por este orgao: {jaAderidoPeloOrgao}; solicitado: {quantidade}.");
        }

        if (quantidade > SaldoAdesaoDisponivel)
        {
            throw new InvalidOperationException(
                $"Adesao excede o limite TOTAL de adesoes (dobro do registrado = {LimiteTotalAdesao}; " +
                $"art. 86, §5º, Lei 14.133/2021). Saldo de adesao disponivel: {SaldoAdesaoDisponivel}; solicitado: {quantidade}.");
        }

        QuantidadeAderida += quantidade;
    }

    /// <summary>
    /// Remaneja (ajusta) o quantitativo registrado deste item (Dec. 11.462/2023, art. 33): o novo
    /// quantitativo nunca pode ficar abaixo do que ja foi efetivamente consumido por gerenciador/
    /// participantes (estado impossivel).
    /// </summary>
    /// <param name="novaQuantidadeRegistrada">Novo quantitativo registrado (positivo).</param>
    /// <exception cref="ArgumentOutOfRangeException">Se a nova quantidade nao for positiva.</exception>
    /// <exception cref="InvalidOperationException">Se a nova quantidade for inferior ao ja contratado.</exception>
    public void Remanejar(decimal novaQuantidadeRegistrada)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(novaQuantidadeRegistrada);
        if (novaQuantidadeRegistrada < QuantidadeContratada)
        {
            throw new InvalidOperationException(
                $"Quantitativo remanejado {novaQuantidadeRegistrada} e inferior ao ja contratado {QuantidadeContratada}; rejeitado.");
        }

        QuantidadeRegistrada = novaQuantidadeRegistrada;
    }
}
