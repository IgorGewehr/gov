namespace Tensorroot.Gov.Modules.Patrimonio.Domain.Bens;

/// <summary>Tipo do bem patrimonial.</summary>
public enum TipoBem
{
    /// <summary>Bem móvel.</summary>
    Movel = 1,

    /// <summary>Bem imóvel (desmembrável em terreno + benfeitoria).</summary>
    Imovel = 2,
}

/// <summary>Situação (estado) do bem patrimonial no ciclo de vida.</summary>
public enum SituacaoBemPatrimonial
{
    /// <summary>Incorporado, ainda sem tombo.</summary>
    EmIncorporacao = 1,

    /// <summary>Tombado e ativo no acervo.</summary>
    Tombado = 2,

    /// <summary>Em cessão/comodato a terceiro.</summary>
    Cedido = 3,

    /// <summary>Baixado (terminal).</summary>
    Baixada = 4,

    /// <summary>Alienado (terminal).</summary>
    Alienada = 5,
}
