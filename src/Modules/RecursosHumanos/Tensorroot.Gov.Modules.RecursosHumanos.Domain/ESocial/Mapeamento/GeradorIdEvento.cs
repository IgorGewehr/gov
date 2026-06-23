using System.Globalization;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

/// <summary>
/// Monta o atributo <c>Id</c> de um evento eSocial. Formato CONCEITUAL do MOS: <c>ID</c> + tpInsc(1) +
/// nrInsc(14, completado) + AAAAMMDDHHMMSS + sequencial(5) = 36 chars.
/// // TODO(validar-oficial): formato/comprimento exatos do Id no MOS da versao travada e regra do
/// sequencial. Aqui produzimos um Id ESTRUTURALMENTE valido e estavel por instante+sequencial.
/// </summary>
public static class GeradorIdEvento
{
    /// <summary>Monta o Id do evento.</summary>
    /// <param name="tpInsc">Tipo de inscricao do declarante.</param>
    /// <param name="nrInsc">Inscricao do declarante (CNPJ/CPF, so digitos).</param>
    /// <param name="instante">Instante de geracao.</param>
    /// <param name="sequencial">Sequencial (1..99999) no instante.</param>
    /// <returns>Id do evento (36 chars).</returns>
    /// <exception cref="ArgumentException">Se a inscricao for vazia.</exception>
    public static string Montar(TipoInscricao tpInsc, string nrInsc, DateTimeOffset instante, int sequencial)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nrInsc);
        var inscricao = new string(nrInsc.Where(char.IsDigit).ToArray()).PadRight(14, '0')[..14];
        var carimbo = instante.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var seq = (Math.Clamp(sequencial, 1, 99999)).ToString("00000", CultureInfo.InvariantCulture);
        return $"ID{(int)tpInsc}{inscricao}{carimbo}{seq}";
    }
}
