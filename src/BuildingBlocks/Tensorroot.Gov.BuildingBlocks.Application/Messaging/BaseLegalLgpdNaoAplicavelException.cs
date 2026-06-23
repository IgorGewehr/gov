using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.BuildingBlocks.Application.Messaging;

/// <summary>
/// Erro de DENY-BY-DEFAULT da accountability LGPD (LG-A2): a hipotese legal aplicada a um acesso
/// sensivel (<see cref="ISensivelLgpd.BaseLegal"/>) NAO pertence ao conjunto de bases legais
/// aplicaveis ao recurso (<see cref="ISensivelLgpd.BasesLegaisAplicaveis"/>), ou nenhuma base legal
/// foi declarada. O acesso e abortado e a TENTATIVA e auditada (trilha de negativa), de modo que
/// nao exista leitura de dado sensivel sem base legal aplicavel registrada (art. 7/11/37 LGPD).
/// </summary>
public sealed class BaseLegalLgpdNaoAplicavelException : Exception
{
    /// <summary>Cria a excecao de base legal nao aplicavel ao recurso sensivel.</summary>
    /// <param name="entidade">Recurso sensivel cujo acesso foi negado.</param>
    /// <param name="baseLegalAplicada">Hipotese legal que a operacao tentou aplicar.</param>
    public BaseLegalLgpdNaoAplicavelException(string entidade, BaseLegalLgpd baseLegalAplicada)
        : base(
            $"Acesso negado (LGPD): a base legal '{baseLegalAplicada}' nao e aplicavel ao recurso " +
            $"sensivel '{entidade}'. Sem hipotese legal aplicavel, o acesso nao pode ser autorizado.")
    {
        Entidade = entidade;
        BaseLegalAplicada = baseLegalAplicada;
    }

    /// <summary>Recurso sensivel cujo acesso foi negado.</summary>
    public string Entidade { get; }

    /// <summary>Hipotese legal que a operacao tentou aplicar (e foi rejeitada).</summary>
    public BaseLegalLgpd BaseLegalAplicada { get; }
}
