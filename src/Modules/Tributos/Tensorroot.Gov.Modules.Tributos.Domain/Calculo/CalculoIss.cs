using Tensorroot.Gov.Modules.Tributos.Domain.Iss;
using Tensorroot.Gov.Modules.Tributos.Domain.Nfse;
using Tensorroot.Gov.Modules.Tributos.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Tributos.Domain.Calculo;

/// <summary>
/// Modalidade de recolhimento do ISS de uma NFS-e (classificação LC 116/2003 + lei municipal).
/// </summary>
public enum ModalidadeIss
{
    /// <summary>ISS próprio (devido pelo prestador estabelecido no município — regra geral art. 3º).</summary>
    Proprio = 1,

    /// <summary>ISS retido na fonte (calculado/retido/recolhido pelo tomador — art. 6º §2º II).</summary>
    RetidoNaFonte = 2,

    /// <summary>ISS por substituição tributária (terceiro substituto — art. 6º caput; depende de lei municipal).</summary>
    SubstituicaoTributaria = 3,
}

/// <summary>
/// Memória de cálculo do ISS de UMA NFS-e (auditável): item da lista, base, alíquota aplicada,
/// modalidade e valor apurado. Determinística e fiscalizável pelo TCE-RS. Ver M6-DESIGN §2.2.
/// </summary>
/// <param name="ChaveAcesso">Chave de acesso da NFS-e.</param>
/// <param name="ItemListaServico">Item da lista LC 116.</param>
/// <param name="BaseCalculo">Base de cálculo (valor do serviço, R$).</param>
/// <param name="AliquotaPercentual">Alíquota aplicada (% — da tabela municipal vigente).</param>
/// <param name="Modalidade">Modalidade de recolhimento.</param>
/// <param name="IssApurado">ISS apurado (R$).</param>
public sealed record MemoriaIssNota(
    string ChaveAcesso,
    string ItemListaServico,
    ValorMonetario BaseCalculo,
    decimal AliquotaPercentual,
    ModalidadeIss Modalidade,
    ValorMonetario IssApurado);

/// <summary>
/// Serviço de domínio que apura o ISS de uma NFS-e a partir da tabela de alíquotas municipal vigente.
/// <para>
/// Classifica a modalidade combinando o XML (indicador de retenção) com a lei municipal (retenção
/// obrigatória / substituição por item da lista). Calcula <c>ISS = valorServico × alíquota</c>. Não há
/// número hardcoded: a alíquota vem sempre da <see cref="TabelaAliquotaIss"/>. Ver M6-DESIGN §2.2.
/// </para>
/// </summary>
public static class CalculadoraIss
{
    /// <summary>Apura o ISS de uma única NFS-e segundo a tabela de alíquotas vigente.</summary>
    /// <param name="nota">NFS-e vigente (situação normal) a apurar.</param>
    /// <param name="tabela">Tabela de alíquotas do ISS vigente na competência.</param>
    /// <returns>A memória de cálculo do ISS da nota.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se a tabela não estiver vigente, se a nota não estiver vigente para apuração, ou se o item da
    /// lista não tiver alíquota definida (lei municipal incompleta — falha explícita, nunca presume valor).
    /// </exception>
    public static MemoriaIssNota Apurar(NotaFiscalServico nota, TabelaAliquotaIss tabela)
    {
        ArgumentNullException.ThrowIfNull(nota);
        ArgumentNullException.ThrowIfNull(tabela);

        if (!tabela.Vigente)
        {
            throw new InvalidOperationException("A tabela de alíquotas do ISS informada não está vigente.");
        }

        if (!nota.VigenteParaApuracao)
        {
            throw new InvalidOperationException($"A NFS-e {nota.ChaveAcesso} está {nota.Situacao} e não entra na apuração do ISS.");
        }

        var item = tabela.ObterItem(nota.ItemListaServico)
            ?? throw new InvalidOperationException(
                $"A tabela de ISS vigente não define alíquota para o item da lista '{nota.ItemListaServico}' (LC 116). Parametrize a lei municipal.");

        var modalidade = ClassificarModalidade(nota, item);
        var baseCalculo = nota.ValorServico;
        var issApurado = baseCalculo.AplicarPercentual(item.AliquotaPercentual);

        return new MemoriaIssNota(
            nota.ChaveAcesso,
            nota.ItemListaServico,
            baseCalculo,
            item.AliquotaPercentual,
            modalidade,
            issApurado);
    }

    /// <summary>
    /// Classifica a modalidade do ISS: substituição tributária (se a lei municipal a prevê para o
    /// item) tem precedência; senão, retido na fonte se o XML indicar retenção OU a lei municipal
    /// tornar a retenção obrigatória para o item; caso contrário, próprio.
    /// </summary>
    private static ModalidadeIss ClassificarModalidade(NotaFiscalServico nota, ItemAliquotaIss item)
    {
        if (item.SubstituicaoTributaria)
        {
            return ModalidadeIss.SubstituicaoTributaria;
        }

        if (nota.IssRetidoNaFonte || item.RetencaoObrigatoria)
        {
            return ModalidadeIss.RetidoNaFonte;
        }

        return ModalidadeIss.Proprio;
    }
}
