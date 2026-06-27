using Tensorroot.Gov.Modules.Transparencia.Domain.DeclaracoesFiscais;

namespace Tensorroot.Gov.Modules.Transparencia.Infrastructure.Integracoes;

/// <summary>
/// Excecao base das validacoes DURAS (fail-CLOSED) da MSC no caminho de geracao do CSV SICONFI. Uma matriz
/// que viole qualquer destas regras seria rejeitada pelo e-Validador SICONFI — o gerador NAO pode emitir um
/// artefato invalido (Regras Gerais MSC 2026, Anexo I Port. STN 642/2019, "Observacoes Importantes").
/// </summary>
public abstract class MatrizMscInvalidaParaCsvException : InvalidOperationException
{
    /// <summary>Cria a excecao com a mensagem informada.</summary>
    /// <param name="message">Descricao da violacao.</param>
    protected MatrizMscInvalidaParaCsvException(string message)
        : base(message)
    {
    }
}

/// <summary>Linha da MSC sem o Poder/Orgao (PO) — obrigatorio em TODAS as contas (IC nº1).</summary>
public sealed class MscLinhaSemPoderOrgaoCsvException : MatrizMscInvalidaParaCsvException
{
    /// <summary>Cria a excecao para a conta sem PO.</summary>
    /// <param name="contaPcasp">Conta PCASP da linha sem PO.</param>
    public MscLinhaSemPoderOrgaoCsvException(string contaPcasp)
        : base($"Linha da MSC sem Poder/Orgao (PO) valido: conta '{contaPcasp}'. " +
               "O PO (5 digitos) e obrigatorio em TODAS as contas (Regras Gerais MSC 2026, IC nº1); " +
               "sem ele a recepcao SICONFI rejeita a remessa.")
        => ContaPcasp = contaPcasp;

    /// <summary>Conta PCASP da linha que disparou a violacao.</summary>
    public string ContaPcasp { get; }
}

/// <summary>Classe contabil (Patrimonial/Orcamentaria/Controle) com balanco D!=C na MSC.</summary>
public sealed class MscClasseDesbalanceadaCsvException : MatrizMscInvalidaParaCsvException
{
    /// <summary>Cria a excecao para a classe desbalanceada.</summary>
    /// <param name="classe">Classe contabil (natureza de informacao).</param>
    /// <param name="totalDevedor">Soma dos saldos devedores da classe.</param>
    /// <param name="totalCredor">Soma dos saldos credores da classe.</param>
    public MscClasseDesbalanceadaCsvException(string classe, decimal totalDevedor, decimal totalCredor)
        : base($"Classe contabil '{classe}' desbalanceada na MSC: " +
               $"debito {totalDevedor:0.00} != credito {totalCredor:0.00}. " +
               "Cada classe (Patrimonial/Orcamentaria/Controle) deve fechar D=C (Regras Gerais MSC 2026).")
    {
        Classe = classe;
        TotalDevedor = totalDevedor;
        TotalCredor = totalCredor;
    }

    /// <summary>Classe contabil desbalanceada.</summary>
    public string Classe { get; }

    /// <summary>Soma dos saldos devedores da classe.</summary>
    public decimal TotalDevedor { get; }

    /// <summary>Soma dos saldos credores da classe.</summary>
    public decimal TotalCredor { get; }
}

/// <summary>Matriz com balanco D!=C global (somatorio geral) na MSC.</summary>
public sealed class MscMatrizDesbalanceadaCsvException : MatrizMscInvalidaParaCsvException
{
    /// <summary>Cria a excecao para a matriz desbalanceada globalmente.</summary>
    /// <param name="totalDevedor">Soma global dos saldos devedores.</param>
    /// <param name="totalCredor">Soma global dos saldos credores.</param>
    public MscMatrizDesbalanceadaCsvException(decimal totalDevedor, decimal totalCredor)
        : base($"Matriz da MSC desbalanceada (global): debito {totalDevedor:0.00} != credito {totalCredor:0.00}. " +
               "A partida dobrada exige Sigma D = Sigma C (Regras Gerais MSC 2026).")
    {
        TotalDevedor = totalDevedor;
        TotalCredor = totalCredor;
    }

    /// <summary>Soma global dos saldos devedores.</summary>
    public decimal TotalDevedor { get; }

    /// <summary>Soma global dos saldos credores.</summary>
    public decimal TotalCredor { get; }
}

/// <summary>
/// Aplica, no caminho de geracao do CSV da MSC (SICONFI), as validacoes DURAS que o agregado robusto
/// <c>MatrizSaldosContabeis</c> (modulo Financas) ja faz na ORIGEM — mas que se PERDEM ao atravessar a ponte
/// de integracao (so as linhas de SALDO FINAL, ja em texto livre, chegam a <see cref="MatrizSaldos"/> da
/// Transparencia). Como Transparencia.Infrastructure nao pode referenciar o Domain de Financas (CLAUDE.md §2),
/// este validador re-impoe FAIL-CLOSED, sobre os dados que o CSV de fato possui, as regras que o e-Validador
/// SICONFI cobra (Regras Gerais MSC 2026, Anexo I Port. STN 642/2019):
/// <list type="number">
///   <item>Poder/Orgao (PO) presente e valido em TODAS as linhas (IC nº1).</item>
///   <item>Balanco D=C por CLASSE contabil (Patrimonial/Orcamentaria/Controle), nao so o total geral.</item>
///   <item>Balanco D=C global (partida dobrada).</item>
/// </list>
/// A consistencia <c>saldo_inicial + movimento = saldo_final</c> por conta (3ª regra dura do SICONFI) e
/// validada na ORIGEM (<c>MatrizSaldosContabeis.Montar</c>, Financas), pois as linhas de saldo inicial e
/// movimento NAO trafegam ate esta camada — aqui so existe o saldo final. Correcao P0-1/P0-2 da AUDITORIA-FINAL.
/// </summary>
public static class ValidadorMscCsv
{
    // Tolerancia de arredondamento ao conferir o fechamento (centavos) — alinhada a MatrizSaldosContabeis
    // (Financas, ToleranciaFechamento=0.01) e a MCASP (partida dobrada admite diferenca de centavos).
    private const decimal ToleranciaFechamento = 0.01m;

