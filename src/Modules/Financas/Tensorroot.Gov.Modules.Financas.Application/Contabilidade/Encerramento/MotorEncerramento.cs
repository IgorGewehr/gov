using System.Security.Cryptography;
using System.Text;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.ReadModels;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;
using Tensorroot.Gov.Modules.Financas.Domain.ValueObjects;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Encerramento;

/// <summary>
/// Motor do encerramento de exercício (espelha o papel do <c>MotorContabil</c>): lê o balancete e
/// emite <see cref="LancamentoContabil"/> de partida dobrada via o MESMO factory provado — NÃO
/// contabiliza ad-hoc nem chama <c>SaveChanges</c> (o handler controla a transação). Cada fase é
/// idempotente por <c>OrigemReferenciaId</c> determinístico (DESIGN §4). Reprodutível: nada de relógio
/// no cálculo — usa a competência/exercício; a data é derivada do exercício (31/12 ou 01/01).
/// </summary>
public sealed class MotorEncerramento(
    IContaContabilRepository contas,
    ILancamentoContabilRepository lancamentos,
    IBalanceteProjection balancete,
    ITenantContext tenant)
{
    /// <summary>Conta de resultado patrimonial do exercício (IPC 03 §§21-26).</summary>
    public const string ContaResultadoExercicio = "2.3.7.1.1.01.00";

    /// <summary>Conta de resultados de exercícios anteriores (IPC 03 §§27-28).</summary>
    public const string ContaResultadoAnteriores = "2.3.7.1.1.02.00";

    /// <summary>Conta de ajustes de exercícios anteriores.</summary>
    public const string ContaAjustesAnteriores = "2.3.7.1.1.03.00";

    private const int MesApuracao = 13;
    private const int MesAbertura = 0;

    /// <summary>
    /// Apura o resultado patrimonial: encerra as analíticas das classes 3 (VPD) e 4 (VPA) com saldo
    /// ≠ 0 contra <see cref="ContaResultadoExercicio"/> (mês 13). Cada conta gera um lançamento
    /// homogêneo patrimonial. Pré-condição: a conta de resultado deve estar zerada (IPC 03 §§21-23).
    /// </summary>
    /// <param name="exercicio">Exercício a apurar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de lançamentos de encerramento gerados.</returns>
    public async Task<int> ApurarResultadoPatrimonialAsync(int exercicio, CancellationToken cancellationToken)
    {
        var contaResultado = await ResolverContaAsync(ContaResultadoExercicio, cancellationToken).ConfigureAwait(false);
        var data = new DateOnly(exercicio, 12, 31);

        // Pré-condição: 2.3.7.1.1.01.00 zerada antes da apuração (saldo de 1 dia).
        var linhas = await balancete.ListarPorPeriodoAsync(exercicio, 12, cancellationToken).ConfigureAwait(false);
        var saldoResultado = linhas.FirstOrDefault(l => l.ContaId == contaResultado.Id.Value)?.SaldoAtual ?? 0m;
        if (saldoResultado != 0m)
        {
            throw new RoteiroContabilInvalidoException(
                $"Conta {ContaResultadoExercicio} deve estar zerada antes da apuracao patrimonial (saldo={saldoResultado}).");
        }

        var gerados = 0;
        foreach (var linha in linhas.Where(EhClassePatrimonialDeResultado).Where(l => l.SaldoAtual != 0m))
        {
            var origem = OrigemDeterministica(exercicio, "APUR-PATR", linha.CodigoConta);
            if (await lancamentos.ExisteParaOrigemAsync(origem, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var conta = await ResolverContaAsync(linha.CodigoConta, cancellationToken).ConfigureAwait(false);
            var valor = ValorMonetario.De(Math.Abs(linha.SaldoAtual));

            // Classe 3 (VPD, devedora): D resultado / C VPD. Classe 4 (VPA, credora): D VPA / C resultado.
            var ladoConta = linha.Classe() == 3 ? LadoPartida.Credito : LadoPartida.Debito;
            var ladoResultado = ladoConta == LadoPartida.Credito ? LadoPartida.Debito : LadoPartida.Credito;

            var partidas = new[]
            {
                Linha(conta, ladoConta, valor),
                Linha(contaResultado, ladoResultado, valor),
            };

            RegistrarEncerramento(
                exercicio, data, MesApuracao,
                $"Apuracao patrimonial: encerramento de {linha.CodigoConta}",
                origem, partidas);
            gerados++;
        }

        return gerados;
    }

    /// <summary>
    /// Apura/confere o resultado orçamentário encerrando verticalmente as analíticas das classes 5 e 6
    /// de execução com saldo ≠ 0 (mês 13). O confronto receita×despesa é evidenciado no Balanço
    /// Orçamentário; aqui apenas zeramos as contas de execução/dotação (IPC 03 §§33-53, cenário §51).
    /// Cada conta é encerrada contra a contraparte do par homogêneo orçamentário (5↔6).
    /// </summary>
    /// <param name="exercicio">Exercício a apurar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de lançamentos de encerramento gerados.</returns>
    // TODO(validar-oficial): cenario §50 (controle origem inicial×adicional) e o encerramento
    // vertical detalhado por par 5.2.x↔6.2.x ficam como evolucao parametrizavel; aqui adotamos o
    // encerramento por contraparte agregada (§51), suficiente para zerar 5/6 e fechar D=C.
    public async Task<int> ApurarResultadoOrcamentarioAsync(int exercicio, CancellationToken cancellationToken)
    {
        var data = new DateOnly(exercicio, 12, 31);
        var linhas = await balancete.ListarPorPeriodoAsync(exercicio, 12, cancellationToken).ConfigureAwait(false);

        // Soma dos saldos das contas orçamentárias devedoras (classe 5) e credoras (classe 6) ainda abertas.
        var orcamentarias = linhas
            .Where(l => l.NaturezaInformacao == NaturezaInformacao.Orcamentaria && l.SaldoAtual != 0m)
            .ToList();

        var gerados = 0;
        foreach (var linha in orcamentarias)
        {
            var origem = OrigemDeterministica(exercicio, "APUR-ORC", linha.CodigoConta);
            if (await lancamentos.ExisteParaOrigemAsync(origem, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var conta = await ResolverContaAsync(linha.CodigoConta, cancellationToken).ConfigureAwait(false);
            var valor = ValorMonetario.De(Math.Abs(linha.SaldoAtual));

            // Encerra a conta contra a conta-espelho de aprovação/execução (mesma natureza, lado oposto).
            // Espelho: classe 5 (devedora) ↔ classe 6 (credora). Quando há apenas a perna 5 ou 6 com
            // saldo (sistema seed minimo), encerra contra a contraparte canonica da previsao/dotacao.
            var (codigoContraparte, ladoConta, ladoContraparte) = ContraparteOrcamentaria(linha);
            var contraparte = await ResolverContaAsync(codigoContraparte, cancellationToken).ConfigureAwait(false);

            var partidas = new[]
            {
                Linha(conta, ladoConta, valor),
                Linha(contraparte, ladoContraparte, valor),
            };

            RegistrarEncerramento(
                exercicio, data, MesApuracao,
                $"Apuracao orcamentaria: encerramento de {linha.CodigoConta}",
                origem, partidas);
            gerados++;
        }

        return gerados;
    }

    /// <summary>
    /// Abre o exercício seguinte (mês 0, 01/01): transfere o resultado do exercício para exercícios
    /// anteriores e encerra os ajustes de exercícios anteriores, se houver (IPC 03 §§27-28). A
    /// reabertura dos saldos de Ativo/Passivo/PL é obtida sem novo lançamento — o balancete transpõe
    /// o saldo das contas permanentes para o exercício seguinte (DESIGN §4.5).
    /// </summary>
    /// <param name="exercicio">Exercício encerrado (a abertura ocorre em exercicio+1).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de lançamentos de abertura gerados.</returns>
    public async Task<int> AbrirExercicioSeguinteAsync(int exercicio, CancellationToken cancellationToken)
    {
        var exercicioAbertura = exercicio + 1;
        var data = new DateOnly(exercicioAbertura, 1, 1);

        var contaResultado = await ResolverContaAsync(ContaResultadoExercicio, cancellationToken).ConfigureAwait(false);
        var contaAnteriores = await ResolverContaAsync(ContaResultadoAnteriores, cancellationToken).ConfigureAwait(false);
        var contaAjustes = await ResolverContaAsync(ContaAjustesAnteriores, cancellationToken).ConfigureAwait(false);

        // Saldo do resultado do exercício APÓS a apuração patrimonial (lê o mês 13).
        var linhas13 = await balancete.ListarPorPeriodoAsync(exercicio, MesApuracao, cancellationToken).ConfigureAwait(false);
        var saldoResultado = linhas13.FirstOrDefault(l => l.ContaId == contaResultado.Id.Value)?.SaldoAtual ?? 0m;
        var saldoAjustes = linhas13.FirstOrDefault(l => l.ContaId == contaAjustes.Id.Value)?.SaldoAtual ?? 0m;

        var gerados = 0;

        if (saldoResultado != 0m)
        {
            var origem = OrigemDeterministica(exercicioAbertura, "ABERT-RESULT", ContaResultadoExercicio);
            if (!await lancamentos.ExisteParaOrigemAsync(origem, cancellationToken).ConfigureAwait(false))
            {
                var valor = ValorMonetario.De(Math.Abs(saldoResultado));
                // Resultado credor (superávit): D ...01 / C ...02. Devedor (déficit): C ...01 / D ...02.
                var resultadoCredor = saldoResultado > 0m;
                var partidas = new[]
                {
                    Linha(contaResultado, resultadoCredor ? LadoPartida.Debito : LadoPartida.Credito, valor),
                    Linha(contaAnteriores, resultadoCredor ? LadoPartida.Credito : LadoPartida.Debito, valor),
                };
                RegistrarEncerramento(
                    exercicioAbertura, data, MesAbertura,
                    "Abertura: transferencia do resultado do exercicio para exercicios anteriores",
                    origem, partidas);
                gerados++;
            }
        }

        if (saldoAjustes != 0m)
        {
            var origem = OrigemDeterministica(exercicioAbertura, "ABERT-AJUSTE", ContaAjustesAnteriores);
            if (!await lancamentos.ExisteParaOrigemAsync(origem, cancellationToken).ConfigureAwait(false))
            {
                var valor = ValorMonetario.De(Math.Abs(saldoAjustes));
                var ajusteCredor = saldoAjustes > 0m;
                var partidas = new[]
                {
                    Linha(contaAjustes, ajusteCredor ? LadoPartida.Debito : LadoPartida.Credito, valor),
                    Linha(contaAnteriores, ajusteCredor ? LadoPartida.Credito : LadoPartida.Debito, valor),
                };
                RegistrarEncerramento(
                    exercicioAbertura, data, MesAbertura,
                    "Abertura: encerramento de ajustes de exercicios anteriores",
                    origem, partidas);
                gerados++;
            }
        }

        return gerados;
    }

    private void RegistrarEncerramento(
        int exercicio,
        DateOnly data,
        int periodoMes,
        string historico,
        Guid origem,
        IReadOnlyCollection<LinhaLancamento> partidas)
    {
        var lancamento = LancamentoContabil.Registrar(
            tenant.TenantId,
            data,
            exercicio,
            historico,
            OrigemLancamento.Encerramento,
            origem,
            eventoContabilId: null,
            partidas,
            periodoAberto: true,
            periodoMesOverride: periodoMes);

        lancamentos.Adicionar(lancamento);
    }

    private static (string Codigo, LadoPartida LadoConta, LadoPartida LadoContraparte) ContraparteOrcamentaria(LinhaBalancete linha)
    {
        // Encerra a conta pelo lado OPOSTO ao seu saldo (zerando-a) e lança a contraparte no espelho.
        // Receita: contraparte = previsao 5.2.1.1.1.00.00. Despesa/credito: contraparte = dotacao 5.2.2.1.01.
        var ladoConta = linha.NaturezaSaldo == NaturezaSaldo.Devedora ? LadoPartida.Credito : LadoPartida.Debito;
        var ladoContraparte = ladoConta == LadoPartida.Credito ? LadoPartida.Debito : LadoPartida.Credito;

        var codigo = linha.CodigoConta;
        var contraparte = codigo.StartsWith("6.2.1", StringComparison.Ordinal) || codigo.StartsWith("5.2.1", StringComparison.Ordinal)
            ? "5.2.1.1.1.00.00"
            : "5.2.2.1.01";

        // Não encerrar a própria contraparte contra ela mesma; usa o outro lado do par.
        if (string.Equals(codigo, contraparte, StringComparison.Ordinal))
        {
            contraparte = codigo.StartsWith("5.2.1", StringComparison.Ordinal) ? "6.2.1.2.01" : "6.2.2.1.1.00.00";
        }

        return (contraparte, ladoConta, ladoContraparte);
    }

    private static bool EhClassePatrimonialDeResultado(LinhaBalancete linha)
        => linha.Classe() is 3 or 4;

    private async Task<ContaContabil> ResolverContaAsync(string codigo, CancellationToken cancellationToken)
        => await contas.ObterPorCodigoAsync(codigo, cancellationToken).ConfigureAwait(false)
            ?? throw new RoteiroContabilInvalidoException($"Conta {codigo} ausente no plano (rodar seed do plano de contas).");

    private static LinhaLancamento Linha(ContaContabil conta, LadoPartida lado, ValorMonetario valor)
        => new(conta.Id, conta.Codigo, conta.NaturezaInformacao, conta.Tipo, lado, valor);

    /// <summary>
    /// Gera um <see cref="Guid"/> determinístico para (exercício, fase, conta) — idempotência
    /// reprodutível: a MESMA entrada produz a MESMA origem, então reexecutar não duplica lançamentos.
    /// </summary>
    private static Guid OrigemDeterministica(int exercicio, string fase, string conta)
    {
        var chave = $"ENCERRAMENTO|{exercicio}|{fase}|{conta}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(chave));
        return new Guid(hash.AsSpan(0, 16));
    }
}

/// <summary>Extensões de leitura da classe contábil a partir do código da linha de balancete.</summary>
internal static class LinhaBalanceteClasseExtensions
{
    /// <summary>Classe contábil (1º dígito do código).</summary>
    /// <param name="linha">Linha do balancete.</param>
    /// <returns>Classe 1 a 8.</returns>
    public static int Classe(this LinhaBalancete linha)
    {
        ArgumentNullException.ThrowIfNull(linha);
        return linha.CodigoConta.Length > 0 && char.IsDigit(linha.CodigoConta[0])
            ? linha.CodigoConta[0] - '0'
            : 0;
    }
}
