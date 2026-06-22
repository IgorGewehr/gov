namespace Tensorroot.Gov.Modules.Cofre.Domain;

/// <summary>Ciclo de vida de um certificado A1 no cofre (A1-DESIGN §2).</summary>
public enum CertificadoStatus
{
    /// <summary>Em uso para assinar. So UM ativo por (tenant, finalidade) por vez.</summary>
    Ativo = 1,

    /// <summary>Revogado manualmente (comprometimento/encerramento).</summary>
    Revogado = 2,

    /// <summary>Expirado (NotAfter no passado).</summary>
    Expirado = 3,

    /// <summary>Substituido por um novo certificado na rotacao.</summary>
    Substituido = 4,
}
