namespace Tensorroot.Gov.Modules.Educacao.Domain.Merenda;

/// <summary>
/// Faixa etaria do PNAE (Resolucao FNDE 06/2020) usada para parametrizar a referencia nutricional
/// e o per capita por refeicao. Operacao local; a classificacao oficial junto ao FNDE = M10.
/// </summary>
public enum FaixaEtariaPnae
{
    /// <summary>Creche (0 a 3 anos e 11 meses).</summary>
    Creche = 0,

    /// <summary>Pre-escola (4 a 5 anos).</summary>
    PreEscola = 1,

    /// <summary>Ensino Fundamental / Medio (a partir dos 6 anos).</summary>
    EnsinoFundamentalMedio = 2,

    /// <summary>Educacao de Jovens e Adultos (EJA).</summary>
    EducacaoJovensAdultos = 3,
}

/// <summary>Tipo de refeicao ofertada no programa de alimentacao escolar (PNAE).</summary>
public enum TipoRefeicao
{
    /// <summary>Desjejum / cafe da manha.</summary>
    Desjejum = 0,

    /// <summary>Lanche da manha.</summary>
    LancheManha = 1,

    /// <summary>Almoco.</summary>
    Almoco = 2,

    /// <summary>Lanche da tarde.</summary>
    LancheTarde = 3,

    /// <summary>Jantar.</summary>
    Jantar = 4,
}

/// <summary>Dia da semana do cardapio (refeicoes planejadas por dia letivo).</summary>
public enum DiaSemanaCardapio
{
    /// <summary>Segunda-feira.</summary>
    Segunda = 1,

    /// <summary>Terca-feira.</summary>
    Terca = 2,

    /// <summary>Quarta-feira.</summary>
    Quarta = 3,

    /// <summary>Quinta-feira.</summary>
    Quinta = 4,

    /// <summary>Sexta-feira.</summary>
    Sexta = 5,
}

/// <summary>Situacao do cardapio semanal (maquina de estados Planejado -&gt; Publicado -&gt; Encerrado).</summary>
public enum SituacaoCardapio
{
    /// <summary>Em planejamento (admite edicao de itens).</summary>
    Planejado = 0,

    /// <summary>Publicado para a semana (base das distribuicoes; nao admite mais edicao de itens).</summary>
    Publicado = 1,

    /// <summary>Encerrado (semana concluida — estado terminal).</summary>
    Encerrado = 2,
}
