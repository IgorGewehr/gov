using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Application.Abstractions;

/// <summary>Artefato MSC gerado: nome do arquivo e os bytes do ZIP (CSV adaptado do XBRL-GL, zipado).</summary>
/// <param name="NomeArquivo">Nome do ZIP gerado.</param>
/// <param name="Conteudo">Bytes do ZIP.</param>
public sealed record ArtefatoMsc(string NomeArquivo, ReadOnlyMemory<byte> Conteudo);

/// <summary>
/// Gerador da Matriz de Saldos Contábeis (MSC) para o SICONFI a partir da
/// <see cref="DeclaracaoFiscal"/>/<c>MatrizSaldos</c> já consolidada (M3). Produz o CSV adaptado do
/// XBRL-GL, zipado — automatizável 100%. NÃO transmite (a homologação é ato humano com e-CPF A3 no portal).
/// </summary>
/// <remarks>
/// A MSC é UMA por município, enviada SÓ pelo Executivo, consolidando o Legislativo via o atributo
/// "Poder e Órgão" — NÃO uma MSC por tenant Câmara (verificação SICONFI C-1). O formato/colunas exatos do
/// CSV/XBRL-GL seguem as Regras Gerais MSC 2026 — <c>// TODO(validar-leiaute-MT-2026)</c> para colunas/
/// atributos do exercício.
/// </remarks>
public interface IGeradorMsc
{
    /// <summary>Gera o artefato MSC (CSV adaptado do XBRL-GL, zipado) a partir da declaração consolidada.</summary>
    /// <param name="declaracao">Declaração fiscal consolidada (com a matriz de saldos).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O artefato MSC pronto para upload manual no portal SICONFI.</returns>
    Task<ArtefatoMsc> GerarAsync(DeclaracaoFiscal declaracao, CancellationToken cancellationToken);
}
