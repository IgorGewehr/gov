using Tensorroot.Gov.Modules.RecursosHumanos.Application.Configuracao;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.ESocial.Mapeamento;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.ESocial;

/// <summary>
/// Mapeia os <see cref="ParametrosESocial"/> do tenant para os grupos de inscricao do empregador exigidos
/// pelo S-1.3 (<c>ideEmpregador</c> e <c>ideEstabLot</c>). Centraliza a derivacao tpInsc=CNPJ (ente publico)
/// e a normalizacao da inscricao, sem hardcode no gerador de XML (CLAUDE.md §7).
/// </summary>
public static class InscricaoEmpregadorFactory
{
    /// <summary>Constroi a inscricao do empregador (<c>ideEmpregador</c>) a partir da configuracao.</summary>
    /// <param name="parametros">Parametros eSocial do tenant.</param>
    /// <returns>Inscricao do empregador (tpInsc=CNPJ).</returns>
    /// <exception cref="InvalidOperationException">Se o CNPJ do ente nao estiver configurado.</exception>
    public static InscricaoEmpregador De(ParametrosESocial parametros)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        var cnpj = ExigirCnpjEnte(parametros);
        return new InscricaoEmpregador((int)TipoInscricao.Cnpj, cnpj);
    }

    /// <summary>Constroi o estabelecimento+lotacao (<c>ideEstabLot</c>) do S-1200/S-1202 a partir da configuracao.</summary>
    /// <param name="parametros">Parametros eSocial do tenant.</param>
    /// <returns>Estabelecimento + lotacao tributaria.</returns>
    /// <exception cref="InvalidOperationException">Se o CNPJ do ente nao estiver configurado.</exception>
    public static EstabelecimentoLotacao EstabLotacao(ParametrosESocial parametros)
    {
        ArgumentNullException.ThrowIfNull(parametros);
        var cnpj = ExigirCnpjEnte(parametros);
        return new EstabelecimentoLotacao((int)TipoInscricao.Cnpj, cnpj, parametros.CodLotacao);
    }

    private static string ExigirCnpjEnte(ParametrosESocial parametros)
    {
        if (string.IsNullOrWhiteSpace(parametros.CnpjEnte))
        {
            throw new InvalidOperationException("CNPJ do ente nao configurado (RecursosHumanos:ESocial:CnpjEnte).");
        }

        return parametros.CnpjEnte;
    }
}
