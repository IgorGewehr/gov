namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;

/// <summary>Situacao atual do vinculo do servidor no ciclo de vida.</summary>
public enum SituacaoServidor
{
    /// <summary>Nomeado/admitido (provimento registrado; estado inicial).</summary>
    Nomeado = 1,

    /// <summary>Posse registrada.</summary>
    Empossado = 2,

    /// <summary>Em efetivo exercicio.</summary>
    EmExercicio = 3,

    /// <summary>Estavel (3 anos de efetivo exercicio — efetivos).</summary>
    Estavel = 4,

    /// <summary>Afastado temporariamente.</summary>
    Afastado = 5,

    /// <summary>Desligado (terminal).</summary>
    Desligado = 6,
}
