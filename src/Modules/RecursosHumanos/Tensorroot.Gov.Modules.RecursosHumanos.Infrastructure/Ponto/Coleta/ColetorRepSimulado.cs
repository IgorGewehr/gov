using Microsoft.Extensions.Logging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Ponto.Coleta;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Ponto;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Infrastructure.Ponto.Coleta;

/// <summary>
/// Implementacao SIMULADA do <see cref="IColetorRep"/> (ACL): GERA um AFD de exemplo (posicional 671,
/// via <see cref="GeradorAfd"/>) a partir de <c>ultimoNsrConhecido + 1</c>, permitindo exercitar o
/// pipeline ponta-a-ponta (coletar -&gt; parse -&gt; ingestao idempotente) SEM rede nem SDK proprietario.
/// <para>
/// // TODO(prod: SDK proprietario): a impl. REAL fala o protocolo do fabricante (Control iD REST
/// get_afd.fcgi/iDCloud; Henry TCP/Serial/USB com filtro por NSR; Topdata SDK Inner REP DLL/TCP;
/// Madis pendrive/SDK; Dimep REST/Protocolo VIII), com Polly (timeout/retry/circuit breaker) e ACL,
/// mapeando erros do fabricante para excecoes de dominio. Credenciais via Key Vault (CLAUDE.md §6).
/// </para>
/// </summary>
public sealed class ColetorRepSimulado(ILogger<ColetorRepSimulado> logger) : IColetorRep
{
    // CPFs validos deterministicos para o AFD de exemplo (nao correspondem necessariamente a servidores
    // do tenant — a ingestao marca como pendentes-de-vinculo quando nao houver servidor com o CPF).
    private static readonly Cpf[] CpfsExemplo =
    [
        Cpf.Create("39053344705"),
        Cpf.Create("11144477735"),
    ];

    private static readonly Cnpj CnpjExemplo = Cnpj.Create("11222333000181");

    // Quantas marcacoes de exemplo a simulacao "coleta" a cada chamada.
    private const int MarcacoesPorColeta = 4;

    /// <inheritdoc />
    public MarcaRep Marca => MarcaRep.Simulado;

    /// <inheritdoc />
    public ModoColeta Modos => ModoColeta.RestCloud | ModoColeta.TcpSdk;

    /// <inheritdoc />
    public Task<LoteAfdColetado> ColetarAsync(RepConexao conexao, long ultimoNsrConhecido, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conexao);

        var inicioNsr = ultimoNsrConhecido + 1;
        var baseData = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

        var linhas = new List<LinhaMarcacaoAfd>(MarcacoesPorColeta);
        for (var i = 0; i < MarcacoesPorColeta; i++)
        {
            var nsr = inicioNsr + i;
            var cpf = CpfsExemplo[i % CpfsExemplo.Length];
            // Batidas alternadas ao longo do dia (8h, 12h, 13h, 17h) — cronologia plausivel.
            var hora = baseData.AddDays(i / CpfsExemplo.Length).AddHours((i % 4) * 4);
            linhas.Add(new LinhaMarcacaoAfd(Nsr.De(nsr), cpf, hora, TipoRep.RepC));
        }

        var cabecalho = new CabecalhoAfd(
            CnpjExemplo,
            "REP SIMULADO (EXEMPLO)",
            DateOnly.FromDateTime(baseData.Date),
            DateOnly.FromDateTime(baseData.AddDays(MarcacoesPorColeta).Date),
            DateTimeOffset.Now);

        var conteudo = GeradorAfd.Gerar(cabecalho, linhas);

        logger.LogInformation(
            "[Coletor SIMULADO] Equipamento {Equipamento}: gerado AFD de exemplo com {Qtd} marcacao(oes) a partir do NSR {NsrInicial}.",
            conexao.IdentificacaoEquipamento,
            linhas.Count,
            inicioNsr);

        // REP simulado nao fornece assinatura CAdES (.p7s).
        return Task.FromResult(new LoteAfdColetado(conteudo, AssinaturaCades: null, MarcaRep.Simulado, conexao.IdentificacaoEquipamento));
    }
}
