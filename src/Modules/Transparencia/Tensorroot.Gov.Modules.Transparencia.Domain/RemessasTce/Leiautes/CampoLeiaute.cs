using Tensorroot.Gov.SharedKernel.Primitives;

namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Definição de UM campo posicional de um <see cref="RegistroLeiauteDef"/> do leiaute SIAPC/PAD: nome
/// lógico, posição inicial (1-based), tamanho fixo (em bytes/colunas ISO-8859-1), tipo, obrigatoriedade e
/// regra de preenchimento. É um objeto de valor: igualdade por (<see cref="PosicaoInicial"/>,
/// <see cref="Tamanho"/>, <see cref="Tipo"/>, <see cref="Nome"/>, <see cref="Obrigatorio"/>,
/// <see cref="Preenchimento"/>).
/// </summary>
/// <remarks>
/// A grade exata (posição/tamanho/tipo de cada campo de cada arquivo) é DIRIGIDA POR DADOS versionados
/// (JSON/seed na Infrastructure), nunca constante em C# (CLAUDE.md §7/§16). O leiaute conferido é de 2010
/// e muda em 2026 (BP/BF + DFC). NUNCA inventar posição/tamanho oficial sem marcar
/// <c>// TODO(validar-leiaute-MT-2026)</c>.
/// </remarks>
public sealed class CampoLeiaute : ValueObject
{
    private CampoLeiaute(
        string nome,
        int posicaoInicial,
        int tamanho,
        TipoCampoLeiaute tipo,
        bool obrigatorio,
        PreenchimentoCampo preenchimento)
    {
        Nome = nome;
        PosicaoInicial = posicaoInicial;
        Tamanho = tamanho;
        Tipo = tipo;
        Obrigatorio = obrigatorio;
        Preenchimento = preenchimento;
    }

    /// <summary>Nome lógico do campo (linguagem ubíqua; não vazio).</summary>
    public string Nome { get; }

    /// <summary>Posição inicial 1-based do campo dentro da linha (&gt;= 1).</summary>
    public int PosicaoInicial { get; }

    /// <summary>Largura fixa do campo em colunas/bytes ISO-8859-1 (&gt;= 1).</summary>
    public int Tamanho { get; }

    /// <summary>Tipo do campo (caractere/numérico/valor/data).</summary>
    public TipoCampoLeiaute Tipo { get; }

    /// <summary>Indica se o campo é de preenchimento obrigatório (pré-validação local).</summary>
    public bool Obrigatorio { get; }

    /// <summary>Regra de alinhamento/preenchimento (deriva do tipo quando <see cref="PreenchimentoCampo.PadraoDoTipo"/>).</summary>
    public PreenchimentoCampo Preenchimento { get; }

    /// <summary>Posição final 1-based (inclusiva) do campo.</summary>
    public int PosicaoFinal => PosicaoInicial + Tamanho - 1;

    /// <summary>Define um campo posicional do leiaute.</summary>
    /// <param name="nome">Nome lógico (não vazio).</param>
    /// <param name="posicaoInicial">Posição inicial 1-based (&gt;= 1).</param>
    /// <param name="tamanho">Largura fixa em colunas (&gt;= 1).</param>
    /// <param name="tipo">Tipo do campo.</param>
    /// <param name="obrigatorio">Se é obrigatório.</param>
    /// <param name="preenchimento">Regra de preenchimento (padrão deriva do tipo).</param>
    /// <returns>Instância de <see cref="CampoLeiaute"/>.</returns>
    /// <exception cref="ArgumentException">Se o nome for vazio.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se posição ou tamanho forem inválidos.</exception>
    public static CampoLeiaute Definir(
        string nome,
        int posicaoInicial,
        int tamanho,
        TipoCampoLeiaute tipo,
        bool obrigatorio = false,
        PreenchimentoCampo preenchimento = PreenchimentoCampo.PadraoDoTipo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentOutOfRangeException.ThrowIfLessThan(posicaoInicial, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);

        // Data tem largura fixa de 8 (ddmmaaaa) — defesa em profundidade.
        if (tipo == TipoCampoLeiaute.Data && tamanho != 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanho), tamanho, "Campo do tipo Data deve ter tamanho 8 (ddmmaaaa).");
        }

        return new CampoLeiaute(nome, posicaoInicial, tamanho, tipo, obrigatorio, preenchimento);
    }

    /// <summary>Resolve a regra efetiva de preenchimento (deriva do tipo quando não especificada).</summary>
    /// <returns>Regra de preenchimento concreta.</returns>
    public PreenchimentoCampo PreenchimentoEfetivo()
        => Preenchimento != PreenchimentoCampo.PadraoDoTipo
            ? Preenchimento
            : Tipo switch
            {
                TipoCampoLeiaute.Caractere => PreenchimentoCampo.EsquerdaEspaco,
                TipoCampoLeiaute.Numerico => PreenchimentoCampo.DireitaZero,
                TipoCampoLeiaute.Valor => PreenchimentoCampo.SinalValor,
                TipoCampoLeiaute.Data => PreenchimentoCampo.DireitaZero,
                _ => PreenchimentoCampo.EsquerdaEspaco,
            };

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nome;
        yield return PosicaoInicial;
        yield return Tamanho;
        yield return Tipo;
        yield return Obrigatorio;
        yield return Preenchimento;
    }
}
