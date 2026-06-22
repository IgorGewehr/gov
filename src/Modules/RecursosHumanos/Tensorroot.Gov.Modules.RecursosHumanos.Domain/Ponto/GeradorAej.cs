using System.Globalization;
using System.Text;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;

/// <summary>Cabecalho do AEJ: empregador e periodo apurado.</summary>
/// <param name="Cnpj">CNPJ do ente.</param>
/// <param name="RazaoSocial">Razao social/nome do ente.</param>
/// <param name="InicioPeriodo">Primeira data do periodo apurado.</param>
/// <param name="FimPeriodo">Ultima data do periodo apurado.</param>
/// <param name="DataHoraGeracao">Instante de geracao.</param>
public sealed record CabecalhoAej(
    Cnpj Cnpj,
    string RazaoSocial,
    DateOnly InicioPeriodo,
    DateOnly FimPeriodo,
    DateTimeOffset DataHoraGeracao);

/// <summary>Linha diaria do AEJ (jornada tratada de um servidor num dia).</summary>
/// <param name="Cpf">CPF do trabalhador.</param>
/// <param name="Dia">Dia apurado.</param>
/// <param name="MinutosContratados">Jornada contratada do dia (minutos).</param>
/// <param name="MinutosTrabalhados">Minutos trabalhados no dia.</param>
/// <param name="MinutosExtras">Minutos de hora extra no dia.</param>
/// <param name="MinutosFalta">Minutos de falta/atraso no dia.</param>
public sealed record LinhaAej(
    Cpf Cpf,
    DateOnly Dia,
    int MinutosContratados,
    int MinutosTrabalhados,
    int MinutosExtras,
    int MinutosFalta);

/// <summary>
/// Gerador do AEJ (Arquivo Eletronico de Jornada — Anexo VI da Portaria MTP 671/2021): arquivo
/// posicional ASCII (ISO-8859-1) com a jornada TRATADA pelo PTRP (substitui AFDT/ACJEF). Estrutura
/// fiel ao CONCEITO (cabecalho + linhas diarias por servidor com jornada contratada/presencas/extras/
/// atrasos + trailer com CRC). NAO altera o AFD. // TODO(validar-oficial): tipos de registro, POSICOES
/// e LARGURAS exatos do Anexo VI nao foram obtidos do texto oficial; ajustar antes de uso fiscal.
/// Servico de dominio PURO; assinatura CAdES e externa.
/// </summary>
public static class GeradorAej
{
    private const int LarguraTipo = 1;
    private const int LarguraCpf = 11;
    private const int LarguraCnpj = 14;
    private const int LarguraRazao = 150;
    private const int LarguraMinutos = 5;   // ate 99999 min/dia (cabe em um dia)
    private const int LarguraContador = 9;

    /// <summary>Gera os bytes do AEJ (ISO-8859-1) a partir das linhas diarias tratadas.</summary>
    /// <param name="cabecalho">Identificacao do empregador e periodo.</param>
    /// <param name="linhas">Linhas diarias tratadas (qualquer ordem; serao ordenadas por CPF e dia).</param>
    /// <returns>Conteudo do AEJ em bytes, pronto para persistencia/assinatura CAdES.</returns>
    /// <exception cref="ArgumentNullException">Se cabecalho ou linhas forem nulos.</exception>
    public static byte[] Gerar(CabecalhoAej cabecalho, IReadOnlyCollection<LinhaAej> linhas)
    {
        ArgumentNullException.ThrowIfNull(cabecalho);
        ArgumentNullException.ThrowIfNull(linhas);

        var ordenadas = linhas
            .OrderBy(l => l.Cpf.Digitos, StringComparer.Ordinal)
            .ThenBy(l => l.Dia)
            .ToList();

        var corpo = new StringBuilder();

        // ----- CABECALHO (tipo 1) -----
        corpo.Append(CampoPosicional.Numero((int)TipoRegistroAfd.Cabecalho, LarguraTipo));
        corpo.Append(CampoPosicional.Numero(SoDigitos(cabecalho.Cnpj.Digitos), LarguraCnpj));
        corpo.Append(CampoPosicional.Data(cabecalho.InicioPeriodo));
        corpo.Append(CampoPosicional.Data(cabecalho.FimPeriodo));
        corpo.Append(CampoPosicional.Data(DateOnly.FromDateTime(cabecalho.DataHoraGeracao.LocalDateTime.Date)));
        corpo.Append(CampoPosicional.Texto(cabecalho.RazaoSocial, LarguraRazao));
        corpo.Append(CampoPosicional.FimDeLinha);

        // ----- LINHAS DIARIAS TRATADAS (tipo 3) -----
        foreach (var l in ordenadas)
        {
            corpo.Append(CampoPosicional.Numero((int)TipoRegistroAfd.Marcacao, LarguraTipo));
            corpo.Append(CampoPosicional.Numero(SoDigitos(l.Cpf.Digitos), LarguraCpf));
            corpo.Append(CampoPosicional.Data(l.Dia));
            corpo.Append(CampoPosicional.Numero(l.MinutosContratados, LarguraMinutos));
            corpo.Append(CampoPosicional.Numero(l.MinutosTrabalhados, LarguraMinutos));
            corpo.Append(CampoPosicional.Numero(l.MinutosExtras, LarguraMinutos));
            corpo.Append(CampoPosicional.Numero(l.MinutosFalta, LarguraMinutos));
            corpo.Append(CampoPosicional.FimDeLinha);
        }

        // ----- TRAILER (tipo 9) -----
        var bytesCorpo = CampoPosicional.CodificacaoArquivo.GetBytes(corpo.ToString());
        var crc = Crc16Ccitt.CalcularHex(bytesCorpo);

        corpo.Append(CampoPosicional.Numero((int)TipoRegistroAfd.Trailer, LarguraTipo));
        corpo.Append(CampoPosicional.Numero(ordenadas.Count, LarguraContador));
        corpo.Append(CampoPosicional.Texto(crc, 4));
        corpo.Append(CampoPosicional.FimDeLinha);

        return CampoPosicional.CodificacaoArquivo.GetBytes(corpo.ToString());
    }

    private static long SoDigitos(string valor)
        => long.Parse(new string(valor.Where(char.IsDigit).ToArray()), CultureInfo.InvariantCulture);
}