    // PO (Poder/Orgao): 5 digitos (2 poder + 3 orgao), conforme Regras Gerais MSC 2026 (IC nº1).
    private const int DigitosPoderOrgao = 5;

    // Codigo do atributo PO no texto canonico "CHAVE=valor" das informacoes complementares.
    private const string CodigoPoderOrgao = "PO";

    /// <summary>
    /// Valida a matriz FAIL-CLOSED antes de emitir o CSV. Lanca se alguma regra dura for violada (a chamada
    /// deve abortar a geracao — nunca emitir um artefato que o SICONFI rejeitaria).
    /// </summary>
    /// <param name="matriz">Matriz de saldos consolidada da declaracao.</param>
    /// <param name="extrairPoderOrgao">Funcao que extrai o PO da informacao complementar de uma linha.</param>
    /// <exception cref="ArgumentNullException">Se a matriz for nula.</exception>
    /// <exception cref="MscLinhaSemPoderOrgaoCsvException">Se alguma linha nao tem PO valido.</exception>
    /// <exception cref="MscClasseDesbalanceadaCsvException">Se alguma classe contabil nao fecha D=C.</exception>
    /// <exception cref="MscMatrizDesbalanceadaCsvException">Se o total global nao fecha D=C.</exception>
    public static void GarantirValida(MatrizSaldos matriz, Func<string?, string?> extrairPoderOrgao)
    {
        ArgumentNullException.ThrowIfNull(matriz);
        ArgumentNullException.ThrowIfNull(extrairPoderOrgao);

        ValidarPoderOrgaoObrigatorio(matriz, extrairPoderOrgao);
        ValidarBalancoGlobal(matriz);
        ValidarBalancoPorClasse(matriz);
    }

    // Regra dura SICONFI #0 (IC nº1): toda linha carrega um PO valido (5 digitos). Sem PO => rejeicao.
    private static void ValidarPoderOrgaoObrigatorio(MatrizSaldos matriz, Func<string?, string?> extrairPoderOrgao)
    {
        foreach (var linha in matriz.Linhas)
        {
            var po = extrairPoderOrgao(linha.InformacaoComplementar);
            if (!PoderOrgaoValido(po))
            {
                throw new MscLinhaSemPoderOrgaoCsvException(linha.ContaPcasp);
            }
        }
    }

    // Regra geral: o total D=C deve fechar (partida dobrada do balancete consolidado).
    private static void ValidarBalancoGlobal(MatrizSaldos matriz)
    {
        var devedor = matriz.TotalDebitos.Valor;
        var credor = matriz.TotalCreditos.Valor;
        if (Math.Abs(devedor - credor) > ToleranciaFechamento)
        {
            throw new MscMatrizDesbalanceadaCsvException(devedor, credor);
        }
    }

    // Regra dura SICONFI #1: em cada classe contabil (Patrimonial=1-4, Orcamentaria=5-6, Controle=7-8) a
    // soma dos saldos devedores deve igualar a dos credores. So o total geral fechar NAO basta.
    private static void ValidarBalancoPorClasse(MatrizSaldos matriz)
    {
        foreach (var grupo in matriz.Linhas.GroupBy(linha => ClasseDe(linha.ContaPcasp)))
        {
            var devedor = grupo
                .Where(linha => linha.NaturezaSaldo == NaturezaSaldo.Devedor)
                .Sum(linha => linha.Valor.Valor);
            var credor = grupo
                .Where(linha => linha.NaturezaSaldo == NaturezaSaldo.Credor)
                .Sum(linha => linha.Valor.Valor);

            if (Math.Abs(devedor - credor) > ToleranciaFechamento)
            {
                throw new MscClasseDesbalanceadaCsvException(grupo.Key, devedor, credor);
            }
        }
    }

    // Classe contabil (natureza de informacao) a partir do 1o digito da conta PCASP: 1-4 Patrimonial,
    // 5-6 Orcamentaria, 7-8 Controle (PCASP §3) — mesma regra do GeradorMscCsv/MatrizSaldosContabeis.
    private static string ClasseDe(string contaPcasp)
        => PrimeiroDigito(contaPcasp) switch
        {
            >= 1 and <= 4 => "Patrimonial",
            5 or 6 => "Orcamentaria",
            _ => "Controle",
        };

    private static int PrimeiroDigito(string contaPcasp)
    {
        foreach (var c in contaPcasp)
        {
            if (char.IsAsciiDigit(c))
            {
                return c - '0';
            }
        }

        return 7;
    }

    private static bool PoderOrgaoValido(string? poderOrgao)
        => poderOrgao is { Length: DigitosPoderOrgao } && poderOrgao.All(char.IsAsciiDigit);

    /// <summary>Codigo do atributo PO (Poder/Orgao) no texto canonico das informacoes complementares.</summary>
    public static string CodigoIcPoderOrgao => CodigoPoderOrgao;
}
