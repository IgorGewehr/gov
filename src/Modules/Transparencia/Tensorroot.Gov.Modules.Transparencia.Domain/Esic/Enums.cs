namespace Tensorroot.Gov.Modules.Transparencia.Domain.Esic;

/// <summary>
/// Situacao do pedido de informacao (LAI Lei 12.527/2011). Maquina de estados:
/// <c>Aberto -&gt; EmAtendimento -&gt; (Respondido|Indeferido) -&gt; [RecursoAberto -&gt; RecursoRespondido] -&gt; Encerrado</c>.
/// </summary>
public enum SituacaoPedidoSic
{
    /// <summary>Pedido protocolado, aguardando atendimento.</summary>
    Aberto = 1,

    /// <summary>Pedido em atendimento pelo orgao.</summary>
    EmAtendimento = 2,

    /// <summary>Pedido respondido (acesso concedido — LAI art. 11).</summary>
    Respondido = 3,

    /// <summary>Pedido indeferido (acesso negado, com fundamento legal — LAI art. 11 §1o).</summary>
    Indeferido = 4,

    /// <summary>Recurso interposto pelo cidadao contra a resposta/indeferimento (LAI art. 15).</summary>
    RecursoAberto = 5,

    /// <summary>Recurso decidido (deferido/indeferido).</summary>
    RecursoRespondido = 6,

    /// <summary>Pedido encerrado (ciclo concluido).</summary>
    Encerrado = 7,
}

/// <summary>Forma de resposta solicitada pelo cidadao (LAI art. 11 §4o / Dec. 7.724/2012).</summary>
public enum FormaResposta
{
    /// <summary>Resposta por e-mail/portal (eletronica).</summary>
    Email = 1,

    /// <summary>Retirada presencial no orgao.</summary>
    RetiradaPresencial = 2,

    /// <summary>Envio postal (correspondencia fisica).</summary>
    Correspondencia = 3,

    /// <summary>Consulta presencial ao documento.</summary>
    ConsultaPresencial = 4,
}

/// <summary>Instancia do recurso administrativo (LAI art. 15-16).</summary>
public enum InstanciaRecurso
{
    /// <summary>1a instancia (autoridade hierarquicamente superior).</summary>
    Primeira = 1,

    /// <summary>2a instancia (autoridade maxima do orgao).</summary>
    Segunda = 2,
}

/// <summary>Resultado da decisao de um recurso.</summary>
public enum ResultadoRecurso
{
    /// <summary>Recurso provido (acesso concedido).</summary>
    Provido = 1,

    /// <summary>Recurso parcialmente provido.</summary>
    ParcialmenteProvido = 2,

    /// <summary>Recurso negado/improvido.</summary>
    Negado = 3,
}
