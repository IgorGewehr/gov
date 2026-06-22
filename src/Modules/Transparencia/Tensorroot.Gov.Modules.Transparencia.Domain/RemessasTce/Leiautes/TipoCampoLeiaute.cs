namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Tipo de um <see cref="CampoLeiaute"/> no leiaute posicional SIAPC/PAD (MT Vol. V §2). Determina o
/// alinhamento, o caractere de preenchimento e a formatação do valor ao serializar a linha de largura fixa.
/// </summary>
/// <remarks>
/// CONFIANÇA: ALTA quanto ao formato (ISO-8859-1, largura fixa); INCERTO quanto aos campos exatos de cada
/// arquivo em 2026 — ver <c>// TODO(validar-leiaute-MT-2026)</c>.
/// </remarks>
public enum TipoCampoLeiaute
{
    /// <summary>Caractere: alinhado à ESQUERDA, preenchido com ESPAÇOS à direita.</summary>
    Caractere = 1,

    /// <summary>Numérico: alinhado à DIREITA, preenchido com ZEROS à esquerda (sem sinal, sem separador).</summary>
    Numerico = 2,

    /// <summary>
    /// Valor monetário em CENTAVOS (sem ponto/vírgula), alinhado à direita com zeros à esquerda e
    /// SINAL (<c>+</c> = 0x2B / <c>-</c> = 0x2D) na posição inicial do campo.
    /// </summary>
    Valor = 3,

    /// <summary>Data no formato <c>ddmmaaaa</c> (8 posições, sem separadores).</summary>
    Data = 4,
}

/// <summary>
/// Regra de preenchimento/alinhamento de um <see cref="CampoLeiaute"/>. Redundante com
/// <see cref="TipoCampoLeiaute"/> por padrão, mas explicitável para campos que fogem à convenção do tipo
/// (defesa em profundidade — o MT lista exceções pontuais).
/// </summary>
public enum PreenchimentoCampo
{
    /// <summary>Deriva do <see cref="TipoCampoLeiaute"/> (convenção padrão).</summary>
    PadraoDoTipo = 0,

    /// <summary>Alinhado à esquerda, completado com espaços à direita (texto).</summary>
    EsquerdaEspaco = 1,

    /// <summary>Alinhado à direita, completado com zeros à esquerda (numérico).</summary>
    DireitaZero = 2,

    /// <summary>Numérico com sinal na posição inicial (valor em centavos).</summary>
    SinalValor = 3,
}
