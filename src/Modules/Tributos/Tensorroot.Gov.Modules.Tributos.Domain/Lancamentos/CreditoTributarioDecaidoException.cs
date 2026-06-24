namespace Tensorroot.Gov.Modules.Tributos.Domain.Lancamentos;

/// <summary>
/// Erro de domínio: tentativa de constituir (lançar) crédito tributário já alcançado pela
/// DECADÊNCIA (CTN art. 173, I). O direito de lançar extingue-se em 5 anos (prazo parametrizável
/// por tenant) contados do PRIMEIRO DIA DO EXERCÍCIO SEGUINTE àquele em que o lançamento poderia
/// ter sido efetuado. Crédito constituído após esse prazo é NULO e insanável; por isso o
/// lançamento é recusado e NENHUM evento de constituição é publicado (README §5 / BDD §8).
/// </summary>
public sealed class CreditoTributarioDecaidoException : InvalidOperationException
{
    /// <summary>Cria a exceção com a mensagem padronizada de decadência.</summary>
    /// <param name="exercicioFatoGerador">Exercício do fato gerador.</param>
    /// <param name="dataLimiteDecadencia">Data em que o direito de lançar se extingue (CTN art. 173, I); a constituição deve ser ANTERIOR a ela.</param>
    /// <param name="dataConstituicao">Data em que se tentou constituir o crédito.</param>
    public CreditoTributarioDecaidoException(int exercicioFatoGerador, DateOnly dataLimiteDecadencia, DateOnly dataConstituicao)
        : base($"Lançamento recusado por decadência (CTN art. 173, I): o crédito do fato gerador de {exercicioFatoGerador} decaiu em {dataLimiteDecadencia:dd/MM/yyyy}; constituição tentada em {dataConstituicao:dd/MM/yyyy}.")
    {
        ExercicioFatoGerador = exercicioFatoGerador;
        DataLimiteDecadencia = dataLimiteDecadencia;
        DataConstituicao = dataConstituicao;
    }

    /// <summary>Exercício do fato gerador.</summary>
    public int ExercicioFatoGerador { get; }

    /// <summary>Data-limite da decadência (CTN art. 173, I).</summary>
    public DateOnly DataLimiteDecadencia { get; }

    /// <summary>Data em que se tentou constituir o crédito.</summary>
    public DateOnly DataConstituicao { get; }
}
