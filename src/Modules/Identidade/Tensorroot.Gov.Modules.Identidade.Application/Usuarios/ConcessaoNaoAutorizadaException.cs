using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;

namespace Tensorroot.Gov.Modules.Identidade.Application.Usuarios;

/// <summary>
/// Falha de NEGOCIO ao conceder uma atribuicao de papel com escopo: a regra-mae I4 ("nao delega o
/// que nao tem" — MODELO §3.1/§4/§8) reprovou a concessao. Carrega o motivo de dominio
/// (<see cref="MotivoConcessaoNegada"/>) para a borda HTTP traduzir em 403 com mensagem clara.
/// </summary>
public sealed class ConcessaoNaoAutorizadaException : Exception
{
    /// <summary>Cria a excecao a partir do resultado de dominio da verificacao.</summary>
    /// <param name="resultado">Resultado negado da verificacao I4.</param>
    public ConcessaoNaoAutorizadaException(ResultadoConcessao resultado)
        : base(Mensagem(resultado))
    {
        ArgumentNullException.ThrowIfNull(resultado);
        Motivo = resultado.Motivo;
        PermissaoFaltante = resultado.PermissaoFaltante;
    }

    /// <summary>Motivo de dominio da negacao.</summary>
    public MotivoConcessaoNegada Motivo { get; }

    /// <summary>Permissao especifica que faltou ao concedente, quando aplicavel.</summary>
    public string? PermissaoFaltante { get; }

    private static string Mensagem(ResultadoConcessao resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        return resultado.Motivo switch
        {
            MotivoConcessaoNegada.SemPoderAdministrativo =>
                "Concessao negada (I4): voce nao tem poder de gerenciar usuarios (identidade.usuarios.gerenciar).",
            MotivoConcessaoNegada.UnidadeInexistente =>
                "Concessao negada: a unidade organizacional alvo nao existe no tenant.",
            MotivoConcessaoNegada.ForaDoEscopoAdministrativo =>
                "Concessao negada (I4): voce nao administra a unidade organizacional alvo (ou parte de sua subarvore).",
            MotivoConcessaoNegada.PermissaoNaoPossuida =>
                $"Concessao negada (I4 - nao delega o que nao tem): voce nao possui a permissao '{resultado.PermissaoFaltante}' no escopo alvo.",
            _ => "Concessao negada.",
        };
    }
}
