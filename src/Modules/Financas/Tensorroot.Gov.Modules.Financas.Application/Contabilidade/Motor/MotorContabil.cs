using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Abstractions;
using Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Seed;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.EventosContabeis;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.Lancamentos;
using Tensorroot.Gov.Modules.Financas.Domain.Contabilidade.PlanoDeContas;
using Tensorroot.Gov.Modules.Financas.Domain.Exceptions;

namespace Tensorroot.Gov.Modules.Financas.Application.Contabilidade.Motor;

/// <summary>
/// Coração da integração: resolve o roteiro vigente de um fato do ciclo, mapeia as contas e
/// persiste os lançamentos contábeis (um por natureza) na MESMA unidade de trabalho do fato.
/// Idempotente por <c>origemReferenciaId</c> + roteiro.
/// </summary>
public sealed class MotorContabil(
    IEventoContabilRepository eventos,
    IContaContabilRepository contas,
    ILancamentoContabilRepository lancamentos,
    ITenantContext tenant)
{
    /// <summary>
    /// Gera e adiciona os lançamentos contábeis para um fato. Não chama SaveChanges — o handler do
    /// fato controla a transação. Retorna sem efeito se já houver lançamento para a origem (retry).
    /// </summary>
    /// <param name="fato">Fato contábil.</param>
    /// <param name="valor">Valor do fato.</param>
    /// <param name="data">Data de competência.</param>
    /// <param name="origemReferenciaId">Id do fato de origem (idempotência).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de lançamentos gerados.</returns>
    /// <exception cref="EventoContabilNaoVigenteException">Se não houver roteiro vigente.</exception>
    public async Task<int> ContabilizarAsync(
        FatoContabil fato,
        Domain.ValueObjects.ValorMonetario valor,
        DateOnly data,
        Guid origemReferenciaId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(valor);
        if (!valor.EhPositivo())
        {
            return 0;
        }

        var exercicio = data.Year;
        var evento = await eventos.ObterVigenteAsync(fato, exercicio, cancellationToken).ConfigureAwait(false)
            ?? throw new EventoContabilNaoVigenteException(fato.ToString(), data);

        if (await lancamentos.ExisteParaOrigemAsync(origemReferenciaId, evento.Id.Value, cancellationToken).ConfigureAwait(false))
        {
            return 0;
        }

        // Pré-carrega todas as contas referenciadas (resolução do Func deve ser síncrona).
        var cache = new Dictionary<string, ContaContabil>(StringComparer.Ordinal);
        foreach (var linha in evento.Linhas)
        {
            var codigo = CodigoDaLinha(linha);
            if (!cache.ContainsKey(codigo))
            {
                cache[codigo] = await contas.ObterPorCodigoAsync(codigo, cancellationToken).ConfigureAwait(false)
                    ?? throw new RoteiroContabilInvalidoException($"Conta {codigo} ausente no plano (rodar seed do plano de contas).");
            }
        }

        var grupos = evento.ResolverPara(valor, linha => cache[CodigoDaLinha(linha)]);

        var gerados = 0;
        foreach (var grupo in grupos)
        {
            var lancamento = LancamentoContabil.Registrar(
                tenant.TenantId,
                data,
                exercicio,
                $"{evento.Nome} (auto)",
                OrigemLancamento.EventoAutomatico,
                origemReferenciaId,
                evento.Id.Value,
                grupo.Linhas,
                periodoAberto: true);

            lancamentos.Adicionar(lancamento);
            gerados++;
        }

        return gerados;
    }

    private static string CodigoDaLinha(LinhaRoteiro linha)
        => linha.CodigoContaFixo
            ?? (linha.Papel is { } papel
                ? RoteirosCatalogo.MapaPapelCodigoDefault().GetValueOrDefault(papel)
                : null)
            ?? throw new RoteiroContabilInvalidoException("Linha de roteiro sem codigo nem papel mapeavel.");
}
