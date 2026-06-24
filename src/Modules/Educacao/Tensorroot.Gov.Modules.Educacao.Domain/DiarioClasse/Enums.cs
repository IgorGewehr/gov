namespace Tensorroot.Gov.Modules.Educacao.Domain.DiarioClasse;

/// <summary>Situacao (estado) do diario de classe.</summary>
public enum SituacaoDiario
{
    /// <summary>Diario aberto, admitindo lancamentos (estado inicial).</summary>
    Aberto = 1,

    /// <summary>Resultado anual apurado (terminal para lancamentos do periodo).</summary>
    Apurado = 2,
}

/// <summary>Resultado anual apurado do aluno no diario.</summary>
public enum ResultadoAluno
{
    /// <summary>Frequencia maior ou igual a 75% e medias suficientes.</summary>
    Aprovado = 1,

    /// <summary>Reprovado por nota/medias insuficientes.</summary>
    Reprovado = 2,

    /// <summary>Frequencia menor que 75% da carga horaria (LDB).</summary>
    ReprovadoPorFrequencia = 3,

    /// <summary>
    /// Cursando/pendente: frequencia suficiente, porem sem registro de rendimento (nenhuma nota
    /// lancada). Nao ha base avaliativa para concluir aprovacao/reprovacao por nota — fail-closed:
    /// nunca aprova de forma vacua. Estado de pendencia que exige lancamento de notas antes da
    /// apuracao definitiva (nao alimenta "Aprovado" na Situacao do Aluno do Censo/INEP).
    /// </summary>
    Cursando = 4,
}
