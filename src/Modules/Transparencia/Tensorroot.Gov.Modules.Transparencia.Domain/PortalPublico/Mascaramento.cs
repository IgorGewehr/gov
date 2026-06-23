using System.Globalization;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.PortalPublico;

/// <summary>
/// Minimizacao LGPD de documentos para a superficie PUBLICA (transparencia ativa, Dec. 7.724/2012 +
/// LGPD art. 6 III). O CPF NUNCA aparece integral em nenhuma superficie publica: e gravado JA mascarado
/// no read model (na projecao do evento), de forma deterministica. CNPJ de fornecedor/empresa e dado
/// publico (razao social/contratos), logo NAO e mascarado.
/// </summary>
public static class Mascaramento
{
    /// <summary>Marcador opaco usado quando nao ha documento ou o formato e desconhecido.</summary>
    public const string SemDocumento = "***";

    /// <summary>
    /// Mascara um documento de pessoa fisica/juridica para exibicao publica. Para CPF (11 digitos)
    /// expoe apenas os 6 digitos centrais no padrao da Receita/CGU (<c>***.456.789-**</c>), ocultando
    /// inicio e fim. Para CNPJ (14 digitos) NAO mascara (dado publico de pessoa juridica). Entrada
    /// vazia/nula vira <see cref="SemDocumento"/>.
    /// </summary>
    /// <param name="documentoSomenteDigitos">Documento somente digitos (CPF 11 / CNPJ 14) ou nulo.</param>
    /// <returns>Documento mascarado pronto para superficie publica.</returns>
    public static string MascararDocumento(string? documentoSomenteDigitos)
    {
        var digitos = ApenasDigitos(documentoSomenteDigitos);
        return digitos.Length switch
        {
            11 => string.Create(
                CultureInfo.InvariantCulture,
                $"***.{digitos.AsSpan(3, 3)}.{digitos.AsSpan(6, 3)}-**"),
            14 => FormatarCnpj(digitos),
            _ => SemDocumento,
        };
    }

    /// <summary>Formata um CNPJ (14 digitos) com mascara padrao — dado PUBLICO de pessoa juridica.</summary>
    /// <param name="digitos">14 digitos do CNPJ.</param>
    /// <returns>CNPJ formatado.</returns>
    private static string FormatarCnpj(string digitos)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{digitos[..2]}.{digitos.AsSpan(2, 3)}.{digitos.AsSpan(5, 3)}/{digitos.AsSpan(8, 4)}-{digitos.AsSpan(12, 2)}");

    private static string ApenasDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        Span<char> destino = stackalloc char[valor.Length];
        var escritos = 0;
        foreach (var caractere in valor)
        {
            if (char.IsDigit(caractere))
            {
                destino[escritos++] = caractere;
            }
        }

        return new string(destino[..escritos]);
    }
}
